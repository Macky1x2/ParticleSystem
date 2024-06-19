using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DirectMove : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    [SerializeField] private int particleMax;
    [SerializeField] private Vector3 startVelocity;
    [SerializeField] private float randomStartSpeed;
    [SerializeField] private float lifetime;
    [SerializeField] private float scale;
    [SerializeField] private float randomScale;

    [SerializeField] private bool is2D;

    [SerializeField] private float spawnSpeed;

    private Emitter emitter;

    private Particle[] spawnInfos;

    private int kernelIndexParticleMain;
    private int kernelIndexSpawnParticle;
    private ComputeBuffer particleComputeBuffer;
    private ComputeBuffer spawnParticlerComputeBuffer;
    private ComputeBuffer spawnCountComputeBuffer;
    private int[] spawnCountBuffer = new int[1];

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        particleComputeBuffer.Dispose();
        spawnCountComputeBuffer.Dispose();
        spawnParticlerComputeBuffer.Dispose();
    }

    // Start is called before the first frame update
    void Start()
    {
        computeShader = Instantiate(computeShader);
        material = Instantiate(material);

        // (1) �J�[�l���̃C���f�b�N�X��ۑ����܂��B
        kernelIndexParticleMain = computeShader.FindKernel("ParticleMain");
        kernelIndexSpawnParticle = computeShader.FindKernel("SpawnParticle");

        // (2) ComputeShader �Ōv�Z�������ʂ�ۑ����邽�߂̃o�b�t�@ (ComputeBuffer) ��ݒ肵�܂��B
        // ComputeShader ���ɁA�����^�œ������O�̃o�b�t�@����`����Ă���K�v������܂��B

        // ComputeBuffer �� �ǂ̒��x�̗̈���m�ۂ��邩���w�肵�ď���������K�v������܂��B
        particleComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
        spawnParticlerComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
        spawnCountComputeBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(int)));

        computeShader.SetBuffer(kernelIndexParticleMain, "particleDataBuffer", particleComputeBuffer);
        computeShader.SetBuffer(kernelIndexSpawnParticle, "particleDataBuffer", particleComputeBuffer);
        computeShader.SetBuffer(kernelIndexSpawnParticle, "spawnParticleDataBuffer", spawnParticlerComputeBuffer);
        computeShader.SetBuffer(kernelIndexSpawnParticle, "spawnCountDataBuffer", spawnCountComputeBuffer);

        // (3) �K�v�Ȃ� ComputeShader �Ƀp�����[�^��n���܂��B
        spawnInfos = new Particle[particleMax];
        Particle[] firstParticles = new Particle[particleMax];
        for (int i = 0; i < particleMax; i++)
        {
            Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
            firstParticles[i].position = transform.position;
            firstParticles[i].scale = transform.localScale.x;
            firstParticles[i].lifetime = 0;
            firstParticles[i].velocity = velocity;
        }
        particleComputeBuffer.SetData(firstParticles);

        if (is2D)
        {
            mesh = new Mesh();
            mesh.vertices = new Vector3[] {
                    new Vector3 (-0.01f, -0.01f),
                    new Vector3 (0.01f, -0.01f),
                    new Vector3 (0.01f, 0.01f),
                    new Vector3 (-0.01f, 0.01f),
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

        emitter = new Emitter(spawnSpeed, particleMax);
    }

    // Update is called once per frame
    void Update()
    {
        int spawnNum = emitter.GetSpawnNum();
        SetSpawnInfo(spawnNum);
        computeShader.SetInt("spawnNum", spawnNum);
        spawnCountBuffer[0] = 0;
        spawnCountComputeBuffer.SetData(spawnCountBuffer);
        computeShader.Dispatch(kernelIndexSpawnParticle, particleMax / (8 * 8 * 8), 1, 1);

        computeShader.SetFloat("deltaTime", Time.deltaTime);

        // (3) ComputeShader �� Dispatch ���\�b�h�Ŏ��s���܂��B
        // �w�肵���C���f�b�N�X�̃J�[�l�����w�肵���O���[�v���Ŏ��s���܂��B
        computeShader.Dispatch(kernelIndexParticleMain, particleMax / (8 * 8 * 8), 1, 1);

        // (4) ���s���ʂ��擾���Ċm�F���܂��B
        //Particle[] resultParticles = new Particle[particleMax];
        //particleComputeBuffer.GetData(resultParticles);

        //graphicsBuffer.SetData(resultParticles);
        // �}�e���A���Ƀo�b�t�@��ݒ�
        material.SetBuffer("_Positions", particleComputeBuffer);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, mesh.bounds, particleComputeBuffer.count);
    }

    private void SetSpawnInfo(int spawnNum)
    {
        spawnNum = Mathf.Min(spawnNum, particleMax);
        //Particle[] spawnInfos = new Particle[spawnNum];

        for (int i = 0; i < spawnNum; i++)
        {
            Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
            float scaleThis = scale + randomScale * Random.value;
            spawnInfos[i].position = transform.position;
            spawnInfos[i].scale = transform.localScale.x;
            spawnInfos[i].lifetime = lifetime;
            spawnInfos[i].velocity = velocity;
        }
        var segment = spawnInfos[0..(spawnNum)];
        spawnParticlerComputeBuffer.SetData(segment);
        //spawnParticlerComputeBuffer.SetData(spawnInfos);
    }
}

public struct Particle
{
    public Vector3 velocity;
    public Vector3 position;
    public float scale;
    public float lifetime;
};

public class Emitter
{
    private float oneTime;
    private int particleMax;

    private float spawnTimer = 0;

    public Emitter(float spawnSpeed, int particleMax)
    {
        Init(spawnSpeed, particleMax);
    }

    public void Init(float spawnSpeed, int particleMax)
    {
        this.oneTime = 1 / spawnSpeed;
        this.particleMax = particleMax;
    }

    public int GetSpawnNum()
    {
        spawnTimer += Time.deltaTime;
        int ret = (int)(spawnTimer / oneTime);
        spawnTimer -= oneTime * ret;
        return ret;
    }

    public void Move()
    {

    }
}