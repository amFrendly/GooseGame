using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    MeshCollider meshCollider;

    Vector3[] verticies;
    int[] triangles;

    public int xSize;
    public int zSize;

    public float spacing;
    public float height;

    public float lacunarity;
    public float persistance;
    public int octaves;


    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        CreateShape();
        UpdateMesh();
    }

    private void Update()
    {
        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        verticies = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int z = 0, i = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = GetOctaves(octaves, 0.3f, lacunarity, persistance, x, z) * height;
                verticies[i] = new Vector3(x * spacing, y, z * spacing);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];
        for (int z = 0, quad = 0, tri = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tri + 0] = quad + 0;
                triangles[tri + 1] = quad + xSize + 1;
                triangles[tri + 2] = quad + 1;

                triangles[tri + 3] = quad + 1;
                triangles[tri + 4] = quad + xSize + 1;
                triangles[tri + 5] = quad + xSize + 2;

                tri += 6;
                quad++;
            }
            quad++;
        }
    }

    float GetOctaves(int amount, float scale, float lacunarity, float persistance, int x, int z)
    {
        float noiceHeight = 0;
        float frequency = 1;
        float amplitude = 1;

        for (int i = 0; i < amount; i++)
        {
            float sampleX = x * scale * frequency;
            float sampleZ = z * scale * frequency;

            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ);
            noiceHeight += perlinValue * amplitude;

            frequency *= lacunarity;
            amplitude *= persistance;
        }

        return noiceHeight;
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = verticies;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;
    }
}
