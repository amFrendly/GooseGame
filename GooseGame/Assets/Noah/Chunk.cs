using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

public class Chunk : MonoBehaviour
{
    [SerializeField]
    [Range(0, 4)]
    int meshSimplification = 1;

    [SerializeField]
    int meshStartSimplifiaction;

    [NonSerialized]
    public Transform player;

    Mesh[] simplify = new Mesh[5];
    int selected = 0;
    int lastSelected = -1;

    [SerializeField]
    float[] levelOfDetailDistances;

    public ChunkData chunkData;
    public NoiseData noiseData;

    private void Update()
    {
        CheckThreadQueue();
    }

    private void OnValidate()
    {
        if (levelOfDetailDistances.Length > 5)
        {
            levelOfDetailDistances = new float[5];
        }
    }

    public void SwapMesh(float distance)
    {
        for (int i = simplify.Length - 1; i >= 0; i--)
        {
            if (distance >= levelOfDetailDistances[i])
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

            if (simplify[i] == null)
            {
                RequestMesh(OnMeshRecieved);
            }
            else
            {
                transform.GetComponent<MeshFilter>().mesh = simplify[meshSimplification];
            }
            lastSelected = selected;
            return;
        }
    }

    const int colliderSimplify = 0;
    public void OnColliderMeshRecieved(MeshData? meshData)
    {
        MeshCollider meshCollider = transform.GetComponent<MeshCollider>();
        meshCollider.enabled = true;

        if (meshCollider.sharedMesh != null) return;

        if (meshData != null)
        {
            Mesh mesh = new Mesh();
            UpdateMesh(ref mesh, meshData.Value);
            meshCollider.sharedMesh = mesh;
        }
        else
        {
            meshCollider.sharedMesh = simplify[colliderSimplify];
        }
    }

    public void ActivateCollder()
    {
        MeshCollider meshCollider = transform.GetComponent<MeshCollider>();
        meshCollider.enabled = true;

        if(meshCollider.sharedMesh == null)
        {
            int oldMeshSimplification = meshSimplification;
            meshSimplification = colliderSimplify;
            RequestMesh(OnColliderMeshRecieved);
            meshSimplification = oldMeshSimplification;
        }

    }

    public void DeactivateCollder()
    {
        MeshCollider meshCollider = transform.GetComponent<MeshCollider>();
        meshCollider.enabled = false;
    }


    public void SetChunkInformation(int seed, float scale, int octaves, float persistance, float lacunarity, float heightMultiplier, AnimationCurve heightCurve)
    {
        Vector2 offset = new Vector2(transform.position.x, transform.position.z);

        noiseData = new NoiseData(seed, scale, octaves, persistance, lacunarity, heightMultiplier, heightCurve, offset);
        meshSimplification = meshStartSimplifiaction;
        RequestChunkData(OnChunkDataRecieved);
    }

    #region Mesh Creation
    private Vector3[] GetVerticies(float[,] heightMap, int meshSimplificationValue)
    {
        AnimationCurve heightCurve = new AnimationCurve(noiseData.heightCurve.keys);
        int verticiesRowAmount = (ChunkLoader.chunkSize / meshSimplificationValue) + 1;
        Vector3[] verticies = new Vector3[verticiesRowAmount * verticiesRowAmount];
        for (int z = 0, i = 0; z <= ChunkLoader.chunkSize; z += meshSimplificationValue)
        {
            for (int x = 0; x <= ChunkLoader.chunkSize; x += meshSimplificationValue)
            {
                lock (noiseData.heightCurve)
                {
                    float y = heightCurve.Evaluate(heightMap[x, z]) * noiseData.heightMultiplier;
                    verticies[i] = new Vector3(x, y, z);
                    i++;
                }
            }
        }
        return verticies;
    }
    private int[] GetTriangles(int quadsRowAmount)
    {
        int[] triangles = new int[quadsRowAmount * quadsRowAmount * 6];
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
        return triangles;
    }
    private void UpdateMesh(ref Mesh mesh, MeshData meshData)
    {
        mesh.Clear();

        mesh.vertices = meshData.vertices;
        mesh.triangles = meshData.triangles;

        mesh.RecalculateNormals();
    }
    private MeshData? CreateShape()
    {
        if (chunkData.heightMap == null) return null;
        if (simplify[meshSimplification] != null) return null;

        int meshSimplificationValue = (int)Mathf.Pow(2f, meshSimplification + meshStartSimplifiaction);
        Vector3[] verticies = GetVerticies(chunkData.heightMap, meshSimplificationValue);

        int quadsRowAmount = ChunkLoader.chunkSize / meshSimplificationValue;
        int[] triangles = GetTriangles(quadsRowAmount);

        return new MeshData(verticies, triangles);
    }
    #endregion
    #region Threading
    Queue<ChunkThreadInfo<ChunkData>> chunkDataInfoQueue = new Queue<ChunkThreadInfo<ChunkData>>();
    Queue<ChunkThreadInfo<MeshData?>> chunkMeshInfoQueue = new Queue<ChunkThreadInfo<MeshData?>>();

