using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DirectMove : MonoBehaviour
{
    private static readonly float baseMeshSize = 0.01f;

    [SerializeField] private ComputeShader computeShader;

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    [SerializeField] private int particleMax;
    [SerializeField] private Vector3 startVelocity;
    [SerializeField] private float randomStartSpeed;
    [SerializeField] private float lifetime;
    [SerializeField] private float scale;
    [SerializeField] private float randomScale;
    [SerializeField] private float emissionColorDeltaSpeed;
    [SerializeField] private float emissionIntensity;

    [SerializeField] private bool is2D;

    [SerializeField] private float spawnSpeed;

    private Emitter emitter;

    private Particle[] spawnInfos;

    private Vector3 prePosition;

    private float timeAfterStart = 0;

    private int kernelIndexParticleMain;
    private int kernelIndexSpawnParticle;
    private ComputeBuffer particleComputeBuffer;
    private ComputeBuffer spawnParticlerComputeBuffer;
    private ComputeBuffer spawnCountComputeBuffer;

    private int[] spawnCountBuffer = new int[1];

    private void OnEnable()
    {
        prePosition = transform.position;
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

        // (1) ƒJ[ƒlƒ‹‚ÌƒCƒ“ƒfƒbƒNƒX‚ð•Û‘¶‚µ‚Ü‚·B
        kernelIndexParticleMain = computeShader.FindKernel("ParticleMain");
        kernelIndexSpawnParticle = computeShader.FindKernel("SpawnParticle");

        // (2) ComputeShader ‚ÅŒvŽZ‚µ‚½Œ‹‰Ê‚ð•Û‘¶‚·‚é‚½‚ß‚Ìƒoƒbƒtƒ@ (ComputeBuffer) ‚ðÝ’è‚µ‚Ü‚·B
        // ComputeShader “à‚ÉA“¯‚¶Œ^‚Å“¯‚¶–¼‘O‚Ìƒoƒbƒtƒ@‚ª’è‹`‚³‚ê‚Ä‚¢‚é•K—v‚ª‚ ‚è‚Ü‚·B

        // ComputeBuffer ‚Í ‚Ç‚Ì’ö“x‚Ì—Ìˆæ‚ðŠm•Û‚·‚é‚©‚ðŽw’è‚µ‚Ä‰Šú‰»‚·‚é•K—v‚ª‚ ‚è‚Ü‚·B
        particleComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
        spawnParticlerComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
        spawnCountComputeBuffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(int)));

        computeShader.SetBuffer(kernelIndexParticleMain, "particleDataBuffer", particleComputeBuffer);
        computeShader.SetBuffer(kernelIndexSpawnParticle, "particleDataBuffer", particleComputeBuffer);
        computeShader.SetBuffer(kernelIndexSpawnParticle, "spawnParticleDataBuffer", spawnParticlerComputeBuffer);
        computeShader.SetBuffer(kernelIndexSpawnParticle, "spawnCountDataBuffer", spawnCountComputeBuffer);

        // (3) •K—v‚È‚ç ComputeShader ‚Éƒpƒ‰ƒ[ƒ^‚ð“n‚µ‚Ü‚·B
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
                    new Vector3 (-baseMeshSize, -baseMeshSize),
                    new Vector3 (baseMeshSize, -baseMeshSize),
                    new Vector3 (baseMeshSize, baseMeshSize),
                    new Vector3 (-baseMeshSize, baseMeshSize),
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

        // (3) ComputeShader ‚ð Dispatch ƒƒ\ƒbƒh‚ÅŽÀs‚µ‚Ü‚·B
        // Žw’è‚µ‚½ƒCƒ“ƒfƒbƒNƒX‚ÌƒJ[ƒlƒ‹‚ðŽw’è‚µ‚½ƒOƒ‹[ƒv”‚ÅŽÀs‚µ‚Ü‚·B
        computeShader.Dispatch(kernelIndexParticleMain, particleMax / (8 * 8 * 8), 1, 1);

        // (4) ŽÀsŒ‹‰Ê‚ðŽæ“¾‚µ‚ÄŠm”F‚µ‚Ü‚·B
        //Particle[] resultParticles = new Particle[particleMax];
        //particleComputeBuffer.GetData(resultParticles);

        //graphicsBuffer.SetData(resultParticles);
        // ƒ}ƒeƒŠƒAƒ‹‚Éƒoƒbƒtƒ@‚ðÝ’è
        material.SetBuffer("_Positions", particleComputeBuffer);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, mesh.bounds, particleComputeBuffer.count);
    }

    void LateUpdate()
    {
        prePosition = transform.position;

        timeAfterStart += Time.deltaTime;
    }

    private void SetSpawnInfo(int spawnNum)
    {
        spawnNum = Mathf.Min(spawnNum, particleMax);
        //Particle[] spawnInfos = new Particle[spawnNum];

        for (int i = 0; i < spawnNum; i++)
        {
            Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
            float scaleThis = scale + randomScale * Random.value;
            Vector3 deltaPos = transform.position - prePosition;
            spawnInfos[i].position = prePosition + deltaPos * (i + 1) / spawnNum;
            spawnInfos[i].scale = scaleThis;
            spawnInfos[i].lifetime = lifetime;
            spawnInfos[i].velocity = velocity;
            spawnInfos[i].color = MakeHDRColor(Color.HSVToRGB(emissionColorDeltaSpeed * timeAfterStart - ((int)(emissionColorDeltaSpeed * timeAfterStart)), 1, 1), emissionIntensity);
        }
        var segment = spawnInfos[0..(spawnNum)];
        spawnParticlerComputeBuffer.SetData(segment);
        //spawnParticlerComputeBuffer.SetData(spawnInfos);
    }

    public static Color MakeHDRColor(Color ldrColor, float intensity)
    {
        var factor = Mathf.Pow(2, intensity);
        return new Color(
            ldrColor.r * factor,
            ldrColor.g * factor,
            ldrColor.b * factor,
            ldrColor.a
        );
    }
}

public struct Particle
{
    public Vector3 velocity;
    public Vector3 position;
    public float scale;
    public float lifetime;
    public Color color;
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