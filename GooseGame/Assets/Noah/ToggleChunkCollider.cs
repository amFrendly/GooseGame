using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleChunkCollider : MonoBehaviour
{
    Chunk chunk;

    private void Awake()
    {
        chunk = GetComponentInParent<Chunk>();
    }

    private void OnTriggerEnter(Collider other)
    {
        chunk.ActivateCollder();
    }

    private void OnTriggerExit(Collider other)
    {
        chunk.DeactivateCollder();
    }

}