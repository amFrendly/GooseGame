using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;

    Vector3[] verticies;
    int[] triangles;

    public int seed;

    public int xSize;
    public int zSize;

    public float scale;
    public float persistance;
    public float lacunarity;
    public int octaves;
    public float heightMultiplier;
    public AnimationCurve heightCurve;

    public Vector2 offset;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

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

        offset = new Vector2(transform.position.x, transform.position.z);

        float[,] heightMap = Noise.GenerateNoiseMap(xSize + 1, zSize + 1, seed, scale, octaves, persistance, lacunarity, offset);

        for (int z = 0, i = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = heightCurve.Evaluate(heightMap[x, z]) * heightMultiplier;

                verticies[i] = new Vector3(x, y, z);
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

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = verticies;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }

    private void OnValidate()
    {
        if (xSize < 1) xSize = 1;
        if (zSize < 1) zSize = 1;

        if (scale > 100) scale = 100;
        if (scale < 5) scale = 5;

        if (persistance > 1) persistance = 1;
        if (persistance < 0) persistance = 0;

        if (lacunarity > 5) lacunarity = 10;
        if (lacunarity < 1) lacunarity = 1;

        if (octaves < 0) octaves = 0;
    }
}
