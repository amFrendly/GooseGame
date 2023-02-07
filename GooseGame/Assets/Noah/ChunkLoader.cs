using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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

    Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();

    private void StartLoadChunks()
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
    private void LoadChunks()
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
