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

        private int forID = 0;

        private int kernelIndexParticleMain;
        private ComputeBuffer particleComputeBuffer;

        // Start is called before the first frame update
        void Start()
        {
            allParticles = new ParticleBuffer[particleMax];

            // (1) カーネルのインデックスを保存します。
            kernelIndexParticleMain = computeShader.FindKernel("ParticleMain");

            // (2) ComputeShader で計算した結果を保存するためのバッファ (ComputeBuffer) を設定します。
            // ComputeShader 内に、同じ型で同じ名前のバッファが定義されている必要があります。

            // ComputeBuffer は どの程度の領域を確保するかを指定して初期化する必要があります。
            particleComputeBuffer = new ComputeBuffer(particleMax, Marshal.SizeOf(typeof(Particle)));
            computeShader.SetBuffer
                (kernelIndexParticleMain, "particleDataBuffer", particleComputeBuffer);

            // (3) 必要なら ComputeShader にパラメータを渡します。
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
            //this.computeShader.SetInt("intValue", 1);

            //// (3) ComputeShader を Dispatch メソッドで実行します。
            //// 指定したインデックスのカーネルを指定したグループ数で実行します。
            //// グループ数は X * Y * Z で指定します。この例では 1 * 1 * 1 = 1 グループです。

            //this.computeShader.Dispatch(this.kernelIndex_KernelFunction_A, 1, 1, 1);

            //// (4) 実行結果を取得して確認します。

            //int[] result = new int[4];

            //this.particleComputeBuffer.GetData(result);

            //Debug.Log("RESULT : KernelFunction_A");

            //for (int i = 0; i < 4; i++)
            //{
            //    Debug.Log(result[i]);
            //}

            //// (5) ComputerShader 内にある異なるカーネルを実行します。
            //// ここではカーネル「KernelFunction_A」で使ったバッファを使いまわします。

            //this.computeShader.SetBuffer
            //    (this.kernelIndex_KernelFunction_B, "intBuffer", this.intComputeBuffer);
            //this.computeShader.Dispatch(this.kernelIndex_KernelFunction_B, 1, 1, 1);

            //this.intComputeBuffer.GetData(result);

            //Debug.Log("RESULT : KernelFunction_B");

            //for (int i = 0; i < 4; i++)
            //{
            //    Debug.Log(result[i]);
            //}

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
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log(particlePool.CountActive);

            emitter.Update();

            List<Matrix4x4> tmpMatrix = new List<Matrix4x4>();
            for(int i = 0; i < allParticles.Length; i++)
            {
                if (allParticles[i] != null && allParticles[i].isActive)
                {
                    allParticles[i].Update();
                    tmpMatrix.Add(Matrix4x4.TRS(allParticles[i].position, allParticles[i].rotation, allParticles[i].scale));
                    if (tmpMatrix.Count == 1023)
                    {
                        Graphics.DrawMeshInstanced(mesh, 0, material, tmpMatrix);
                        tmpMatrix.Clear();
                    }
                }
            }
            Graphics.DrawMeshInstanced(mesh, 0, material, tmpMatrix);
        }

        // ObjectPool コンストラクタ 1つ目の引数の関数 
        // プールに空きがないときに新たに生成する処理
        // objectPool.Get()のときに呼ばれる
        private ParticleBuffer OnCreatePoolObject()
        {
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
                if (particlePool.CountAll < particleMax)
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
                return;
            }

            Move();
        }

        private void Move()
        {
            position += velocity * Time.deltaTime;
        }
    }
}
