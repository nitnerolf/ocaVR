using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer))]
public class OcaFastRotator : OcaInteractable
{
    [Range(1.01f, 10f)] public float velocity;
    [Range(.1f, 3f)] public float radius;
    [Min(3)] public int sectorCount;
    [Range(1000f, 16000f)] public float temperature = 3000f;
    [Range(0.01f, 1f)] public float u;

    SphereData _collisionMeshData;
    Mesh _collisionMesh;
    MeshCollider _meshCollider;

    Mesh _mesh;
    MeshFilter _meshFilter;
    Material _material;
    SphereData _sphereMeshData;
    Shader _quadraticLD;
    Shader _linearLD;

    OcaFastRotator(int sectorCount, float initialVelocity, float radius)
    {
        this.sectorCount = sectorCount;
        this.radius = radius;
        this.velocity = initialVelocity;
    }


    float _prevRadius;
    float _prevVelocity;

    void Start()
    {
        _prevRadius = radius;
        _material = GetComponent<Renderer>().material;
        _quadraticLD = Shader.Find("Example/QuadraticLD");
        _linearLD = Shader.Find("Example/LinearLD");

        TryGetComponent<MeshFilter>(out _meshFilter);
        TryGetComponent<MeshCollider>(out _meshCollider);

        _meshCollider.cookingOptions = MeshColliderCookingOptions.UseFastMidphase;
        _mesh = new Mesh();
        _collisionMesh = new Mesh();

        Generate(out _sphereMeshData, sectorCount, 1, 1);
        _mesh.SetVertices(_sphereMeshData.vertices);
        _mesh.SetIndices(_sphereMeshData.indices.AsArray(), MeshTopology.Triangles, 0);
        _mesh.SetNormals(_sphereMeshData.normals);
        _mesh.SetUVs(0, _sphereMeshData.texCoords);
        _meshFilter.mesh = _mesh;
        _sphereMeshData.Dispose();

        Generate(out _collisionMeshData, 8, velocity, 1);
        _collisionMesh.SetVertices(_collisionMeshData.vertices);
        _collisionMesh.SetIndices(_collisionMeshData.indices.AsArray(), MeshTopology.Triangles, 0);
        _meshCollider.sharedMesh = _collisionMesh;
        _collisionMeshData.Dispose();


        /*
            Contrainer for fields of this class we want to expose as parameters in-game
            see OcaControllerHUD
        */
        HUDElementDescriptors.Add(new HUDElementDescriptor(ElementType.Label, "sectorCount", "Sectors"));
        HUDElementDescriptors.Add(new HUDElementDescriptor(ElementType.Slider, "radius"));
        HUDElementDescriptors.Add(new HUDElementDescriptor(ElementType.Slider, "velocity"));
        HUDElementDescriptors.Add(new HUDElementDescriptor(ElementType.Slider, "temperature"));
        HUDElementDescriptors.Add(new HUDElementDescriptor(ElementType.Slider, "u", "Darkening"));

    }


    static void Generate(out SphereData _sphereMeshData, int sectorCount, float velocity, float radius)
    {
        _sphereMeshData = new SphereData();
        _sphereMeshData.actualSectorCount = sectorCount;
        _sphereMeshData.actualStackCount = _sphereMeshData.actualSectorCount + 1;
        _sphereMeshData.ro = new NativeArray<float>(_sphereMeshData.actualStackCount + 1, Allocator.Persistent);
        _sphereMeshData._y = new NativeArray<float>(_sphereMeshData.actualStackCount + 1, Allocator.Persistent);

        int vertexCount = (_sphereMeshData.actualSectorCount + 1) * (_sphereMeshData.actualStackCount + 1);

        _sphereMeshData.vertices = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        _sphereMeshData.normals = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
        _sphereMeshData.texCoords = new NativeArray<Vector2>(vertexCount, Allocator.Persistent);
        _sphereMeshData.indices = new NativeList<int>(sectorCount * sectorCount * 6, Allocator.Persistent);

        _sphereMeshData.radius = 1;
        _sphereMeshData.V = Mathf.Log10(velocity);
        _sphereMeshData.Schedule().Complete();
    }

