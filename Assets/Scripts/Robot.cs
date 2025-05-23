using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using Random = UnityEngine.Random;

// A simple struct representing a fuzzy state for the robot.
public struct FuzzyState {
    public float internalValue; // e.g. representing internal capability or energy level
    public float externalValue; // e.g. representing the distance to the food source

    public FuzzyState(float internalValue, float externalValue) {
        this.internalValue = internalValue;
        this.externalValue = externalValue;
    }
}

public class Robot : MonoBehaviour
{
    // === Existing Public Fields ===
    public string robotId;  // e.g. set per-instance in the Inspector
    private KnowledgePersistence persistence;
    // flat (state,action) → Q-value
    private Dictionary<(int state, int action), float> qTable;

    public float detectionRange = 10f;
    public float moveSpeed = 3.5f;
    public float wanderRadius = 10f;
    public bool useBaseKnowledge = false;
    public bool useEmpatheticBehavior = true; // Toggle to enable empathetic sharing and no-food memory
    public Transform foodSource;
    public Color searchingColor = Color.blue;
    public Color foundFoodColor = Color.green;
    public Color obstacleDetectedColor = Color.red;
    public float fieldOfViewAngle = 60f; // Field of view angle for vision
    [HideInInspector]
    public bool showFOV = false;
    public int fovSegments = 20;
    
    // === RL Integration Flag ===
    public bool useReinforcementLearning = true; // Toggle RL-based behavior

    // === Existing Private Fields ===
    private NavMeshAgent agent;
    private bool foodFound = false;
    private static Vector3 lastKnownFoodLocation = Vector3.zero;
    private Renderer robotRenderer;
    private List<Vector3> visitedLocations = new List<Vector3>(); // Stores visited locations
    private static int robotsThatFoundFood = 0;
    private static float simulationStartTime;

    // List to store areas where no food was found (used only when empathetic behavior is on)
    private static List<Vector3> noFoodAreas = new List<Vector3>();
    public static float noFoodAreaRadius = 5f; // Radius to consider an area as "no-food"

    // A small threshold to determine if a candidate point is considered visited.
    public float memoryRadius = 1f;
    // Maximum number of visited locations to remember.
    public int maxMemoryCount = 10;

    // === RL-Specific Fields ===
    // Q-table: mapping (state, action) pair to Q-value.
    ///private Dictionary<(int, int), float> qTable = new Dictionary<(int, int), float>();

    // Parameters for Q-learning.
    private float learningRate = 0.1f;
    private float discountFactor = 0.95f;
    private float explorationRate = 0.1f;
    private float binSize = 5.0f; // Bin size for discretizing the distance to food.
    private int numActions = 8;  // 8 possible directions (every 45 degrees).

    private int currentState;
    private int currentAction;
    private Vector3 previousPosition;
    private LineRenderer fovRenderer;

    // === Initialization in Start ===
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        robotRenderer = GetComponent<Renderer>();
        // FOV visualization
        fovRenderer = gameObject.AddComponent<LineRenderer>();
        fovRenderer.material = new Material(Shader.Find("Sprites/Default"));
        fovRenderer.widthMultiplier = 0.02f;
        fovRenderer.loop = false;
        fovRenderer.useWorldSpace = true;
        fovRenderer.positionCount = fovSegments + 2;
        fovRenderer.enabled = showFOV;
        robotRenderer.material.color = searchingColor;

        // Start simulation timer for the first robot.
        if (robotsThatFoundFood == 0)
        {
            simulationStartTime = Time.time;
        }
        
        // If using reinforcement learning, initialize RL parameters.
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
            // Existing behavior: load persistent knowledge.
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
        
