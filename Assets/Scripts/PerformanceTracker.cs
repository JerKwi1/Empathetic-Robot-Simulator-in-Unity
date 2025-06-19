using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    private int totalRobots;
    private int robotsThatReachedFood = 0;
    private float startTime;

    void Start()
    {
        startTime = Time.time;
        totalRobots = FindObjectsOfType<Robot>().Length;
    }

    public void RobotReachedFood()
    {
        robotsThatReachedFood++;

        if (robotsThatReachedFood >= totalRobots)
        {
            float timeTaken = Time.time - startTime;
            Debug.Log("All robots reached food in " + timeTaken + " seconds.");
            enabled = false;
        }
    }
}