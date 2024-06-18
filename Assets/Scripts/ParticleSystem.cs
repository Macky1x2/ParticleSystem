using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;

namespace Library
{
    public class ParticleSystem : MonoBehaviour
    {
        [SerializeField] private ComputeShader computeShader;

        [SerializeField] private Mesh mesh;
        [SerializeField] private Material material;

        [SerializeField] private float lifetime;
        [SerializeField] private float randomLifetime;
        [SerializeField] private int particleMax;
        [SerializeField] private Vector3 startVelocity;
        [SerializeField] private float randomStartSpeed;
        [SerializeField] private float spawnSpeed;

        [SerializeField] private bool is2D;

        private Emitter emitter;

        private ObjectPool<ParticleBuffer> particlePool;

        private ParticleBuffer[] allParticles;

        private Particle[] resultParticles;
        public List<int> resetIDs { get; private set; } = new List<int>();

        private int forID = 0;

        private int kernelIndexParticleMain;
        private ComputeBuffer particleComputeBuffer;
        private GraphicsBuffer graphicsBuffer;

        private void OnDisable()
        {
            graphicsBuffer.Dispose();
            particleComputeBuffer.Dispose();
        }

        // Start is called before the first frame update
        void Start()
        {
            allParticles = new ParticleBuffer[particleMax];

            computeShader = Instantiate(computeShader);
            material = Instantiate(material);

            // GraphicsBuffer‚ð¶¬‚µ‚ÄÀ•Wî•ñ‚ðÝ’è‚·‚é
            graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleMax,
                Marshal.SizeOf<Particle>());

            // (1) ƒJ[ƒlƒ‹‚ÌƒCƒ“ƒfƒbƒNƒX‚ð•Û‘¶‚µ‚Ü‚·B
            kernelIndexParticleMain = computeShader.FindKernel("ParticleMain");

            // (2) ComputeShader ‚ÅŒvŽZ‚µ‚½Œ‹‰Ê‚ð•Û‘¶‚·‚é‚½‚ß‚Ìƒoƒbƒtƒ@ (ComputeBuffer) ‚ðÝ’è‚µ‚Ü‚·B
            // ComputeShader “à‚ÉA“¯‚¶Œ^‚Å“¯‚¶–¼‘O‚Ìƒoƒbƒtƒ@‚ª’è‹`‚³‚ê‚Ä‚¢‚é•K—v‚ª‚ ‚è‚Ü‚·B

            // ComputeBuffer ‚Í ‚Ç‚Ì’ö“x‚Ì—Ìˆæ‚ðŠm•Û‚·‚é‚©‚ðŽw’è‚µ‚Ä‰Šú‰»‚·‚é•K—v‚ª‚ ‚è‚Ü‚·B
            particleComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
            computeShader.SetBuffer
                (kernelIndexParticleMain, "particleDataBuffer", particleComputeBuffer);

            // (3) •K—v‚È‚ç ComputeShader ‚Éƒpƒ‰ƒ[ƒ^‚ð“n‚µ‚Ü‚·B
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

            particlePool = new ObjectPool<ParticleBuffer>(
                OnCreatePoolObject, 
                OnTakeFromPool, 
                OnReturnedToPool, 
                OnDestroyPoolObject);

             emitter = new Emitter(spawnSpeed, particleMax, particlePool);

            resultParticles = new Particle[particleMax];
        }