        // === Reinforcement Learning Update ===
        // When using RL, check if the robot has nearly reached its destination.
        if (useReinforcementLearning && !foodFound && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Get the new state.
            int newState = GetState();
            
            // Calculate reward using the specified mathematical formula.
            // Here we use an exponential decay formula based on the change in distance to food:
            // reward = exp(–λ * previousDistance) – exp(–λ * currentDistance)
            float lambda = 0.1f;
            float previousDistance = Vector3.Distance(previousPosition, foodSource.position);
            float currentDistance = Vector3.Distance(transform.position, foodSource.position);
            float reward = Mathf.Exp(-lambda * previousDistance) - Mathf.Exp(-lambda * currentDistance);
            
            // Additional bonus reward if food is reached.
            if (foodFound)
            {
                reward += 10f;
            }
            
            // Q-learning update: Get max Q-value for the new state.
            float maxQNext = float.MinValue;
            for (int a = 0; a < numActions; a++)
            {
                float qVal = GetQValue(newState, a);
                if (qVal > maxQNext) { maxQNext = qVal; }
            }
            float oldQ = GetQValue(currentState, currentAction);
            float newQ = oldQ + learningRate * (reward + discountFactor * maxQNext - oldQ);
            SetQValue(currentState, currentAction, newQ);

            // Update the state and choose the next action.
            currentState = newState;
            currentAction = ChooseAction(newState);
            previousPosition = transform.position;
            
            // Compute the new destination based on the chosen action.
            Vector3 newDestination = ComputeDestinationFromAction(currentAction);
            agent.SetDestination(newDestination);
        }
        if (fovRenderer != null)
        {
            fovRenderer.enabled = showFOV;
            if (showFOV) DrawFOV();
        }
    }

    void Awake()
    {
        persistence = new KnowledgePersistence(robotId);
        persistence.LoadQTable(out qTable);
    }

    void OnDestroy()
    {
        persistence.SaveQTable(qTable);
    }

    void DrawFOV()
    {
        float step = fieldOfViewAngle / fovSegments;
        float startAngle = -fieldOfViewAngle / 2f;
        fovRenderer.positionCount = fovSegments + 2;

        // central point
        fovRenderer.SetPosition(0, transform.position);
        for (int i = 0; i <= fovSegments; i++)
        {
            float ang = startAngle + step * i;
            Vector3 dir = Quaternion.Euler(0, ang, 0) * transform.forward;
            Vector3 point = transform.position + dir.normalized * detectionRange;
            fovRenderer.SetPosition(i + 1, point);
        }
    }

    // === Existing Methods (ContinuousMovement, Wander, etc.) ===
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

    bool IsVisited(Vector3 candidate)
    {
        foreach (Vector3 visited in visitedLocations)
        {
            if (Vector3.Distance(candidate, visited) < memoryRadius)
                return true;
        }
        return false;
    }

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
        else if (other.CompareTag("Obstacle"))
        {
            AvoidObstacle();
        }
    }

    void AvoidObstacle()
    {
        Vector3 newDirection = transform.position + Random.insideUnitSphere * wanderRadius;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newDirection, out navHit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
    }

    void SaveFoodLocation(Vector3 location)
    {
        KnowledgeBaseManager.AddFoodLocation(location);
    }

    void CheckSimulationEnd()
    {
        if (robotsThatFoundFood >= FindObjectsOfType<Robot>().Length)
        {
            float elapsedTime = Time.time - simulationStartTime;
            Debug.Log("Simulation finished in " + elapsedTime + " seconds.");
            StartCoroutine(RobotManager.Instance.RestartSimulation());
        }
    }

    public static void ResetSimulationVariables()
    {
        robotsThatFoundFood = 0;
        simulationStartTime = Time.time;
        noFoodAreas.Clear();
    }

    public static float GetSimulationStartTime()
    {
        return simulationStartTime;
    }

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

    float CalculateFuzzySimilarity(FuzzyState stateA, FuzzyState stateB)
    {
        float diffInternal = Mathf.Abs(stateA.internalValue - stateB.internalValue);
        float diffExternal = Mathf.Abs(stateA.externalValue - stateB.externalValue);
        float similarity = 1f - ((diffInternal + diffExternal) / 2f);
        return Mathf.Clamp01(similarity);
    }

    // === RL Helper Methods ===

    // Discretize current distance to food into a state.
    int GetState()
    {
        float distance = Vector3.Distance(transform.position, foodSource.position);
        int state = Mathf.FloorToInt(distance / binSize);
        return state;
    }

    // Epsilon-greedy action selection.
    int ChooseAction(int state)
    {
        if (Random.value < explorationRate)
        {
            return Random.Range(0, numActions);
        }
        else
        {
            float maxQ = float.MinValue;
            int bestAction = 0;
            for (int a = 0; a < numActions; a++)
            {
                float q = GetQValue(state, a);
                if (q > maxQ)
                {
                    maxQ = q;
                    bestAction = a;
                }
            }
            return bestAction;
        }
    }

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

    void SetQValue(int state, int action, float value)
    {
        qTable[(state, action)] = value;
    }

    // Given an action (discrete 0 to numActions-1), compute a destination.
    Vector3 ComputeDestinationFromAction(int action)
    {
        // Compute the directional angle (in degrees) for the action.
        float angle = action * (360f / numActions);
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        // Use wanderRadius as the step length (adjust if needed).
        Vector3 destination = transform.position + direction.normalized * wanderRadius;
        return destination;
    }

    // === Gizmo Drawing (Optional) ===
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