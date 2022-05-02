using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Profiling;

public class SphereGeneratorCompute : MonoBehaviour
{
    private Mesh mesh;
    public MeshCollider meshCollider;

    public ComputeShader computeShader;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer normalsBuffer;
    private ComputeBuffer texCoordsBuffer;

    private int _kernel;
    private int dispatchCount_x = 0;
    private AsyncGPUReadbackRequest request;


    void Start()
    {

        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            print("AsyncGPUReadback not supported!");
            this.gameObject.SetActive(false); return;
        }


        _kernel = computeShader.FindKernel("CSMain");
        uint threadX = 0;
        uint threadY = 0;
        uint threadZ = 0;
        computeShader.GetKernelThreadGroupSizes(_kernel, out threadX, out threadY, out threadZ);
        dispatchCount_x = 1;
        // dispatchCount_x = Mathf.CeilToInt((int)((vertices.Length) / threadX));

        // vertexBuffer = new ComputeBuffer(sphere.vertices.Length, 12); // 3*4bytes = sizeof(Vector3)
        // vertexBuffer.SetData(sphere.vertices);
        // computeShader.SetBuffer(_kernel, "vertexBuffer", vertexBuffer);

        // normalsBuffer = new ComputeBuffer(sphere.normals.Length, 12); // 3*4bytes = sizeof(Vector3)
        // normalsBuffer.SetData(sphere.normals);
        // computeShader.SetBuffer(_kernel, "normalsBuffer", normalsBuffer);

        // texCoordsBuffer = new ComputeBuffer(sphere.texCoords.Length, 8); // 2*4bytes = sizeof(Vector3)
        // texCoordsBuffer.SetData(sphere.texCoords);
        // computeShader.SetBuffer(_kernel, "texCoordsBuffer", texCoordsBuffer);


        // AsyncGPUReadbackRequest Returns an AsyncGPUReadbackRequest that you can use to determine when the data is available.
        // Otherwise, a request with an error is returned.
        // https://docs.unity3d.com/ScriptReference/Rendering.AsyncGPUReadback.Request.html
        // asyncGPUReadbackCallback -= AsyncGPUReadbackCallback;
        // asyncGPUReadbackCallback += AsyncGPUReadbackCallback;
        // request = AsyncGPUReadback.Request(vertexBuffer, asyncGPUReadbackCallback);
    }

    public bool threaded;
    void Update()
    {
        // //run the compute shader, the position of particles will be updated in GPU
        // computeShader.SetFloat("_Time", Time.time);
        // computeShader.SetFloat("radius", radius);
        // computeShader.SetFloat("V", V);
        // computeShader.SetFloat("size", sphere.vertices.Length);
        // computeShader.Dispatch(_kernel, 128, 1, 1);
    }

    int ii;
    // //The callback will be run when the request is ready
    private static event System.Action<AsyncGPUReadbackRequest> asyncGPUReadbackCallback;
    public void AsyncGPUReadbackCallback(AsyncGPUReadbackRequest request)
    {
        if (!mesh) return;
        // ii++;

        //Readback and show result on texture
        // sphere.vertices = request.GetData<Vector3>().ToArray();

        // // Update mesh
        //meshCollider.sharedMesh = mesh;

        //Request AsyncReadback again
        // request = AsyncGPUReadback.Request(testBuffer, asyncGPUReadbackCallback);
        request = AsyncGPUReadback.Request(vertexBuffer, asyncGPUReadbackCallback);

        // for (int i = 0; i < testArray.Length; i++)
        // {
        //     Debug.Log(i + ": " + testArray[i]);
        // }

        // if (ii >= 4) EditorApplication.ExitPlaymode();
    }

    void OnDestroy()
    {
        asyncGPUReadbackCallback -= AsyncGPUReadbackCallback;
        if (vertexBuffer != null) vertexBuffer.Release();
        if (normalsBuffer != null) normalsBuffer.Release();
        if (texCoordsBuffer != null) texCoordsBuffer.Release();
    }
}
