using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Horizontal"))
        {
            transform.position = new Vector3(transform.position.x - 300 * Input.GetAxis("Horizontal") * Time.deltaTime, transform.position.y, transform.position.z);
        }
    }
}
