using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveRandom : MonoBehaviour
{
    [SerializeField] private Vector3 randomRange;
    [SerializeField] private float randomRadius;

    private GPUParticleSystem ps;

    // Start is called before the first frame update
    void Start()
    {
        ps = GetComponent<GPUParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(Random.value * randomRange.x, 5 + Random.value * randomRange.y, Random.value * randomRange.z);
        //ps.RandomScale = Random.value * randomRadius;
    }
}
