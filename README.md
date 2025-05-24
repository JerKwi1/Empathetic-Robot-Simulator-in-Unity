# Robot Swarm Simulator with Artificial Empathy

A Unity-based swarm simulation where autonomous robot agents cooperate, share knowledge, and learn empathetically to locate resources (food). Includes reinforcement learning, fuzzy-empathy modeling, and configurable simulation parameters.

---

## 🚀 Features

- **Autonomous Agents**: Robots navigate via Unity's `NavMeshAgent` and seek targets dynamically.
- **Artificial Empathy**: Agents share and adopt discovered information based on a fuzzy-similarity model.
- **Reinforcement Learning**: Q-learning with reward shaping and epsilon-greedy policy for adaptive behaviors.
- **Knowledge Persistence**: Food/no-food locations saved to text files for persistent memory across runs.
- **Configurable UI**: In-game debug panel to adjust robot count, toggle learning or knowledge modes, and control runs.
- **Performance Tracking**: Automatic logging of time-to-target for analysis and benchmarking.

---

## 📁 Repository Structure

```bash
/Empathetic-Robot-Simulator-in-Unity
├── Assets/                # Unity project folder
│   ├── Scripts
│   │   ├── Robot.cs
│   │   ├── RobotManager.cs
│   │   ├── KnowledgeBaseManager.cs
│   │   ├── PerformanceTracker.cs
│   │   ├── DebugSimulationUI.cs
│   │   └── CameraController.cs
│   └── ...                # Other Unity assets (Scenes, Prefabs, Materials)
├── Documentation/         # PDF Files
│   └── TechnicalDocumentation.pdf
├── .gitignore
└── README.md              # ← You are here
```

---

## 🎯 Getting Started

### Prerequisites

- **Unity 2023.3** or later (LTS recommended)
- **.NET 4.x** scripting runtime
- Git (for cloning)

### Clone the Repository

```bash
git clone https://github.com/JerKwi1/Empathetic-Robot-Simulator-in-Unity.git
cd Empathetic-Robot-Simulator-in-Unity
```

### Open in Unity

1. Launch Unity Hub.
2. Click **Add** and select this project folder.
3. Open the project.
4. Open the main scene (e.g., `Assets/Scenes/Main.unity`).
5. Press **Play** to run the simulation in the Editor.

### Run the Compiled Build

1. Navigate to Realeses and download latest zip file.
2. On Windows, double-click `RobotSwarm.exe`.
3. On Mac/Linux, run the appropriate executable or shell script.

---

## ⚙️ Configuration

- **DebugSimulationUI** panel (in-game UI):
  - Toggle **Reinforcement Learning** and **Knowledge Mode**.
  - Adjust **Robot Count**, **Simulation Runs**, and **Memory Settings**.
  - **Start**, **Stop**, and **Reset** simulation controls.

- **KnowledgeBaseManager** files (in `PersistentDataPath`):
  - `KnowledgeBase.txt` (food locations)
  - `NoFoodAreas.txt` (empty searches)

- **RobotManager**:
  - Configure total runs, spawn positions, and logging path.

---

## 🛠️ Maintenance and Contributions

See [Documentation/MaintenanceDocumentation.pdf](Documentation/MaintenanceDocumentation.pdf) for improvement suggestions:

- Code refactoring and modularization
- Performance optimizations and profiling
- Advanced RL (DQN, multi-agent frameworks)
- Extended empathy/social network modeling
- CI/CD with automated testing and builds
