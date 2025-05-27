using UnityEngine;

public class DebugSimulationUI : MonoBehaviour
{
    private bool isSimulationRunning = false;
    private int   numberOfRobots = 5;
    private int   maxSimulations = 3;
    private bool  useReinforcementLearning = true;
    private bool  useBaseKnowledge = false;
    private float memoryRadius = 1f;
    private int   maxMemoryCount = 10;
    private bool  showFOV = false;

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 400), "Simulation Controls", GUI.skin.window);
        useReinforcementLearning = GUILayout.Toggle(useReinforcementLearning, "Use Reinforcement Learning");
        useBaseKnowledge         = GUILayout.Toggle(useBaseKnowledge,         "Use Base Knowledge");


        GUILayout.Label($"Robots: {numberOfRobots}");
        numberOfRobots = (int)GUILayout.HorizontalSlider(numberOfRobots, 1, 100);

        GUILayout.Label($"Max Runs: {maxSimulations}");
        maxSimulations = (int)GUILayout.HorizontalSlider(maxSimulations, 1, 1000);

        GUILayout.Space(10);
        GUILayout.Label($"Memory Radius: {memoryRadius:F1}");
        memoryRadius = GUILayout.HorizontalSlider(memoryRadius, 0.1f, 10f);

        GUILayout.Label($"Max Memory: {maxMemoryCount}");
        maxMemoryCount = (int)GUILayout.HorizontalSlider(maxMemoryCount, 1, 100);

        GUILayout.Space(10);
        showFOV = GUILayout.Toggle(showFOV, "Show Field of View");

        GUILayout.Space(10);
        if (!isSimulationRunning)
        {
            if (GUILayout.Button("▶ Start Simulation"))
            {
                ApplySettings();
                RobotManager.Instance.StartSimulation();
                isSimulationRunning = true;
            }
            if (GUILayout.Button("⟳ Reset Simulation"))
            {
                Robot.SaveModelNow();
                RobotManager.Instance.ResetToInitial();
                isSimulationRunning = false;
            }
        }
        else
        {
            if (GUILayout.Button("■ Stop Simulation"))
            {
                Robot.SaveModelNow();
                RobotManager.Instance.ResetToInitial();
                isSimulationRunning = false;
            }
        }

        GUILayout.EndArea();
    }

    void ApplySettings()
    {
        var manager = FindObjectOfType<RobotManager>();
        if (manager != null)
        {
            manager.numberOfRobots = numberOfRobots;
            manager.maxSimulations = maxSimulations;
            manager.useReinforcementLearning  = useReinforcementLearning;
            manager.useBaseKnowledge = useBaseKnowledge;
            manager.memoryRadius = memoryRadius;
            manager.maxMemoryCount = maxMemoryCount;
            manager.showFOV = showFOV;
        }

        foreach (var robot in FindObjectsOfType<Robot>())
        {
            robot.memoryRadius = memoryRadius;
            robot.maxMemoryCount = maxMemoryCount;
            robot.useReinforcementLearning = useReinforcementLearning;
            robot.useBaseKnowledge = useBaseKnowledge;
            robot.showFOV = showFOV;
        }
    }
}