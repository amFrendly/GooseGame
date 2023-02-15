using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField]
    Transform ballHolder;

    [SerializeField]
    Transform ball;

    [SerializeField]
    float spawnTimer;

    float timer = 0;

    float xMax = 1000;
    float zMax = 1000;
    float ybase = 100;

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            timer = spawnTimer;
            float x = Random.Range(-xMax, xMax);
            float z = Random.Range(-zMax, zMax);
            float y = ybase;
            Vector3 offsetSpawn = new Vector3(x, y, z);

            Instantiate(ball, ballHolder).transform.position = offsetSpawn + transform.position;
        }
    }
}
