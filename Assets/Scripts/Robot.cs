using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System.Linq;
using Random = UnityEngine.Random;


public struct FuzzyState {
    public float internalValue;
    public float externalValue;

    public FuzzyState(float internalValue, float externalValue) {
        this.internalValue = internalValue;
        this.externalValue = externalValue;
    }
}

public class Robot : MonoBehaviour
{
    private const string MODEL_FILE = "trained_model.json";
    [Tooltip("If true, run in training mode (will overwrite MODEL_FILE on exit). " + "If false, load MODEL_FILE but do not save it.")]
    public bool isTrainingMode = true;
    public string robotId;
    private KnowledgePersistence persistence;
    private static Dictionary<(int state, int action), float> qTable;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float stuckTimeThreshold = 2f;  
    private HashSet<int> visitedStates = new HashSet<int>();

    [Tooltip("Penalty added to reward when revisiting the same state")]
    private float visitedStatePenalty = -1f; 

    public float detectionRange = 10f;
    public float moveSpeed = 3.5f;
    public float wanderRadius = 10f;
    public bool useBaseKnowledge = false;
    public bool useEmpatheticBehavior = true;
    public Transform foodSource;
    public Color searchingColor = Color.blue;
    public Color foundFoodColor = Color.green;
    public Color obstacleDetectedColor = Color.red;
    public float fieldOfViewAngle = 60f;

    [HideInInspector]
    public bool showFOV = false;
    public int fovSegments = 20;
    public bool useReinforcementLearning = true;
    private NavMeshAgent agent;
    private bool foodFound = false;
    private static Vector3 lastKnownFoodLocation = Vector3.zero;
    private Renderer robotRenderer;
    private List<Vector3> visitedLocations = new List<Vector3>();
    private static int robotsThatFoundFood = 0;
    private static float simulationStartTime;
    private static List<Vector3> noFoodAreas = new List<Vector3>();
    public static float noFoodAreaRadius = 5f; 

    public float memoryRadius = 1f;

    public int maxMemoryCount = 10;

    private float learningRate = 0.1f;
    private float discountFactor = 0.95f;
    private float explorationRate = 0.1f;
    private float binSize = 5.0f; 
    private int numActions = 8;  

    private int currentState;
    private int currentAction;
    private Vector3 previousPosition;
    private LineRenderer fovRenderer;

    // Called once at startup: sets up NavMeshAgent, renderer, FOV, and chooses initial behavior (RL, base-knowledge, or wandering).
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        robotRenderer = GetComponent<Renderer>();
        fovRenderer = gameObject.AddComponent<LineRenderer>();
        fovRenderer.material = new Material(Shader.Find("Sprites/Default"));
        fovRenderer.widthMultiplier = 0.02f;
        fovRenderer.loop = false;
        fovRenderer.useWorldSpace = true;
        fovRenderer.positionCount = fovSegments + 2;
        fovRenderer.enabled = showFOV;
        robotRenderer.material.color = searchingColor;

        if (robotsThatFoundFood == 0)
        {
            simulationStartTime = Time.time;
        }

