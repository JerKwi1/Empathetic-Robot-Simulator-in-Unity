using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class RobotManager : MonoBehaviour
{
    public GameObject robotPrefab;
    public int numberOfRobots = 5;
    public Transform groundPlane;
    public Transform foodSource;
    public Vector3 spawnAreaSize = new Vector3(10f, 1f, 10f);
    public bool useBaseKnowledge = false;
    public bool useReinforcementLearning = true;

    [Header("Robot Memory Settings")]
    public float memoryRadius = 1f;
    public int maxMemoryCount = 10;
    [HideInInspector]
    public bool showFOV = false;
    public int maxSimulations = 3;
    private int currentSimulation = 0;
    private static string logFilePath;
    private bool simulationStarted = false;

    public static RobotManager Instance;

    void Awake()
    {
        Instance = this;
        logFilePath = Path.Combine(Application.dataPath, "SimulationResults.txt");
    }

    void Start()
    {
        if (groundPlane == null)
        {
            Debug.LogError("Ground plane not assigned in RobotManager");
            return;
        }
        if (foodSource == null)
        {
            Debug.LogError("Food source not assigned in RobotManager"); // Prevents crashes
            return;
        }
    }

    public void StartSimulation()
    {
        if (simulationStarted) return;
        simulationStarted = true;
        currentSimulation = 0;
        Robot.ResetSimulationVariables();
        SpawnRobots();
    }

    public void ResetToInitial()
    {
        StopAllCoroutines();
        ClearRobots();
        currentSimulation = 0;
        simulationStarted = false;
        Robot.ResetSimulationVariables();
    }

    void SpawnRobots()
    {
        for (int i = 0; i < numberOfRobots; i++)
        {
            Vector3 randomPosition = GetRandomSpawnPosition();
            if (randomPosition != Vector3.zero)
            {
                GameObject robot = Instantiate(robotPrefab, randomPosition, Quaternion.identity);
                Robot robotScript = robot.GetComponent<Robot>();
                if (robotScript != null)
                {
                    robotScript.useBaseKnowledge = useBaseKnowledge;
                    robotScript.useReinforcementLearning = useReinforcementLearning;
                    robotScript.memoryRadius = memoryRadius;
                    robotScript.maxMemoryCount = maxMemoryCount;
                    robotScript.foodSource = foodSource;
                    robotScript.showFOV = showFOV;
                }
            }
            else
            {
                Debug.LogWarning("Failed to find a valid spawn position for robot " + i);
            }
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = groundPlane.GetComponent<Renderer>().bounds;
        Vector3 randomPos;
        int attempts = 10;
        for (int i = 0; i < attempts; i++)
        {
            randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 0.5f, // Ensure robots spawn slightly above the ground
                Random.Range(bounds.min.z, bounds.max.z)
            );
            if (IsValidSpawnPosition(randomPos))
            {
                return randomPos;
            }
        }
        return Vector3.zero; // Fallback if no valid position found
    }

    bool IsValidSpawnPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Obstacle"))
            {
                return false;
            }
        }
        return true;
    }

    // ───── Restart Simulation Coroutine ─────
    public IEnumerator RestartSimulation()
    {
        if (currentSimulation < maxSimulations)
        {
            currentSimulation++;
            Debug.Log("Restarting Simulation " + currentSimulation + " of " + maxSimulations);
            LogSimulationTime();
            yield return new WaitForSeconds(2f); // Delay before restarting
            yield return new WaitForSeconds(2f); // Additional wait before clearing robots
            ClearRobots();
            
            // Reset simulation variables so that the timer starts at 0 and found-food count is cleared
            Robot.ResetSimulationVariables();
            
            SpawnRobots();
        }
        else
        {
            Debug.Log("Simulation complete. All " + maxSimulations + " runs finished.");
            LogSimulationTime();
        }
    }

    void ClearRobots()
    {
        Robot[] robots = FindObjectsOfType<Robot>();
        foreach (Robot robot in robots)
        {
            Destroy(robot.gameObject);
        }
    }

    void LogSimulationTime()
    {
        // Calculate simulation elapsed time as the difference from when the simulation started
        float elapsedTime = Time.time - Robot.GetSimulationStartTime();
        Time.timeScale = 1f; // Ensure time is running normally
        string logEntry = "Simulation " + currentSimulation + " completed in " + elapsedTime + " seconds.";
        File.AppendAllText(logFilePath, logEntry + "\n");
        Debug.Log("Simulation time logged to file: " + logFilePath);
        ResetTimer();
    }

    private void ResetTimer()
    {
        // In Unity, Time.time cannot be reset manually. 
        // Instead, we reset our simulation timer via Robot.ResetSimulationVariables().
        // Here, we ensure timeScale is normal.
        Time.timeScale = 1f;
    }
}