    void Update()
    {
        Vector3 camToObjectDirection = (transform.position - Camera.main.transform.position).normalized;

        // _material.SetColor("temperature", Mathf.CorrelatedColorTemperatureToRGB(temperature));
        _material.SetFloat("temperature", temperature);
        _material.SetVector("cameraLookDirection", camToObjectDirection);
        _material.SetFloat("u", u);


        bool _changed = (_prevVelocity != velocity);

        if (_prevRadius != radius)
        {
            _prevRadius = radius;
            gameObject.transform.localScale = new Vector3(radius, radius, radius);
        }

        if (_changed)
        {
            _prevVelocity = velocity;

            Generate(out _sphereMeshData, sectorCount, 1, 1);

            _sphereMeshData.radius = 1;
            _sphereMeshData.V = Mathf.Log10(velocity);
            _sphereMeshData.Schedule().Complete();

            _mesh.SetVertices(_sphereMeshData.vertices);
            _mesh.SetIndices(_sphereMeshData.indices.AsArray(), MeshTopology.Triangles, 0);
            _mesh.SetNormals(_sphereMeshData.normals);
            _mesh.SetUVs(0, _sphereMeshData.texCoords);
            _meshFilter.mesh = _mesh;

            _sphereMeshData.Dispose();
        }
    }




    /////////////////////////////////////////
    // Fast rotator deformation algorithme
    // Multithreaded & Burst compiled
    ////////////////////////////////////

    [BurstCompile(CompileSynchronously = true)]
    public struct SphereData : IJob
    {
        public NativeArray<float> ro;
        public NativeArray<float> _y;
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> normals;
        public NativeArray<Vector2> texCoords;
        public NativeList<int> indices;

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

            ro[0] = 0;
            _y[0] = 1;
            ro[actualStackCount - 1] = 0;
            _y[actualStackCount - 1] = -1;

            for (int i = 1; i < (actualStackCount) / 2; i++)
            {
                float phi = Mathf.PI / 2f * (float)i / (float)(actualStackCount - 1) * 2;
                float ff = Mathf.Pow(1.5f * a, 1.5f) * Mathf.Sin(phi);
                float rr = a * C1 * Mathf.Asin(ff) / (ff / 3.0f);

                ro[i] = rr * Mathf.Sin(phi) / a * C2;
                ro[actualStackCount - 1 - i] = ro[i];

                _y[i] = rr * Mathf.Cos(phi) / a * C2;
                _y[actualStackCount - 1 - i] = -_y[i];
            }

            _y[(actualStackCount - 1) / 2] = 0.0f;
            float f = Mathf.Pow(1.5f * a, 1.5f);
            float r = a * C1 * Mathf.Asin(f) / (f / 3.0f);
            ro[(actualStackCount - 1) / 2] = r / a * C2;


            sectorStep = 2 * Mathf.PI / actualSectorCount;
            stackStep = Mathf.PI / actualStackCount;

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

                    z = ro[i] * Mathf.Cos(theta) * radius; // forward axis
                    x = ro[i] * Mathf.Sin(theta) * radius; // right axis
                    y = _y[i] * radius;
                    vertices[j + i * (actualStackCount)] = (new Vector3(x, y, z));

                    nx = x * lengthInv;
                    ny = y * lengthInv;
                    nz = z * lengthInv;
                    normals[j + i * (actualStackCount)] = (new Vector3(nx, ny, nz));

                    s = (float)j / actualSectorCount;
                    t = (float)i / actualStackCount;
                    texCoords[j + i * (actualStackCount)] = (new Vector2(s, t));
                }
            }


            //////////////////////////////
            // Generating indices
            //////////////////////////

            int k1;
            int k2;
            for (int i = 0; i < actualStackCount; i++)
            {
                k1 = i * (actualSectorCount + 1); // beginning of current stack
                k2 = k1 + actualSectorCount + 1; // beginning of next stack;

                for (int j = 0; j < actualSectorCount; j++, k1++, k2++)
                {
                    // 2 triangles per sector excluding first and last stacks
                    // k1 => k2 => k1+1
                    if (i != 0)
                    {
                        indices.Add(k1);
                        indices.Add(k2);
                        indices.Add(k1 + 1);
                    }
                    // k1+1 => k2 => k2+1
                    if (i != (actualStackCount - 1))
                    {
                        indices.Add(k1 + 1);
                        indices.Add(k2);
                        indices.Add(k2 + 1);
                    }
                }
            }
        }

        public void Dispose()
        {
            ro.Dispose();
            _y.Dispose();
            vertices.Dispose();
            indices.Dispose();
            normals.Dispose();
            texCoords.Dispose();
        }
    }
}
