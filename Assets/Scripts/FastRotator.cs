using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Profiling;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
public class FastRotator : MonoBehaviour
{
    public class MeshData
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> normals;
        public NativeArray<Vector2> texCoords;
        public List<int> indices;
    }

    MeshData g_meshData;
    Mesh mesh;
    MeshFilter meshFilter;

    MeshData g_collisionMeshData;
    Mesh collisionMesh;
    MeshCollider meshCollider;

    public SphereData sphereData;

    Recorder updateRecorder;
    [Range(0.01f, 1f)] public float V;
    [Min(0)] public float radius;
    [Min(3)] public int sectorCount;

    FastRotator(int sectorCount, float initialVelocity, float radius)
    {
        this.sectorCount = sectorCount;
        this.radius = radius;
        this.V = initialVelocity;
    }

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshCollider.cookingOptions = MeshColliderCookingOptions.UseFastMidphase;
        mesh = new Mesh();
        collisionMesh = new Mesh();

        sphereData.actualSectorCount = sectorCount;
        sphereData.actualStackCount = sphereData.actualSectorCount + 1;
        int vertexCount = (sphereData.actualSectorCount + 1) * (sphereData.actualStackCount + 1);

        g_meshData = Generate(sectorCount, V, radius);
        g_collisionMeshData = Generate(8, V, radius);
        collisionMesh.SetVertices(g_collisionMeshData.vertices);
        collisionMesh.SetIndices(g_collisionMeshData.indices, MeshTopology.Triangles, 0);
        meshCollider.sharedMesh = collisionMesh;
    }

    void Update()
    {
        sphereData = new SphereData();
        sphereData.actualSectorCount = sectorCount;
        sphereData.actualStackCount = sphereData.actualSectorCount + 1;
        sphereData._ro = new NativeArray<float>(sphereData.actualStackCount + 1, Allocator.Persistent);
        sphereData._y = new NativeArray<float>(sphereData.actualStackCount + 1, Allocator.Persistent);

        sphereData.vertices = g_meshData.vertices;
        sphereData.normals = g_meshData.normals;
        sphereData.texCoords = g_meshData.texCoords;

        int vertexCount = (sphereData.actualSectorCount + 1) * (sphereData.actualStackCount + 1);

        sphereData.radius = radius;
        sphereData.V = V;
        sphereData.Schedule().Complete();

        mesh.SetVertices(sphereData.vertices);
        mesh.SetIndices(g_meshData.indices, MeshTopology.Triangles, 0);
        mesh.SetNormals(sphereData.normals);
        mesh.SetUVs(0, sphereData.texCoords);
        meshFilter.mesh = mesh;
        sphereData._ro.Dispose();
        sphereData._y.Dispose();
    }

    void OnDestroy()
    {
        g_meshData.vertices.Dispose();
        g_meshData.normals.Dispose();
        g_meshData.texCoords.Dispose();
        g_collisionMeshData.vertices.Dispose();
        g_collisionMeshData.normals.Dispose();
        g_collisionMeshData.texCoords.Dispose();
    }





    ////////////////////////////////////
    // Sphere Mesh generation
    ////////////////////////////////

    // todo:
    // This function is in large part redundant(see SphereData.Execute()), we could potentially eliminate it to simplify this class.
    // The only problem is that we need to generate the indicies somewhere else.
    // We could do that conditionnaly in the SphereData.Execute() job
    public MeshData Generate(int sectorCount, float V, float radius)
    {
        MeshData meshData = new MeshData();

        float sectorStep;
        float stackStep;
        float theta;
        float phi;

        int stackCount = sectorCount + 1;
        int vertexCount = (sectorCount + 1) * (stackCount + 1);
        meshData.vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        meshData.normals = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        meshData.texCoords = new NativeArray<Vector2>(vertexCount, Allocator.Persistent);
        meshData.indices = new List<int>();

        float[] _ro = new float[stackCount + 1];
        float[] _y = new float[stackCount + 1];

        float C1 = Mathf.Sin(1f / 3f);
        float C2 = C1 * Mathf.PI;
        float a = 2f / 3f * V;

        _ro[0] = 0;
        _y[0] = 1;
        _ro[stackCount - 1] = 0;
        _y[stackCount - 1] = -1;

        for (int i = 1; i < (stackCount) / 2; i++)
        {
            float local_phi = Mathf.PI / 2f * (float)i / (float)(stackCount - 1) * 2;
            float ff = Mathf.Pow(1.5f * a, 1.5f) * Mathf.Sin(local_phi);
            float rr = a * C1 * Mathf.Asin(ff) / (ff / 3.0f);

            _ro[i] = rr * Mathf.Sin(local_phi) / a * C2;
            _ro[stackCount - 1 - i] = _ro[i];

            _y[i] = rr * Mathf.Cos(local_phi) / a * C2;
            _y[stackCount - 1 - i] = -_y[i];
        }

        _y[(stackCount - 1) / 2] = 0.0f;
        float f = Mathf.Pow(1.5f * a, 1.5f);
        float r = a * C1 * Mathf.Asin(f) / (f / 3.0f);
        _ro[(stackCount - 1) / 2] = r / a * C2;




        {// if sector/stack count changes
            sectorStep = 2 * Mathf.PI / sectorCount;
            stackStep = Mathf.PI / stackCount;
        }


        theta = 0;
        phi = 0;

        float x, y, z;
        float nx, ny, nz, lengthInv = 1 / radius;
        float s, t;

        for (int i = 0; i <= stackCount; i++)
        {
            phi = Mathf.PI / 2 - i * stackStep; // starting from pi/2 to -pi/2
            // ro = radius * Mathf.Cos(phi);
            // y = radius * Mathf.Sin(phi); // up axis

            for (int j = 0; j <= sectorCount; j++)
            {
                theta = j * sectorStep;
                z = _ro[i] * Mathf.Cos(theta) * radius; // forward axis
                x = _ro[i] * Mathf.Sin(theta) * radius; // right axis
                y = _y[i] * radius;
                meshData.vertices[j + i * (stackCount)] = (new Vector3(x, y, z));
                nx = x * lengthInv;
                ny = y * lengthInv;
                nz = z * lengthInv;
                meshData.normals[j + i * (stackCount)] = (new Vector3(nx, ny, nz));

                s = j / sectorCount;
                t = i / stackCount;
                meshData.texCoords[j + i * (stackCount - 1)] = (new Vector2(s, t));
            }
        }




        //////////////////////////////
        // Generating indices
        //////////////////////////

        int k1;
        int k2;
        for (int i = 0; i < stackCount; i++)
        {
            k1 = i * (sectorCount + 1); // beginning of current stack
            k2 = k1 + sectorCount + 1; // beginning of next stack;

            for (int j = 0; j < sectorCount; j++, k1++, k2++)
            {
                // 2 triangles per sector excluding first and last stacks
                // k1 => k2 => k1+1
                if (i != 0)
                {
                    meshData.indices.Add(k1);
                    meshData.indices.Add(k2);
                    meshData.indices.Add(k1 + 1);
                }
                // k1+1 => k2 => k2+1
                if (i != (stackCount - 1))
                {
                    meshData.indices.Add(k1 + 1);
                    meshData.indices.Add(k2);
                    meshData.indices.Add(k2 + 1);
                }
            }
        }

        return meshData;
    }






    /////////////////////////////////////////
    // Fast rotator deformation algorithme
    // Multithreaded & Burst compiled
    ////////////////////////////////////

    [BurstCompile(CompileSynchronously = true)]
    public struct SphereData : IJob
    {
        public NativeArray<float> _ro;
        public NativeArray<float> _y;
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> normals;
        public NativeArray<Vector2> texCoords;

        public int actualSectorCount;
        public int actualStackCount;
        public float radius;
        public float V;

        float sectorStep;
        float stackStep;
        float theta;
        float phi;

        public void Execute()
        {

            float C1 = Mathf.Sin(1f / 3f);
            float C2 = C1 * Mathf.PI;
            float a = 2f / 3f * V;

            _ro[0] = 0;
            _y[0] = 1;
            _ro[actualStackCount - 1] = 0;
            _y[actualStackCount - 1] = -1;

            for (int i = 1; i < (actualStackCount) / 2; i++)
            {
                float phi = Mathf.PI / 2f * (float)i / (float)(actualStackCount - 1) * 2;
                float ff = Mathf.Pow(1.5f * a, 1.5f) * Mathf.Sin(phi);
                float rr = a * C1 * Mathf.Asin(ff) / (ff / 3.0f);

                _ro[i] = rr * Mathf.Sin(phi) / a * C2;
                _ro[actualStackCount - 1 - i] = _ro[i];

                _y[i] = rr * Mathf.Cos(phi) / a * C2;
                _y[actualStackCount - 1 - i] = -_y[i];
            }

            _y[(actualStackCount - 1) / 2] = 0.0f;
            float f = Mathf.Pow(1.5f * a, 1.5f);
            float r = a * C1 * Mathf.Asin(f) / (f / 3.0f);
            _ro[(actualStackCount - 1) / 2] = r / a * C2;




            {// if sector/stack count changes
                sectorStep = 2 * Mathf.PI / actualSectorCount;
                stackStep = Mathf.PI / actualStackCount;
            }

            float x, y, z;
            float nx, ny, nz, lengthInv;
            float s, t;

            theta = 0;
            phi = 0;

            lengthInv = 1 / radius;
            for (int i = 0; i <= actualStackCount; i++)
            {
                phi = Mathf.PI / 2f - i * stackStep; // starting from pi/2 to -pi/2
                                                     // ro = radius * Mathf.Cos(phi);
                                                     // y = radius * Mathf.Sin(phi); // up axis

                for (int j = 0; j <= actualSectorCount; j++)
                {
                    theta = j * sectorStep;

                    z = _ro[i] * Mathf.Cos(theta) * radius; // forward axis
                    x = _ro[i] * Mathf.Sin(theta) * radius; // right axis
                    y = _y[i] * radius;
                    vertices[j + i * (actualStackCount)] = (new Vector3(x, y, z));

                    nx = x * lengthInv;
                    ny = y * lengthInv;
                    nz = z * lengthInv;
                    normals[j + i * (actualStackCount)] = (new Vector3(nx, ny, nz));

                    s = j / actualSectorCount;
                    t = i / actualStackCount;
                    texCoords[j + i * (actualStackCount)] = (new Vector2(s, t));
                }
            }
        }
    }
}