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
    public bool useEmpatheticBehavior = true;
    public bool useBaseKnowledge = false;
    public bool useReinforcementLearning = true;
    [Header("Map Selection")]
    public Transform[] mapRoots;   

    [Header("Robot Memory Settings")]
    public float memoryRadius = 1f;
    public int maxMemoryCount = 10;
    [HideInInspector]
    public bool showFOV = false;
    public int maxSimulations = 3;
    private int currentSimulation = 0;
    private static string logFilePath;
    private bool simulationStarted = false;
    public int selectedMapIndex = 0;    

    public static RobotManager Instance;
    public bool isTrainingMode = true;

    // Called when the script instance is being loaded.
    // Sets up the singleton instance and determines the log file path.
    void Awake()
    {
        Instance = this;
        string exeFolder = System.AppDomain.CurrentDomain.BaseDirectory;
        logFilePath = Path.Combine(exeFolder, "SimulationResults.txt");
    }

    // Runs once at the beginning: activates the chosen map and checks for required references.
    void Start()
    {
        ActivateMap(selectedMapIndex);

        if (groundPlane == null || foodSource == null)
            Debug.LogError("Map setup error: missing GroundPlane or FoodSource under " + mapRoots[selectedMapIndex].name);

        if (groundPlane == null)
        {
            Debug.LogError("Ground plane not assigned in RobotManager");
            return;
        }
        if (foodSource == null)
        {
            Debug.LogError("Food source not assigned in RobotManager"); 
            return;
        }
    }

    // Begins a new simulation run (if not already started): resets variables and spawns robots.
    public void StartSimulation()
    {
        if (simulationStarted) return;
        simulationStarted = true;
        currentSimulation = 0;
        Robot.ResetSimulationVariables();
        SpawnRobots();
    }

    // Stops any ongoing coroutines, clears existing robots, resets counters, and allows a new simulation to start.
    public void ResetToInitial()
    {
        StopAllCoroutines();
        ClearRobots();
        currentSimulation = 0;
        simulationStarted = false;
        Robot.ResetSimulationVariables();
    }

    // Enables only the map at the given index and updates groundPlane/foodSource references accordingly.
    public void ActivateMap(int index)
    {
        selectedMapIndex = Mathf.Clamp(index, 0, mapRoots.Length - 1);
        for (int i = 0; i < mapRoots.Length; i++)
        {
            mapRoots[i].gameObject.SetActive(i == selectedMapIndex);
        }

        var root = mapRoots[selectedMapIndex];
        groundPlane = root.Find("GroundPlane");
        foodSource = root.Find("FoodSource");
    }

    // Instantiates the configured number of robots at random valid positions within the ground plane.
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
                    robotScript.isTrainingMode = this.isTrainingMode;
                    robotScript.useBaseKnowledge = this.useBaseKnowledge;
                    robotScript.useReinforcementLearning = this.useReinforcementLearning;
                    robotScript.useEmpatheticBehavior = this.useEmpatheticBehavior;
                    robotScript.memoryRadius = this.memoryRadius;
                    robotScript.maxMemoryCount = this.maxMemoryCount;
                    robotScript.foodSource = this.foodSource;
                    robotScript.showFOV = this.showFOV;
                }
            }
            else
            {
                Debug.LogWarning("Failed to find a valid spawn position for robot " + i);
            }
        }
    }

    // Attempts up to 10 times to pick a random point over the ground plane that is not obstructed.
    // Returns Vector3.zero if no valid point is found.
    Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = groundPlane.GetComponent<Renderer>().bounds;
        Vector3 randomPos;
        int attempts = 10;
        for (int i = 0; i < attempts; i++)
        {
            randomPos = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 0.5f, 
                Random.Range(bounds.min.z, bounds.max.z)
            );
            if (IsValidSpawnPosition(randomPos))
            {
                return randomPos;
            }
        }
        return Vector3.zero; 
    }

    // Returns true if the given position is not overlapping any collider tagged “Obstacle.”
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

    // Called when a run finishes: logs time, waits, destroys old robots, resets state, and spawns a new batch until maxSimulations is reached.
    public IEnumerator RestartSimulation()
    {
        if (currentSimulation < maxSimulations)
        {
            currentSimulation++;
            Debug.Log("Restarting Simulation " + currentSimulation + " of " + maxSimulations);
            LogSimulationTime();
            yield return new WaitForSeconds(2f);
            yield return new WaitForSeconds(2f);
            ClearRobots();
            Robot.ResetSimulationVariables();
            SpawnRobots();
        }
        else
        {
            Debug.Log("Simulation complete. All " + maxSimulations + " runs finished.");
            LogSimulationTime();
        }
    }

    // Finds all active Robot instances in the scene and destroys their GameObjects.
    void ClearRobots()
    {
        Robot[] robots = FindObjectsOfType<Robot>();
        foreach (Robot robot in robots)
        {
            Destroy(robot.gameObject);
        }
    }

    // Computes elapsed time since the last simulation start, appends it to a log file, and resets the timer.
    void LogSimulationTime()
    {
        float elapsedTime = Time.time - Robot.GetSimulationStartTime();
        Time.timeScale = 1f; 
        string logEntry = "Simulation " + currentSimulation + " completed in " + elapsedTime + " seconds.";
        File.AppendAllText(logFilePath, logEntry + "\n");
        Debug.Log("Simulation time logged to file: " + logFilePath);
        ResetTimer();
    }

    // Ensures the game’s time scale is set back to normal (1.0) after logging.
    private void ResetTimer()
    {
        Time.timeScale = 1f;
    }
}
