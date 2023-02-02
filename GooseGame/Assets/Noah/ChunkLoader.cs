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
    int loadDistance;
    #endregion

    private void Start()
    {
        CreateChunk(transform.position);
    }

    public void Update()
    {
        LoadNewChunks2();
        //LoadNewChunks(2); 
    }


    void LoadNewChunks2()
    {
        transform.position = new Vector3();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform chunk = transform.GetChild(i);

            if (Vector3.Distance(chunk.position, new Vector3(player.position.x, 0, player.position.z)) > loadDistance) continue;
            ChunkConnection chunkConnection = chunk.GetComponent<Chunk>().connection;

            if (!chunkConnection.CanConnect()) continue;
            ChunkConnection.Direction direction = chunkConnection.NearestConnection(player, chunkSize, out Vector3 closest);
            Chunk newChunk = CreateChunk(transform.position + closest);
            //chunkConnection.Connect(direction, ref newChunk.connection);
            newChunk.connection.UpdateConnection(transform, chunkSize);
        }
    }

    void LoadNewChunks(int chunkView)
    {
        int posX = (int)player.position.x / chunkSize;
        int posZ = (int)player.position.z / chunkSize;
        Vector3 center = new Vector3(posX, 0, posZ);

        chunkView *= chunkView;
        chunkView++;

        for(int x = 0; x < chunkView; x++)
        {
            for(int z = 0; z < chunkView; z++)
            {
                Vector3 checkPosition = (new Vector3(x, 0, z) + center) * chunkSize;
                if(ChunkEmpty(checkPosition))
                {
                    CreateChunk(checkPosition);
                }
            }
        }
    }

    bool ChunkEmpty(Vector3 position)
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            Transform chunk = transform.GetChild(i);
            if (chunk.position == position) return false;
        }
        return true;
    }

    private Chunk CreateChunk(Vector3 position)
    {
        GameObject newChunk = Instantiate(chunk, transform);
        newChunk.transform.position = position;
        Chunk chunkComponent = newChunk.GetComponent<Chunk>();
        chunkComponent.player = player;
        chunkComponent.SetChunkInformation(seed, scale, octaves, persistance, lacunarity, heightMultiplier, heightCurve, meshSimplification);
        chunkComponent.CreateShape();
        return chunkComponent;
    }

    public void GenerateMap(int size)
    {
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                GameObject newChunk = Instantiate(chunk, transform);
                newChunk.transform.position = new Vector3(x * chunkSize, 0, z * chunkSize);
                Chunk chunkComponent = newChunk.GetComponent<Chunk>();
                chunkComponent.SetChunkInformation(seed, scale, octaves, persistance, lacunarity, heightMultiplier, heightCurve, meshSimplification);
            }
        }
    }

    private void OnValidate()
    {
        if (loadDistance < 1)
        {
            loadDistance = 1;
        }
    }
}