        if (useReinforcementLearning)
        {
            previousPosition = transform.position;
            currentState = GetState();
            currentAction = ChooseAction(currentState);
            Vector3 destination = ComputeDestinationFromAction(currentAction);
            agent.SetDestination(destination);
        }
        else if (useBaseKnowledge)
        {
            KnowledgeBaseManager.LoadKnowledgeBase();
            KnowledgeBaseManager.LoadNoFoodAreas();
            Vector3 bestFoodLocation = KnowledgeBaseManager.GetBestFoodLocation();
            if (bestFoodLocation != Vector3.zero)
            {
                agent.SetDestination(bestFoodLocation);
            }
            else
            {
                StartCoroutine(ContinuousMovement());
            }
        }
        else
        {
            StartCoroutine(ContinuousMovement());
        }
    }

    // Runs every frame: handles food detection, empathetic behavior, reinforcement‐learning updates, movement, and FOV drawing.
    void Update()
    {
        if (!foodFound)
        {
            DetectFood();
            if (useEmpatheticBehavior)
            {
                DetectNearbyRobots();
            }
        }

        if (useReinforcementLearning && !foodFound && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            int newState = GetState();

            bool isRevisit = visitedStates.Contains(newState);
            if (!isRevisit) 
                visitedStates.Add(newState);
            float lambda = 0.1f;
            float previousDistance = Vector3.Distance(previousPosition, foodSource.position);
            float currentDistance = Vector3.Distance(transform.position, foodSource.position);
            float reward = Mathf.Exp(-lambda * previousDistance) - Mathf.Exp(-lambda * currentDistance);
            if (foodFound)
            {
                reward += 10f;
            }

            if (isRevisit)
                reward += visitedStatePenalty;
            float maxQNext = float.MinValue;
            for (int a = 0; a < numActions; a++)
            {
                float qVal = GetQValue(newState, a);
                if (qVal > maxQNext) { maxQNext = qVal; }
            }
            float oldQ = GetQValue(currentState, currentAction);
            float newQ = oldQ + learningRate * (reward + discountFactor * maxQNext - oldQ);
            SetQValue(currentState, currentAction, newQ);

            currentState = newState;
            currentAction = ChooseAction(newState);
            previousPosition = transform.position;
            Vector3 newDestination;
            int safety = 0;
            do {
                newDestination = ComputeDestinationFromAction(currentAction);
                if ( IsVisited(newDestination) || IsPointInNoFoodArea(newDestination) ) {
                currentAction = ChooseAction(newState);
                safety++;
                } else {
                break;
                }
            } while (safety < 10);

            visitedLocations.Add(newDestination);
            if (visitedLocations.Count > maxMemoryCount)
                visitedLocations.RemoveAt(0);

            agent.SetDestination(newDestination);
        }

        float deltaMove = Vector3.Distance(transform.position, lastPosition);

        if (agent.hasPath && agent.remainingDistance > 0.1f && deltaMove < 0.01f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        if (stuckTimer > stuckTimeThreshold)
        {
            Vector3 rescue = GetNewExplorationPoint();
            if (NavMesh.SamplePosition(rescue, out var navHit, wanderRadius, NavMesh.AllAreas))
                agent.SetDestination(navHit.position);
            stuckTimer = 0f;
        }

        
        lastPosition = transform.position;

        if (fovRenderer != null)
        {
            fovRenderer.enabled = showFOV;
            if (showFOV) DrawFOV();
        }
    }

    // Called when GameObject is instantiated: loads or initializes the Q‐table and sets up initial positions.
    void Awake()
    {
        if (qTable == null)
        {
            visitedStates.Clear();
            bool managerTrainingFlag = false;
            if (RobotManager.Instance != null)
            {
                managerTrainingFlag = RobotManager.Instance.isTrainingMode;
            }
            this.isTrainingMode = managerTrainingFlag;
            persistence = new KnowledgePersistence(
                MODEL_FILE,
                isTrainingMode ? MODEL_FILE : null
            );
            persistence.LoadQTable(out qTable);
            lastPosition = transform.position;

        }
    }

    // Saves the Q‐table to disk if in training mode when the application is closing.
    void OnApplicationQuit()
    {
        if (isTrainingMode)
            persistence.SaveQTable(qTable);
    }

    // Static helper to force‐save the Q‐table at any time.
    public static void SaveModelNow()
    {
        if (RobotManager.Instance != null && RobotManager.Instance.isTrainingMode)
        {
            var persistence = new KnowledgePersistence(MODEL_FILE, MODEL_FILE);
            persistence.SaveQTable(qTable);
        }
    }

    // Draws the robot’s field‐of‐view cone using a LineRenderer for debugging or visualization.
    void DrawFOV()
    {
        float step = fieldOfViewAngle / fovSegments;
        float startAngle = -fieldOfViewAngle / 2f;
        fovRenderer.positionCount = fovSegments + 2;

        
        fovRenderer.SetPosition(0, transform.position);
        for (int i = 0; i <= fovSegments; i++)
        {
            float ang = startAngle + step * i;
            Vector3 dir = Quaternion.Euler(0, ang, 0) * transform.forward;
            Vector3 point = transform.position + dir.normalized * detectionRange;
            fovRenderer.SetPosition(i + 1, point);
        }
    }

    // Repeatedly picks a new wander destination until food is found; marks no‐food areas if empathetic behavior is enabled.
    IEnumerator ContinuousMovement()
    {
        while (!foodFound)
        {
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                if (useEmpatheticBehavior)
                {
                    Vector3 currentPos = transform.position;
                    if (!IsPointInNoFoodArea(currentPos))
                    {
                        noFoodAreas.Add(currentPos);
                        if (useBaseKnowledge)
                        {
                            KnowledgeBaseManager.AddNoFoodArea(currentPos);
                        }
                        Debug.Log("Marking area as no-food: " + currentPos);
                    }
                }
                Wander();
            }
            yield return new WaitForSeconds(0.5f); 
        }
    }

    // Chooses a random valid NavMesh point and moves the agent toward it.
    void Wander()
    {
        Vector3 newDestination = GetNewExplorationPoint();
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newDestination, out navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
            agent.isStopped = false;
            visitedLocations.Add(navHit.position);
        }
    }

    // Samples multiple candidate points within wanderRadius and returns the furthest unvisited (or random fallback).
    Vector3 GetNewExplorationPoint()
    {
        Vector3 bestPoint = Vector3.zero;
        float maxDistance = 0f;
        bool candidateFound = false;
        for (int i = 0; i < 10; i++)
        {
            Vector3 candidatePoint = transform.position + Random.insideUnitSphere * wanderRadius;
            if (useEmpatheticBehavior && IsPointInNoFoodArea(candidatePoint))
            {
                continue;
            }
            if (IsVisited(candidatePoint))
            {
                continue;
            }
            candidateFound = true;
            float distanceToCandidate = Vector3.Distance(transform.position, candidatePoint);
            if (distanceToCandidate > maxDistance)
            {
                maxDistance = distanceToCandidate;
                bestPoint = candidatePoint;
            }
        }
        if (!candidateFound)
        {
            bestPoint = transform.position + Random.insideUnitSphere * wanderRadius;
            Debug.Log("Fallback: All candidate points were visited, using a random point.");
        }
        visitedLocations.Add(bestPoint);
        if (visitedLocations.Count > maxMemoryCount)
        {
            visitedLocations.RemoveAt(0);
        }
        return bestPoint;
    }

    // Returns true if the candidate position is within memoryRadius of any recently visited location.
    bool IsVisited(Vector3 candidate)
    {
        foreach (Vector3 visited in visitedLocations)
        {
            if (Vector3.Distance(candidate, visited) < memoryRadius)
                return true;
        }
        return false;
    }

    // Checks if a given point lies inside any no‐food region (local list or loaded from knowledge base).
    bool IsPointInNoFoodArea(Vector3 point)
    {
        foreach (Vector3 area in noFoodAreas)
        {
            if (Vector3.Distance(point, area) < noFoodAreaRadius)
                return true;
        }
        foreach (Vector3 area in KnowledgeBaseManager.GetNoFoodAreas())
        {
            if (Vector3.Distance(point, area) < noFoodAreaRadius)
                return true;
        }
        return false;
    }

    // Casts a ray in the robot’s forward FOV to see if “Food” is visible; if found, sets state and updates knowledge.
    void DetectFood()
    {
        if (foodSource == null) return;
        RaycastHit hit;
        Vector3 directionToFood = (foodSource.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToFood);
        if (angle < fieldOfViewAngle * 0.5f)
        {
            if (Physics.Raycast(transform.position, directionToFood, out hit, detectionRange))
            {
                if (hit.collider.CompareTag("Food"))
                {
                    if (!foodFound)
                    {
                        foodFound = true;
                        robotsThatFoundFood++;
                    }
                    agent.SetDestination(foodSource.position);
                    robotRenderer.material.color = foundFoodColor;
                    SaveFoodLocation(foodSource.position);
                    noFoodAreas.RemoveAll(area => Vector3.Distance(area, foodSource.position) < noFoodAreaRadius);
                    if (useBaseKnowledge)
                    {
                        KnowledgeBaseManager.RemoveNoFoodAreaCloseTo(foodSource.position, noFoodAreaRadius);
                    }
                    CheckSimulationEnd();
                }
            }
        }
    }

    // Scans nearby robots within detectionRange; if any have found food and are “fuzzy similar,” follow them.
    void DetectNearbyRobots()
    {
        Collider[] nearbyRobots = Physics.OverlapSphere(transform.position, detectionRange);
        FuzzyState myState = GetCurrentFuzzyState();
        foreach (Collider robotCollider in nearbyRobots)
        {
            Robot otherRobot = robotCollider.GetComponent<Robot>();
            if (otherRobot != null && otherRobot.foodFound)
            {
                Vector3 toOther = otherRobot.transform.position - transform.position;
                float angleToOther = Vector3.Angle(transform.forward, toOther);
                if (angleToOther < fieldOfViewAngle * 0.5f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, toOther.normalized, out hit, detectionRange))
                    {
                        if (hit.collider.gameObject != otherRobot.gameObject)
                            continue;
                    }
                    FuzzyState otherState = otherRobot.GetCurrentFuzzyState();
                    float similarity = CalculateFuzzySimilarity(myState, otherState);
                    if (similarity > 0.7f)
                    {
                        if (!foodFound) { robotsThatFoundFood++; }
                        foodFound = true;
                        agent.SetDestination(otherRobot.foodSource.position);
                        robotRenderer.material.color = foundFoodColor;
                        Debug.Log("Empathetic decision: adopting destination from green robot with similarity " + similarity);
                        CheckSimulationEnd();
                        return;
                    }
                }
            }
        }
    }

    // Trigger callback: if colliding with food, mark as found; if colliding with obstacle/robot, steer away.
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Food"))
        {
            if (!foodFound)
            {
                foodFound = true;
                robotsThatFoundFood++;
            }
            lastKnownFoodLocation = other.transform.position;
            agent.SetDestination(foodSource.position);
            robotRenderer.material.color = foundFoodColor;
            SaveFoodLocation(lastKnownFoodLocation);
            CheckSimulationEnd();
        }
        else if (other.CompareTag("Obstacle") || other.CompareTag("Robot"))
        {
            
            Vector3 away = (transform.position - other.transform.position).normalized;
            Vector3 target = transform.position + away * wanderRadius;
            if (NavMesh.SamplePosition(target, out var navHit, wanderRadius, NavMesh.AllAreas))
                agent.SetDestination(navHit.position);
        }
    }

    // Chooses and navigates to a random point away from the current position to avoid obstacles.
    void AvoidObstacle()
    {
        Vector3 newDirection = transform.position + Random.insideUnitSphere * wanderRadius;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newDirection, out navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    // Adds a discovered food position to the shared knowledge base.
    void SaveFoodLocation(Vector3 location)
    {
        KnowledgeBaseManager.AddFoodLocation(location);
    }

    // If all robots have found food, logs total time and requests a simulation restart.
    void CheckSimulationEnd()
    {
        if (robotsThatFoundFood >= FindObjectsOfType<Robot>().Length)
        {
            float elapsedTime = Time.time - simulationStartTime;
            Debug.Log("Simulation finished in " + elapsedTime + " seconds.");
            StartCoroutine(RobotManager.Instance.RestartSimulation());
        }
    }

    // Static helper to reset counters and clear no‐food zones at the start of a new simulation.
    public static void ResetSimulationVariables()
    {
        robotsThatFoundFood = 0;
        simulationStartTime = Time.time;
        noFoodAreas.Clear();
    }

    // Returns the timestamp when the simulation began.
    public static float GetSimulationStartTime()
    {
        return simulationStartTime;
    }

    // Computes a fuzzy state using random internal value and normalized distance to food as external value.
    FuzzyState GetCurrentFuzzyState()
    {
        float internalValue = Random.Range(0f, 1f);
        float externalValue = 1f;
        if (foodSource != null)
        {
            float distance = Vector3.Distance(transform.position, foodSource.position);
            externalValue = Mathf.Clamp01(distance / 20f);
        }
        return new FuzzyState(internalValue, externalValue);
    }

    // Computes similarity between two fuzzy states in [0,1] based on absolute differences.
    float CalculateFuzzySimilarity(FuzzyState stateA, FuzzyState stateB)
    {
        float diffInternal = Mathf.Abs(stateA.internalValue - stateB.internalValue);
        float diffExternal = Mathf.Abs(stateA.externalValue - stateB.externalValue);
        float similarity = 1f - ((diffInternal + diffExternal) / 2f);
        return Mathf.Clamp01(similarity);
    }

    // Discretizes the robot’s distance to the food source into a state index using binSize.
    int GetState()
    {
        float distance = Vector3.Distance(transform.position, foodSource.position);
        int state = Mathf.FloorToInt(distance / binSize);
        return state;
    }

    // Implements ε‐greedy: with probability explorationRate pick random action, otherwise choose max‐Q action.
    int ChooseAction(int state)
    {
        
        if (Random.value < explorationRate)
            return Random.Range(0, numActions);

        float maxQ = Enumerable
            .Range(0, numActions)
            .Select(a => GetQValue(state, a))
            .Max();

        var bestActions = Enumerable
            .Range(0, numActions)
            .Where(a => Mathf.Approximately(GetQValue(state, a), maxQ))
            .ToList();

        int choice = bestActions[ Random.Range(0, bestActions.Count) ];
        return choice;
    }

    // Retrieves Q‐value for (state, action) from the Q‐table, initializing to 0 if missing.
    float GetQValue(int state, int action)
    {
        var key = (state, action);
        if (qTable.ContainsKey(key))
        {
            return qTable[key];
        }
        else
        {
            qTable[key] = 0.0f;
            return 0.0f;
        }
    }

    // Sets (or updates) the Q‐table entry for a given (state, action).
    void SetQValue(int state, int action, float value)
    {
        qTable[(state, action)] = value;
    }

    // Converts an action index into a world‐space direction and returns a point wanderRadius away.
    Vector3 ComputeDestinationFromAction(int action)
    {
        
        float angle = action * (360f / numActions);
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        
        Vector3 destination = transform.position + direction.normalized * wanderRadius;
        return destination;
    }

    // Draws the robot’s FOV cone and any empathetic no‐food zones in the editor for debugging.
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * detectionRange);
        int segments = 20;
        float angleStep = fieldOfViewAngle / segments;
        Vector3 previousPoint = transform.position + leftBoundary * detectionRange;
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -fieldOfViewAngle / 2 + angleStep * i;
            Vector3 currentPoint = transform.position + (Quaternion.Euler(0, currentAngle, 0) * transform.forward) * detectionRange;
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
        if (useEmpatheticBehavior)
        {
            Gizmos.color = Color.red;
            foreach (Vector3 area in noFoodAreas)
            {
                Gizmos.DrawWireSphere(area, noFoodAreaRadius);
            }
        }
    }
}