using System;
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

    public const int chunkSize = 128;

    [SerializeField]
    float scale;

    [SerializeField]
    [Range(0, 1)]
    float persistance;

    [SerializeField]
    [Range(1, 5)]
    float lacunarity;

    [SerializeField]
    int octaves;

    [SerializeField]
    float heightMultiplier;

    [SerializeField]
    AnimationCurve heightCurve;

    [SerializeField]
    [Range(0, 4)]
    int meshSimplification = 1;

    [SerializeField]
    Vector2 offset;

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
        int meshSimplificationValue = (int)Mathf.Pow(2f, meshSimplification);
        int verticiesRowAmount = (chunkSize / meshSimplificationValue) + 1;
        verticies = new Vector3[verticiesRowAmount * verticiesRowAmount];

        offset = new Vector2(transform.position.x, transform.position.z);

        float[,] heightMap = Noise.GenerateNoiseMap(chunkSize + 1, chunkSize + 1, seed, scale, octaves, persistance, lacunarity, offset);

        for (int z = 0, i = 0; z <= chunkSize; z+= meshSimplificationValue)
        {
            for (int x = 0; x <= chunkSize; x+= meshSimplificationValue)
            {
                float y = heightCurve.Evaluate(heightMap[x, z]) * heightMultiplier;

                verticies[i] = new Vector3(x, y, z);
                i++;
            }
        }

        int quadsRowAmount = chunkSize / meshSimplificationValue;
        triangles = new int[quadsRowAmount * quadsRowAmount * 6];
        for (int z = 0, quad = 0, tri = 0; z < quadsRowAmount; z++)
        {
            for (int x = 0; x < quadsRowAmount; x++)
            {
                triangles[tri + 0] = quad + 0;
                triangles[tri + 1] = quad + quadsRowAmount + 1;
                triangles[tri + 2] = quad + 1;

                triangles[tri + 3] = quad + 1;
                triangles[tri + 4] = quad + quadsRowAmount + 1;
                triangles[tri + 5] = quad + quadsRowAmount + 2;

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
        if (scale > 100) scale = 100;
        if (scale < 5) scale = 5;

        if (octaves < 0) octaves = 0;
    }
}
