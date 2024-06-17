using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
using Library;

public class DirectMove : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    [SerializeField] private int particleMax;
    [SerializeField] private Vector3 startVelocity;
    [SerializeField] private float randomStartSpeed;
    [SerializeField] private float lifetime;

    [SerializeField] private bool is2D;

    private int kernelIndexParticleMain;
    private ComputeBuffer particleComputeBuffer;
    private GraphicsBuffer graphicsBuffer;

    private Particle[] resultParticles;

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        graphicsBuffer.Dispose();
        particleComputeBuffer.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        computeShader = Instantiate(computeShader);
        material = Instantiate(material);

        // GraphicsBuffer�𐶐����č��W����ݒ肷��
        graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleMax,
            Marshal.SizeOf<Particle>());

        // (1) �J�[�l���̃C���f�b�N�X��ۑ����܂��B
        kernelIndexParticleMain = computeShader.FindKernel("ParticleMain");

        // (2) ComputeShader �Ōv�Z�������ʂ�ۑ����邽�߂̃o�b�t�@ (ComputeBuffer) ��ݒ肵�܂��B
        // ComputeShader ���ɁA�����^�œ������O�̃o�b�t�@����`����Ă���K�v������܂��B

        // ComputeBuffer �� �ǂ̒��x�̗̈���m�ۂ��邩���w�肵�ď���������K�v������܂��B
        particleComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
        computeShader.SetBuffer
            (kernelIndexParticleMain, "particleDataBuffer", particleComputeBuffer);

        // (3) �K�v�Ȃ� ComputeShader �Ƀp�����[�^��n���܂��B
        Particle[] firstParticles = new Particle[particleMax];
        for (int i = 0; i < particleMax; i++)
        {
            Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
            firstParticles[i].position = transform.position;
            firstParticles[i].scale = transform.localScale.x;
            firstParticles[i].lifetime = lifetime;
            firstParticles[i].velocity = velocity;
        }
        particleComputeBuffer.SetData(firstParticles);

        if (is2D)
        {
            mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                    new Vector3 (-0.5f, -0.5f),
                    new Vector3 (0.5f, -0.5f),
                    new Vector3 (0.5f, 0.5f),
                    new Vector3 (-0.5f, 0.5f),
                };
            mesh.uv = new Vector2[]{
                    new Vector2(0,0),
                    new Vector2(1f,0),
                    new Vector2(1f,1f),
                    new Vector2(0,1f),
                };
            mesh.triangles = new int[] {
                    0, 1, 2,
                    0, 2, 3,
                };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        resultParticles = new Particle[particleMax];

    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetFloat("deltaTime", Time.deltaTime);

        // (3) ComputeShader �� Dispatch ���\�b�h�Ŏ��s���܂��B
        // �w�肵���C���f�b�N�X�̃J�[�l�����w�肵���O���[�v���Ŏ��s���܂��B
        computeShader.Dispatch(kernelIndexParticleMain, particleMax / (8 * 8 * 8), 1, 1);

        // (4) ���s���ʂ��擾���Ċm�F���܂��B
        //Particle[] resultParticles = new Particle[particleMax];
        particleComputeBuffer.GetData(resultParticles);

        graphicsBuffer.SetData(resultParticles);
        // �}�e���A���Ƀo�b�t�@��ݒ�
        material.SetBuffer("_Positions", graphicsBuffer);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, mesh.bounds, graphicsBuffer.count);
    }
}
