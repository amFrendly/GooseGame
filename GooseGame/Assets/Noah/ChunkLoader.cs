using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChunkLoader : MonoBehaviour
{
    public const int chunkSize = 128;

    public int seed;
    #region SerialziedField Variables
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
    Transform player;

    [SerializeField]
    GameObject chunk;

    [SerializeField]
    float loadChunksDistance;
    #endregion

    Vector2 oldPos;

    const float moveThresholdForChunkUpdate = 25;
    const float sqrMoveThresholdForChunkUpdate = moveThresholdForChunkUpdate * moveThresholdForChunkUpdate;

    Vector2 currentChunkCoord;

    private void Start()
    {
        StartLoadChunks();
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        oldPos = playerPos;
    }

    void Update()
    {
        int currentChunkCoordX = Mathf.RoundToInt((player.position.x - chunkSize / 2) / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt((player.position.z - chunkSize / 2) / chunkSize);
        currentChunkCoord = new Vector2(currentChunkCoordX, currentChunkCoordY);

        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        if ((oldPos - playerPos).sqrMagnitude > sqrMoveThresholdForChunkUpdate)
        {
            oldPos = playerPos;
            LoadChunks();
        }
    }

    public Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();

    public void StartLoadChunks()
    {
        int loadChunksAmount = Mathf.RoundToInt(loadChunksDistance / chunkSize);

        for (int yOffset = -loadChunksAmount; yOffset <= loadChunksAmount; yOffset++)
        {
            for (int xOffset = -loadChunksAmount; xOffset <= loadChunksAmount; xOffset++)
            {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset);

                if (!chunks.ContainsKey(viewChunkCoord))
                {
                    CreateChunk(new Vector3(viewChunkCoord.x, 0, viewChunkCoord.y));
                }
            }
        }

        for (int yOffset = -loadChunksAmount; yOffset <= loadChunksAmount; yOffset++)
        {
            for (int xOffset = -loadChunksAmount; xOffset <= loadChunksAmount; xOffset++)
            {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset);

                if (chunks.ContainsKey(viewChunkCoord))
                {
                    float distance = Vector2.Distance(currentChunkCoord, viewChunkCoord);
                    chunks[viewChunkCoord].SwapMesh(distance);
                }
            }
        }
    }
    public void LoadChunks()
    {
        int loadChunksAmount = Mathf.RoundToInt(loadChunksDistance / chunkSize);
        for (int yOffset = -loadChunksAmount; yOffset <= loadChunksAmount; yOffset++)
        {
            for (int xOffset = -loadChunksAmount; xOffset <= loadChunksAmount; xOffset++)
            {
                Vector2 viewChunkCoord = new Vector2(currentChunkCoord.x + xOffset, currentChunkCoord.y + yOffset);

                if (chunks.ContainsKey(viewChunkCoord))
                {
                    float distance = Vector2.Distance(currentChunkCoord * chunkSize, viewChunkCoord * chunkSize);
                    chunks[viewChunkCoord].SwapMesh(distance);
                }
                else
                {
                    CreateChunk(new Vector3(viewChunkCoord.x, 0, viewChunkCoord.y));
                }
            }
        }

    }

    private Chunk CreateChunk(Vector3 position)
    {
        Chunk newChunk = Instantiate(chunk, transform).GetComponent<Chunk>();
        chunks.Add(new Vector2(position.x, position.z), newChunk);
        newChunk.transform.position = position * chunkSize;
        Chunk chunkComponent = newChunk.GetComponent<Chunk>();
        chunkComponent.player = player;
        chunkComponent.SetChunkInformation(seed, scale, octaves, persistance, lacunarity, heightMultiplier, heightCurve);
        return chunkComponent;
    }

    private void OnValidate()
    {
        if (loadChunksDistance < 1)
        {
            loadChunksDistance = 1;
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ChunkLoader))]
class ChunkLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ChunkLoader parent = (ChunkLoader)target;
            
        base.OnInspectorGUI();

        if(GUILayout.Button("Create Chunks"))
        {
            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.transform.GetChild(i).gameObject);
            }
            parent.chunks.Clear();
            parent.LoadChunks();
            for(int i = 0; i < parent.transform.childCount; i++)
            {
                Chunk chunk = parent.transform.GetChild(i).GetComponent<Chunk>();
                chunk.CheckThreadQueue();
            }
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                Chunk chunk = parent.transform.GetChild(i).GetComponent<Chunk>();
                chunk.CheckThreadQueue();

                chunk.AddObjects();
            }
        }
        if(GUILayout.Button("Clear Chunks"))
        {
            parent.chunks.Clear();
            for (int i = parent.transform.childCount -1; i >= 0; i--)
            {
                DestroyImmediate(parent.transform.GetChild(i).gameObject);
            }
        }
    }
}


#endif