        // Update is called once per frame
        void Update()
        {
            emitter.Update();
            for(int i = 0; i < particleMax; i++)
            {
                if (allParticles[i] != null && allParticles[i].isActive == true)
                {
                    allParticles[i].Update();
                }
            }

            computeShader.SetFloat("deltaTime", Time.deltaTime);

            // (3) ComputeShader ‚ð Dispatch ƒƒ\ƒbƒh‚ÅŽÀs‚µ‚Ü‚·B
            // Žw’è‚µ‚½ƒCƒ“ƒfƒbƒNƒX‚ÌƒJ[ƒlƒ‹‚ðŽw’è‚µ‚½ƒOƒ‹[ƒv”‚ÅŽÀs‚µ‚Ü‚·B
            computeShader.Dispatch(kernelIndexParticleMain, particleMax / (8 * 8 * 8), 1, 1);

            // (4) ŽÀsŒ‹‰Ê‚ðŽæ“¾‚µ‚ÄŠm”F‚µ‚Ü‚·B
            //Particle[] resultParticles = new Particle[particleMax];
            particleComputeBuffer.GetData(resultParticles);

            graphicsBuffer.SetData(resultParticles);
            // ƒ}ƒeƒŠƒAƒ‹‚Éƒoƒbƒtƒ@‚ðÝ’è
            material.SetBuffer("_Positions", graphicsBuffer);
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, mesh.bounds, graphicsBuffer.count);
        }

        private void LateUpdate()
        {
            for (int i = 0; i < resetIDs.Count; i++)
            {
                //Debug.Log(resetIDs[i]);
                Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
                resultParticles[resetIDs[i]].position = transform.position;
                resultParticles[resetIDs[i]].scale = transform.localScale.x;
                resultParticles[resetIDs[i]].lifetime = lifetime;
                resultParticles[resetIDs[i]].velocity = velocity;
            }
            particleComputeBuffer.SetData(resultParticles);

            resetIDs.Clear();
        }

        // ObjectPool コンストラクタ 1つ目の引数の関数 
        // プールに空きがないときに新たに生成する処理
        // objectPool.Get()のときに呼ばれる
        private ParticleBuffer OnCreatePoolObject()
        {
            resetIDs.Add(forID);

            Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
            ParticleBuffer p = new ParticleBuffer(transform.position, transform.rotation, transform.localScale, lifetime + randomLifetime * Random.value, velocity, particlePool, forID++);
            p.isActive = true;
            allParticles[p.id] = p;
            return p;
        }

        // ObjectPool コンストラクタ 2つ目の引数の関数 
        // プールに空きがあったときの処理
        // objectPool.Get()のときに呼ばれる
        private void OnTakeFromPool(ParticleBuffer target)
        {
            resetIDs.Add(target.id);

            Vector3 velocity = startVelocity + new Vector3(Random.value, Random.value, Random.value) * randomStartSpeed;
            target.Init(transform.position, transform.rotation, transform.localScale, lifetime + randomLifetime * Random.value, velocity);
            target.isActive = true;
        }

        // ObjectPool コンストラクタ 3つ目の引数の関数 
        // プールに返却するときの処理
        private void OnReturnedToPool(ParticleBuffer target)
        {
            target.isActive = false;
        }

        // ObjectPool コンストラクタ 4つ目の引数の関数 
        // MAXサイズより多くなったときに自動で破棄する
        private void OnDestroyPoolObject(ParticleBuffer target)
        {
            target.isActive = false;
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

        private ObjectPool<ParticleBuffer> particlePool;

        public Emitter(float spawnSpeed, int particleMax, ObjectPool<ParticleBuffer> particlePool)
        {
            Init(spawnSpeed, particleMax);

            this.particlePool = particlePool;
        }

        public void Init(float spawnSpeed, int particleMax)
        {
            this.oneTime = 1 / spawnSpeed;
            this.particleMax = particleMax;
        }

        public void Update()
        {
            spawnTimer += Time.deltaTime;
            while (spawnTimer >= oneTime)
            {
                Debug.Log(particlePool.CountActive);
                if (particlePool.CountActive < particleMax)
                {
                    particlePool.Get();
                    spawnTimer -= oneTime;
                }
                else
                {
                    break;
                }
            }

            Move();
        }

        private void Move()
        {

        }
    }

    public class ParticleBuffer
    {
        public int id { get; private set; }

        public Vector3 position { get; private set; }
        public Quaternion rotation { get; private set; }
        public Vector3 scale { get; private set; }
        private float lifetime;
        private Vector3 velocity;

        public bool isActive { get; set; }

        private ObjectPool<ParticleBuffer> particlePool;

        public ParticleBuffer(Vector3 position, Quaternion rotation, Vector3 scale, float lifetime, Vector3 velocity, ObjectPool<ParticleBuffer> particlePool, int id)
        {
            Init(position, rotation, scale, lifetime, velocity);

            this.particlePool = particlePool;
            this.id = id;
        }

        public void Init(Vector3 position, Quaternion rotation, Vector3 scale, float lifetime, Vector3 velocity)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.lifetime = lifetime;
            this.velocity = velocity;
        }

        public void Update()
        {
            lifetime -= Time.deltaTime;

            if (lifetime <= 0)
            {
                particlePool.Release(this);
            }
        }
    }
}
