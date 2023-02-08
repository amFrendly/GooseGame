using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Chunk : MonoBehaviour
{
    [SerializeField]
    [Range(0, 4)]
    int meshSimplification = 1;

    int meshStartSimplifiaction = 2;

    [NonSerialized]
    public Transform player;

    Mesh[] simplify = new Mesh[3];
    int selected = 0;
    int lastSelected = -1;

    [SerializeField]
    float[] swapDetailOnDistance;

    [SerializeField]
    List<GameObject> tryFit = new List<GameObject>();

    public ChunkData chunkData;
    public NoiseData noiseData;

    bool once = true;

    private void Update()
    {
        CheckThreadQueue();

        if (!once) return;
        if (chunkData.heightMap == null) return;
        for (int i = tryFit.Count - 1; i >= 0; i--)
        {
            GameObject tryFitItem = tryFit[i];
            Vector2 size = tryFitItem.GetComponent<MeshCollider>().sharedMesh.bounds.size;
            bool fit = chunkData.FindFlatSPace(out Vector3? position, size, noiseData.heightMultiplier, 0.0005f);
            if (fit)
            {
                Debug.Log($"The Size {"X:" + size.x + "Z:" + size.y + (fit ? $" Fit At Position {"X:" + position.Value.x + " Y:" + position.Value.y + "Z:" + position.Value.z}" : "Does Not Fit")}");
                Instantiate(tryFitItem, transform).transform.position = transform.position + position.Value;
            }
        }
        once = false;
    }

    private void Awake()
    {
        chunkDataInfoQueue = new Queue<ChunkThreadInfo<ChunkData>>();
        chunkMeshInfoQueue = new Queue<ChunkThreadInfo<MeshData?>>();
    }

    private void OnValidate()
    {
        if (swapDetailOnDistance.Length > 3)
        {
            swapDetailOnDistance = new float[3];
        }
    }

    public void SwapMesh(float distance)
    {
        for (int i = simplify.Length - 1; i >= 0; i--)
        {
            if (distance >= swapDetailOnDistance[i])
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
    public void SetChunkInformation(int seed, float scale, int octaves, float persistance, float lacunarity, float heightMultiplier, AnimationCurve heightCurve)
    {
        Vector2 offset = new Vector2(transform.position.x, transform.position.z);

        noiseData = new NoiseData(seed, scale, octaves, persistance, lacunarity, heightMultiplier, heightCurve, offset);
        meshSimplification = meshStartSimplifiaction;
        RequestChunkData(OnChunkDataRecieved);
    }

    #region Collider
    const int colliderSimplify = 0;
    public void OnColliderMeshRecieved(MeshData? meshData)
    {
        MeshCollider meshCollider = transform.GetComponent<MeshCollider>();

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

        if (meshCollider.sharedMesh == null)
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
    #endregion
    #region Mesh Creation
    private Vector3[] GetVerticies(float[,] heightMap, int meshSimplificationValue)
    {
        int verticiesRowAmount = (ChunkLoader.chunkSize / meshSimplificationValue) + 1;
        Vector3[] verticies = new Vector3[verticiesRowAmount * verticiesRowAmount];
        for (int z = 0, i = 0; z <= ChunkLoader.chunkSize; z += meshSimplificationValue)
        {
            for (int x = 0; x <= ChunkLoader.chunkSize; x += meshSimplificationValue)
            {
                lock (noiseData.heightCurve)
                {
                    float y = heightMap[x, z] * noiseData.heightMultiplier;
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
        float[,] heightMap = Noise.GenerateNoiseMap(ChunkLoader.chunkSize + 1, ChunkLoader.chunkSize + 1, noiseData.seed, noiseData.scale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, noiseData.heightCurve, noiseData.offset);
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
    public bool FindFlatSPace(out Vector3? chunkPosition, Vector3 size, float heightMultiplier, float tolerance)
    {
        chunkPosition = new Vector3();
        for (int y = 0; y < heightMap.GetLength(1) - size.y; y++)
        {
            for (int x = 0; x < heightMap.GetLength(0) - size.x; x++)
            {
                float height = heightMap[x, y] * heightMultiplier;
                Vector2 testPosition = new Vector2(x, y);
                if (FlatSpace(testPosition, size, tolerance))
                {
                    chunkPosition = new Vector3(testPosition.x, height, testPosition.y) + new Vector3(size.x / 2, 0, size.z / 2);
                    return true;
                }
            }
        }
        return false;
    }
    private bool FlatSpace(Vector2 position, Vector2 fitSpace, float tolerance)
    {
        int startX = (int)position.x;
        int startY = (int)position.y;

        int endX = (int)fitSpace.x + startX;
        int endY = (int)fitSpace.y + startY;

        float startHeight = Mathf.Abs(heightMap[startX, startY]);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                float testHeight = Mathf.Abs(heightMap[x, y]);
                float testTolerance = Mathf.Abs(testHeight - startHeight);
                if (testTolerance > tolerance)
                {
                    return false;
                }
            }
        }
        return true;
    }
}