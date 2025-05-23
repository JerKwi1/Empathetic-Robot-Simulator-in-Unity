using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class KnowledgePersistence
{
    private readonly string path;

    public KnowledgePersistence(string robotId)
    {
        path = Path.Combine(Application.persistentDataPath, $"robot_{robotId}_qtable.json");
    }

    // Save the flat Q-table
    public void SaveQTable(Dictionary<(int state, int action), float> qTable)
    {
        var data = new QTableData();
        foreach (var kv in qTable)
        {
            data.entries.Add(new QEntry {
                stateKey  = kv.Key.state,
                actionKey = kv.Key.action,
                value     = kv.Value
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log($"[Persistence] Saved Q-table ({qTable.Count} entries) to {path}");
    }

    // Load into a flat Q-table
    public void LoadQTable(out Dictionary<(int state, int action), float> qTable)
    {
        qTable = new Dictionary<(int state, int action), float>();

        if (!File.Exists(path))
        {
            Debug.Log($"[Persistence] No Q-table found at {path}; starting empty.");
            return;
        }

        string json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<QTableData>(json);

        foreach (var e in data.entries)
        {
            var key = (e.stateKey, e.actionKey);
            if (!qTable.ContainsKey(key))
                qTable[key] = e.value;
        }

        Debug.Log($"[Persistence] Loaded Q-table ({qTable.Count} entries) from {path}");
    }
}