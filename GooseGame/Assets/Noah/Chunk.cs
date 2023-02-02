using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;

public class Chunk : MonoBehaviour
{
    Vector3[] verticies;
    int[] triangles;

    public int seed;
    public const int chunkSize = 128;

    #region SerializeField Variables
    [Header("variables are shown for debbugging purposes")]
    [Header("and does not do anything if changed")]

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

    [NonSerialized]
    public Transform player;
    #endregion

    [NonSerialized]
    public ChunkConnection connection;


    Mesh[] simplify = new Mesh[5];
    int selected = 0;
    int lastSelected = -1;

    [SerializeField]
    float distance;

    [SerializeField]
    float distanceMulitplier;

    private void Awake()
    {
        connection = new ChunkConnection(transform);
    }

    bool created = false;

    private void Update()
    {
        distance = Vector3.Distance(transform.position, player.position);
        SwapMesh(distance);
    }

    private void SwapMesh(float distance)
    {
        for(int i  = simplify.Length -1; i >= 0; i--)
        {
            if (distance >= i * i * distanceMulitplier)
            {
                selected = i;
            }
            else 
            { 
                continue; 
            }

            if (selected == lastSelected) return;

            selected = i;
            meshSimplification = selected;
            if (simplify[i] == null) CreateShape();
            transform.GetComponent<MeshFilter>().mesh = simplify[meshSimplification];
            lastSelected = selected;
            return;
        }
    }

    public void SetChunkInformation(int seed, float scale, int octaves, float persistance, float lacunarity, float heightMultiplier, AnimationCurve heightCurve, int meshSimplification)
    {
        this.seed = seed;
        this.scale = scale;
        this.octaves = octaves;
        this.persistance = persistance;
        this.lacunarity = lacunarity;
        this.heightMultiplier = heightMultiplier;
        this.heightCurve = heightCurve;
        this.meshSimplification = meshSimplification;
    }
    public void CreateShape()
    {
        int meshSimplificationValue = (int)Mathf.Pow(2f, meshSimplification);
        int verticiesRowAmount = (chunkSize / meshSimplificationValue) + 1;
        verticies = new Vector3[verticiesRowAmount * verticiesRowAmount];

        Vector2 offset = new Vector2(transform.position.x, transform.position.z);

        float[,] heightMap = Noise.GenerateNoiseMap(chunkSize + 1, chunkSize + 1, seed, scale, octaves, persistance, lacunarity, offset, 1);

        for (int z = 0, i = 0; z <= chunkSize; z += meshSimplificationValue)
        {
            for (int x = 0; x <= chunkSize; x += meshSimplificationValue)
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

        if (simplify[meshSimplification] == null)
        {
            simplify[meshSimplification] = new Mesh();
            UpdateMesh(ref simplify[meshSimplification]);
        }
    }

    void UpdateMesh(ref Mesh mesh)
    {
        mesh.Clear();

        mesh.vertices = verticies;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }
}
