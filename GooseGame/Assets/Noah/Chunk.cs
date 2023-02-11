using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class Chunk : MonoBehaviour
{
    [SerializeField]
    [Range(0, 4)]
    int meshSimplification = 1;

    int meshStartSimplifiaction = 0;

    [NonSerialized]
    public Transform player;

    Mesh[] simplify = new Mesh[3];
    int selected = 0;
    int lastSelected = -1;

    [SerializeField]
    float[] swapDetailOnDistance;

    [SerializeField]
    List<ChunkObject> tryFit = new List<ChunkObject>();

    public ChunkData chunkData;
    public NoiseData noiseData;

    bool once = true;

    private void Update()
    {
        CheckThreadQueue();

        if (!once) return;
        if (simplify[meshSimplification] == null) return;
        //chunkData.GetFlatSpaces(simplify[meshSimplification], meshSimplification + meshStartSimplifiaction, 0.001f);

        //AddObjects();
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

    public Vector3[] CalculateNormals(Vector3[] verticies, int[] triangles)
    {
        Vector3[] vertexNormals = new Vector3[verticies.Length];
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int triangleIndex = i * 3;
            int vertexIndexA = triangles[triangleIndex];
            int vertexIndexB = triangles[triangleIndex + 1];
            int vertexIndexC = triangles[triangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexIndexA, vertexIndexB, vertexIndexC, verticies);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndicies(int triangleA, int triangleB, int triangleC, Vector3[] verticies)
    {
        Vector3 pointA = verticies[triangleA];
        Vector3 pointB = verticies[triangleB];
        Vector3 pointC = verticies[triangleC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void AddObjects()
    {
        for (int i = tryFit.Count - 1; i >= 0; i--)
        {
            //chunkData.AddObject(tryFit[i], transform, noiseData.heightMultiplier);

            int meshSimplificationValue = (int)Mathf.Pow(2, meshSimplification + meshStartSimplifiaction);
            chunkData.AddChunkObject(tryFit[i], transform, noiseData.heightMultiplier, meshSimplificationValue);
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
        meshSimplification = 0;
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

        DebugTime.Start();
        mesh.normals = CalculateNormals(meshData.vertices, meshData.triangles);
        //mesh.RecalculateNormals();
        DebugTime.Stop();
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

    public void CheckThreadQueue()
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
    [NonSerialized]
    public Dictionary<GameObject, List<GameObject>> chunkObjects;

    public ChunkData(float[,] heightMap)
    {
        this.heightMap = heightMap;
        chunkObjects = new Dictionary<GameObject, List<GameObject>>();

        flatSpaceLeft = new Dictionary<Vector2, Vector2>();
    }
    private Dictionary<Vector2, Vector2> flatSpaceLeft;

    public void GetFlatSpaces(Mesh mesh, int meshSimlification, float tolerance)
    {
        Vector3[] normals = mesh.normals;
        int vertexAmountWidth = ChunkLoader.chunkSize / (int)Mathf.Pow(2, meshSimlification);

        for (int i = 0; i < normals.Length; i++)
        {
            int x = i % vertexAmountWidth;
            int y = Mathf.RoundToInt(i / vertexAmountWidth);
            float dot = Vector3.Dot(normals[i], Vector3.up);
            if (dot >= 1 - tolerance)
            {
                // Add flat Verticies
                Vector2 position = new Vector2(x, y);
                flatSpaceLeft.Add(position, position);
            }
            else
            {
                // skip indexes based on how far from tolerance
            }
        }
    }

    public void AddChunkObject(ChunkObject chunkObject, Transform parent, float heightMultiplier, int meshSimplificationValue)
    {
        if (chunkObjects.ContainsKey(chunkObject.gameObject))
        {
            return;
        }
        else
        {
            TryToAdd(chunkObject, parent, heightMultiplier, meshSimplificationValue);
        }
    }

    private void TryToAdd(ChunkObject chunkObject, Transform parent, float heightMultiplier, int meshSimplificationValue)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        chunkObjects.Add(chunkObject.gameObject, gameObjects);

        Vector3 size3D = chunkObject.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        size3D = new Vector3(size3D.x, 0, size3D.z);
        Vector2 size = new Vector2(Mathf.RoundToInt(size3D.x), Mathf.RoundToInt(size3D.z));

        for (int i = 0; i < chunkObject.amount; i++)
        {
            Vector2 position = new Vector2();
            bool fit = false;
            foreach (Vector2 testPosition in flatSpaceLeft.Keys)
            {
                if (FitObject(testPosition, size))
                {
                    position = testPosition;
                    fit = true;
                    break;
                }
            }

            if (fit)
            {
                RemoveFromSpaceLeft(position, size);
                // Spawn GameObject

                float worldPositionX = position.x * meshSimplificationValue;
                float worldPositionZ = position.y * meshSimplificationValue;
                float height = heightMap[(int)position.x, (int)position.y] * heightMultiplier;

                Vector3 worldPosition = parent.position + new Vector3(worldPositionX, height, worldPositionZ) + size3D / 2;

                GameObject newGameobject = GameObject.Instantiate(chunkObject.gameObject, parent);
                newGameobject.transform.position = worldPosition;
                gameObjects.Add(newGameobject);
            }
            else
            {
                return;
            }
        }
    }

    private void RemoveFromSpaceLeft(Vector2 position, Vector2 size)
    {
        for (int y = 0; y < (int)size.y; y++)
        {
            for (int x = 0; x < (int)size.x; x++)
            {
                flatSpaceLeft.Remove(position + new Vector2(x, y));
            }
        }
    }

    private bool FitObject(Vector2 position, Vector2 size)
    {
        for (int y = 0; y < (int)size.y; y++)
        {
            for (int x = 0; x < (int)size.x; x++)
            {
                if (!flatSpaceLeft.ContainsKey(position + new Vector2(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #region Wokring But Slow
    public void AddObject(ChunkObject chunkObject, Transform parent, float heightMultiplier)
    {
        if (chunkObjects.ContainsKey(chunkObject.gameObject))
        {
            return;
        }
        else
        {
            CheckToAdd(chunkObject, parent, heightMultiplier);
        }
    }
    private void CheckToAdd(ChunkObject chunkObject, Transform parent, float heightMultiplier)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        chunkObjects.Add(chunkObject.gameObject, gameObjects);
        Vector2 startAt = new Vector2();
        Vector3 size = chunkObject.gameObject.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        Vector2 endedAt = startAt;

        for (int i = 0; i < chunkObject.amount; i++)
        {

            bool found = false;
            do
            {
                found = FindFlatSpace(out Vector3? chunkPos, size, heightMultiplier, chunkObject.steepTolerance, startAt);

                if (chunkPos == null) chunkPos = new Vector3();
                Vector3 worldPosition = chunkPos.Value + parent.position + new Vector3(size.x / 2, 0, size.z / 2);

                if (FarEnoughAway(worldPosition, gameObjects, chunkObject.minimumSpacingForThis) && found)
                {
                    GameObject newGameobject = GameObject.Instantiate(chunkObject.gameObject, parent);
                    newGameobject.transform.position = worldPosition;
                    gameObjects.Add(newGameobject);
                    break; // Found a suitable place to put the gameObject hopefully...
                }
                else
                {
                    found = false;
                    Vector2 extraX = new Vector2(size.x + chunkObject.minimumSpacingForThis, 0);
                    Vector2 extraY = new Vector2(0, size.z + chunkObject.minimumSpacingForThis);
                    if (!IsOutofBounds(endedAt + extraX))
                    {
                        endedAt += extraX;
                    }
                    else if (!IsOutofBounds(new Vector2(0, endedAt.y) + extraY))
                    {
                        endedAt += extraY;
                        endedAt = new Vector2(0, endedAt.y);
                    }
                    else
                    {
                        return;
                    }
                    startAt = endedAt;
                }


            } while (found == false);

        }
    }
    private bool FarEnoughAway(Vector3 position, List<GameObject> gameObjects, float distance)
    {
        for (int i = 0; i < gameObjects.Count; i++)
        {
            Vector3 otherPosition = gameObjects[i].transform.position;
            if (Vector3.Distance(position, otherPosition) < distance)
            {
                return false;
            }
        }
        return true;
    }
    private Vector3 RoundToInts(Vector3 vector)
    {
        return new Vector3(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
    }
    public bool FindFlatSpace(out Vector3? chunkPosition, Vector3 size, float heightMultiplier, float tolerance, Vector2 startAt)
    {
        float startHeight = heightMap[(int)startAt.x, (int)startAt.y] * heightMultiplier;
        chunkPosition = new Vector3(startAt.x, startHeight, startAt.y);
        if (IsOutofBounds(startAt)) return false;
        for (int y = (int)startAt.y; y < heightMap.GetLength(1) - size.y; y++)
        {
            for (int x = (int)startAt.x; x < heightMap.GetLength(0) - size.x; x++)
            {
                Vector2 testPosition = new Vector2(x, y);
                if (FlatSpace(testPosition, size, tolerance))
                {
                    float lowestPoint = GetLowestHeight(testPosition, size) * heightMultiplier;
                    chunkPosition = new Vector3(testPosition.x, lowestPoint, testPosition.y);
                    return true;
                }
            }
        }
        return false;
    }
    private bool IsOutofBounds(Vector2 point)
    {
        if (point.x > 126 || point.y > 126)
        {
            return true;
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
                if (IsOutofBounds(new Vector2(x, y))) return false;
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
    private float GetLowestHeight(Vector2 position, Vector2 size)
    {
        int startX = (int)position.x;
        int startY = (int)position.y;

        int endX = (int)size.x + startX;
        int endY = (int)size.y + startY;

        float height = heightMap[startX, startY];

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                float testHeight = heightMap[x, y];
                if (height > testHeight)
                {
                    height = testHeight;
                }
            }
        }

        return height;
    }
    #endregion

}

[Serializable]
public struct ChunkObject
{
    public GameObject gameObject;
    [Range(0, 128)]
    public float minimumSpacingForThis;
    [Range(0, 128)]
    public float minimumSpacingForOther;
    public GameObject[] keepDistanceFromOther;
    public int amount;
    public float steepTolerance;
}