using UnityEngine;
using System.Linq;

public class DebugSimulationUI : MonoBehaviour
{
    private Vector2 scrollPosition = Vector2.zero;
    private bool isSimulationRunning = false;
    private int numberOfRobots = 5;
    private int maxSimulations = 3;
    private float memoryRadius = 1f;
    private int maxMemoryCount = 10;
    private bool showFOV = false;
    private bool uiTrainingMode = true;
    private bool uiUseReinforcement = true;
    private bool uiUseBaseKnowledge = false;
    private bool uiUseEmpathy = true;
    private int uiSelectedMap = 0;
    private string[] mapNames;

    // Called when this UI component is enabled: fetches map names and syncs selectedMapIndex from RobotManager.
    void OnEnable()
    {
        var mgr = FindObjectOfType<RobotManager>();
        mapNames = mgr.mapRoots.Select(r => r.name).ToArray();
        uiSelectedMap = mgr.selectedMapIndex;
    }

    // Renders the simulation control window: sliders, toggles, buttons, and handles user input.
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 400), "Simulation Controls", GUI.skin.window);
        scrollPosition = GUILayout.BeginScrollView(
        scrollPosition,
        GUILayout.Width(300),
        GUILayout.Height(360)
        );

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
        
        GUILayout.BeginVertical("box");
        uiTrainingMode = GUILayout.Toggle(uiTrainingMode, "Training Mode");
        uiUseReinforcement = GUILayout.Toggle(uiUseReinforcement, "Use Reinforcement Learning");
        uiUseBaseKnowledge = GUILayout.Toggle(uiUseBaseKnowledge, "Use Base Knowledge");
        uiUseEmpathy = GUILayout.Toggle(uiUseEmpathy, "Use Empathetic Behavior");
        GUILayout.EndVertical();

        GUILayout.Label("Map:");
        uiSelectedMap = GUILayout.SelectionGrid(
            uiSelectedMap,
            mapNames,
            1,
            GUILayout.Width(150)
        );

        if (uiSelectedMap != RobotManager.Instance.selectedMapIndex)
        {
            RobotManager.Instance.ActivateMap(uiSelectedMap);
            RobotManager.Instance.ResetToInitial();
        }

        if (GUI.changed)
        {
            ApplyTogglesToRobots();
        }

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
                if (RobotManager.Instance.isTrainingMode)
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
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // Iterates over all active Robot instances and updates their mode/toggle fields based on the UI.
    private void ApplyTogglesToRobots()
    {
        var all = FindObjectsOfType<Robot>();
        foreach (var r in all)
        {
            r.isTrainingMode = uiTrainingMode;
            r.useReinforcementLearning = uiUseReinforcement;
            r.useBaseKnowledge = uiUseBaseKnowledge;
            r.useEmpatheticBehavior = uiUseEmpathy;
        }
        Debug.Log($"[UI] Applied toggles → Training:{uiTrainingMode}  RL:{uiUseReinforcement}  BaseKB:{uiUseBaseKnowledge}  Empathy:{uiUseEmpathy}");
    }

    // Pushes the current UI slider values to the RobotManager and updates all spawned robots accordingly.
    void ApplySettings()
    {
        var manager = FindObjectOfType<RobotManager>();
        if (manager != null)
        {
            manager.isTrainingMode = uiTrainingMode;
            manager.numberOfRobots = numberOfRobots;
            manager.maxSimulations = maxSimulations;
            manager.useReinforcementLearning = uiUseReinforcement;
            manager.useBaseKnowledge = uiUseBaseKnowledge;
            manager.useEmpatheticBehavior = uiUseEmpathy;
            manager.memoryRadius = memoryRadius;
            manager.maxMemoryCount = maxMemoryCount;
            manager.showFOV = showFOV;
        }

        foreach (var robot in FindObjectsOfType<Robot>())
        {
            robot.isTrainingMode = uiTrainingMode;
            robot.memoryRadius = memoryRadius;
            robot.maxMemoryCount = maxMemoryCount;
            robot.useReinforcementLearning = uiUseReinforcement;
            robot.useBaseKnowledge = uiUseBaseKnowledge;
            robot.showFOV = showFOV;
        }
    }
}