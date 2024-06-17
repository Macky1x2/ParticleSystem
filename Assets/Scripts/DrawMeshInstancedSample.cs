using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
using Library;


class DrawMeshInstancedSample : MonoBehaviour
{
    public Material material;
    public Mesh mesh;

    public Vector3[] positions = new[]
    {
        new Vector3(0, 0, 0),
        new Vector3(2, 0, 0),
        new Vector3(4, 0, 0)
    };

    private GraphicsBuffer _graphicsBuffer;

    private void OnEnable()
    {
        // GraphicsBufferを生成して座標情報を設定する
        _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, positions.Length,
            Marshal.SizeOf<Vector3>());
        _graphicsBuffer.SetData(positions);
    }

    private void OnDisable()
    {
        _graphicsBuffer.Dispose();
    }

    public void Update()
    {
        Debug.Log(_graphicsBuffer.count);
        // マテリアルにバッファを設定
        material.SetBuffer("_Positions", _graphicsBuffer);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, mesh.bounds, _graphicsBuffer.count);
    }
}