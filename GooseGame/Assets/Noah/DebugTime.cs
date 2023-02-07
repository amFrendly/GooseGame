using UnityEngine;

public static class DebugTime
{
    static float time;

    public static void Start()
    {
        time = -Time.realtimeSinceStartup;
    }
    public static void Stop()
    {
        time += Time.realtimeSinceStartup;
        Debug.Log("Time to execute was: " + time);
        time = 0;
    }
}