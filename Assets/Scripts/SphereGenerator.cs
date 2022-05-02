using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

// sphere generation fails when sectorCount > 255
// vertices from bottom-up go missing increasingly

public class SphereGenerator : MonoBehaviour
{
    List<Vector3> vertices;
    List<int> indices;
    List<Vector3> normals;
    List<Vector2> texCoords;

    Mesh mesh;
    Material mat;
    MeshFilter meshFilter;

    void Awake()
    {
        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        texCoords = new List<Vector2>();
        indices = new List<int>();

        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<BoxCollider>();
        meshRenderer.material = Resources.Load<Material>("Materials/Prop Green");
        meshFilter = gameObject.AddComponent<MeshFilter>();
    }





    int stackCount;

    [Range(16, 255)] public int sectorCount;
    [Range(0.01f, 1f)] public float V = 1;
    [Min(0)] public float radius;

    float sectorStep;
    float stackStep;
    float theta;
    float phi;
    ASphere sphere;

    void GenerateSphere()
    {
        vertices.Clear();
        normals.Clear();
        texCoords.Clear();
        indices.Clear();

        vertices.TrimExcess();
        normals.TrimExcess();
        texCoords.TrimExcess();
        indices.TrimExcess();


        ///////////////////////////////
        // Generating vertices
        //
        //
        // x = (r*cos(phi))*cos(theta)
        // y = (r*cos(phi))*sin(theta)
        // z = r*sin(phi)
        //
        // theta = 2pi*(sectorStep/sectorCount)
        // phi = pi/2*-pi*(stackStep/stackCount)
        ////////////////////////////
        stackCount = sectorCount + 1;
        sectorStep = 2 * Mathf.PI / sectorCount;
        stackStep = Mathf.PI / stackCount;

        theta = 0;
        phi = 0;

        float x, y, z, ro;
        float nx, ny, nz, lengthInv = 1 / radius;
        float s, t;

        for (int i = 0; i <= stackCount; i++)
        {
            phi = Mathf.PI / 2 - i * stackStep; // starting from pi/2 to -pi/2
            ro = radius * Mathf.Cos(phi);
            y = radius * Mathf.Sin(phi); // up axis

            for (int j = 0; j <= sectorCount; j++)
            {
                theta = j * sectorStep;
                z = ro * Mathf.Cos(theta); // forward axis
                x = ro * Mathf.Sin(theta); // right axis
                vertices.Add(new Vector3(x, y, z));
                nx = x * lengthInv;
                ny = y * lengthInv;
                nz = z * lengthInv;
                normals.Add(new Vector3(nx, ny, nz));

                s = j / sectorCount;
                t = i / stackCount;
                texCoords.Add(new Vector2(s, t));
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
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }
                // k1+1 => k2 => k2+1
                if (i != (stackCount - 1))
                {
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k2 + 1);
                }
            }
        }

        mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = texCoords.ToArray();


        meshFilter.mesh = mesh;
        if (gameObject.GetComponent<BoxCollider>() != null)
        {
            Destroy(gameObject.GetComponent<BoxCollider>());
            BoxCollider meshCollider = gameObject.AddComponent<BoxCollider>();
            // meshCollider.convex = true;
        }
    }





    int actualStackCount;
    int actualSectorCount;

    public void Start()
    {
        GenerateSphere();
        actualSectorCount = sectorCount;
        actualStackCount = stackCount;
    }

#if true
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            GenerateSphere();
            actualSectorCount = sectorCount;
            actualStackCount = stackCount;
        }

        {
            float[] _ro = new float[actualStackCount + 1];
            float[] _y = new float[actualStackCount + 1];

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
                // Debug.Log("C1: " + C1 + " asin: " + Mathf.Asin(ff) + " ff" + (ff));

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


            // mesh.triangles = indices.ToArray();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = texCoords.ToArray();
            meshFilter.mesh = mesh;
        }
    }
#endif
}