    private ChunkData GetChunkData()
    {
        float[,] heightMap = Noise.GenerateNoiseMap(ChunkLoader.chunkSize + 1, ChunkLoader.chunkSize + 1, noiseData.seed, noiseData.scale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, noiseData.offset);
        ChunkData chunkData = new ChunkData(heightMap);
        return chunkData;
    }
    private void RequestChunkData(Action<ChunkData> callback)
    {
        ThreadStart threadStart = delegate
        {
            ChunkDataThread(callback);
        };
        new Thread(threadStart).Start();
    }
    private void ChunkDataThread(Action<ChunkData> callback)
    {
        ChunkData chunkData = GetChunkData();
        lock (chunkDataInfoQueue)
        {
            chunkDataInfoQueue.Enqueue(new ChunkThreadInfo<ChunkData>(callback, chunkData));
        }
    }
    private void OnChunkDataRecieved(ChunkData chunkData)
    {
        this.chunkData = chunkData;

        RequestMesh(OnMeshRecieved);
    }

    private void RequestMesh(Action<MeshData?> callback)
    {
        //Debug.Log($"Running on Thread \"{Thread.CurrentThread.Name}\"");
        ThreadStart threadStart = delegate
        {
            MeshThread(callback);
        };
        new Thread(threadStart).Start();
    }
    private void OnMeshRecieved(MeshData? meshData)
    {
        if (meshData != null)
        {
            simplify[meshSimplification] = new Mesh();
            UpdateMesh(ref simplify[meshSimplification], meshData.Value);
        }

        transform.GetComponent<MeshFilter>().mesh = simplify[meshSimplification];
    }
    private void MeshThread(Action<MeshData?> callback)
    {
        MeshData? meshData = CreateShape();

        lock (chunkDataInfoQueue)
        {
            chunkMeshInfoQueue.Enqueue(new ChunkThreadInfo<MeshData?>(callback, meshData));
        }
    }

    private void CheckThreadQueue()
    {
        for (int i = 0; i < chunkDataInfoQueue.Count; i++)
        {
            ChunkThreadInfo<ChunkData> threadInfo = chunkDataInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }

        for (int i = 0; i < chunkMeshInfoQueue.Count; i++)
        {
            ChunkThreadInfo<MeshData?> threadInfo = chunkMeshInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }
    }
    struct ChunkThreadInfo<T>
    {
        public Action<T> callback;
        public T parameter;

        public ChunkThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
    #endregion
}

public struct NoiseData
{
    public int seed;
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public float heightMultiplier;
    public AnimationCurve heightCurve;
    public Vector2 offset;

    public NoiseData(int seed, float scale, int octaves, float persistance, float lacunarity, float heightMultiplier, AnimationCurve heightCurve, Vector2 offset)
    {
        this.seed = seed;
        this.scale = scale;
        this.octaves = octaves;
        this.persistance = persistance;
        this.lacunarity = lacunarity;
        this.heightMultiplier = heightMultiplier;
        this.heightCurve = heightCurve;
        this.offset = offset;
    }
}
public struct MeshData
{
    public Vector3[] vertices;
    public int[] triangles;

    public MeshData(Vector3[] verticies, int[] triangles)
    {
        this.vertices = verticies;
        this.triangles = triangles;
    }
}
public struct ChunkData
{
    [NonSerialized]
    public float[,] heightMap;

    public ChunkData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}