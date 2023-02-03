using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

[Serializable]
public class ChunkConnection
{
    public enum Direction
    {
        left,
        right,
        forward,
        back
    }

    public ChunkConnection(Transform transform)
    {
        this.transform = transform;
    }

    public Transform transform;

    public Transform left;
    public Transform right;
    public Transform forward;
    public Transform back;

    public bool CanConnect()
    {
        if (left == null || right == null || forward == null || back == null)
        {
            return true;
        }

        return false;
    }
    public Direction NearestConnection(Transform transform, int chunkSize, out Vector3 position)
    {
        float closestDistance = float.MaxValue;
        Direction direction = Direction.right;
        float distance = 0;
        position = new Vector3();

        if (right == null)
        {
            Vector3 rightPos = this.transform.position + this.transform.right * chunkSize;
            distance = Vector3.Distance(rightPos, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                direction = Direction.right;
                position = rightPos;
            }
        }

        if (left == null)
        {
            Vector3 leftPos = this.transform.position - this.transform.right * chunkSize;
            distance = Vector3.Distance(leftPos, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                direction = Direction.left;
                position = leftPos;
            }
        }

        if (forward == null)
        {
            Vector3 forwardPos = this.transform.position + this.transform.forward * chunkSize;
            distance = Vector3.Distance(forwardPos, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                direction = Direction.forward;
                position = forwardPos;
            }
        }

        if (back == null)
        {
            Vector3 backPos = this.transform.position - this.transform.forward * chunkSize;
            distance = Vector3.Distance(backPos, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                direction = Direction.back;
                position = backPos;
            }
        }

        return direction;
    }
    public void Connect(Direction direction, ref ChunkConnection other)
    {
        switch (direction)
        {
            case Direction.right:
                right = transform;
                other.left = transform;
                break;
            case Direction.left:
                left = transform;
                other.right = transform;
                break;
            case Direction.forward:
                forward = transform;
                other.back = transform;
                break;
            case Direction.back:
                back = transform;
                other.forward = transform;
                break;
        }
    }
    public void UpdateConnection(Transform children, int chunkSize)
    {
        for (int i = 0; i < children.childCount; i++)
        {
            Transform chunk = children.GetChild(i);
            if (Neighbour(Direction.left, chunk.position, chunkSize))
            {
                ChunkConnection chunkConnection = chunk.GetComponent<Chunk>().connection;
                Connect(Direction.left, ref chunkConnection);
            }

            if (Neighbour(Direction.right, chunk.position, chunkSize))
            {
                ChunkConnection chunkConnection = chunk.GetComponent<Chunk>().connection;
                Connect(Direction.right, ref chunkConnection);
            }

            if (Neighbour(Direction.forward, chunk.position, chunkSize))
            {
                ChunkConnection chunkConnection = chunk.GetComponent<Chunk>().connection;
                Connect(Direction.forward, ref chunkConnection);
            }

            if (Neighbour(Direction.back, chunk.position, chunkSize))
            {
                ChunkConnection chunkConnection = chunk.GetComponent<Chunk>().connection;
                Connect(Direction.back, ref chunkConnection);
            }
        }
    }
    private bool Neighbour(Direction direction, Vector3 position, int chunkSize)
    {
        switch (direction)
        {
            case Direction.left:
                Vector3 leftPos = transform.position - transform.right * chunkSize;
                if (leftPos == position) return true;
                break;
            case Direction.right:
                Vector3 rightPos = transform.position + transform.right * chunkSize;
                if (rightPos == position) return true;
                break;
            case Direction.forward:
                Vector3 forwardPos = transform.position + transform.forward * chunkSize;
                if (forwardPos == position) return true;
                break;
            case Direction.back:
                Vector3 backPos = transform.position - transform.forward * chunkSize;
                if (backPos == position) return true;
                break;
        }

        return false;
    }
}
