using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class KnowledgePersistence
{
    private readonly string loadPath;
    private readonly string savePath;

    public KnowledgePersistence(string loadFileName, string saveFileName = null)
    {
        loadPath = Path.Combine(Application.persistentDataPath, loadFileName);
        savePath = saveFileName != null
            ? Path.Combine(Application.persistentDataPath, saveFileName)
            : null;
    }

    // Save the flat Q-table
    public void SaveQTable(Dictionary<(int state, int action), float> qTable)
    {
        if (savePath == null) return;      // skip in eval mode
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
        File.WriteAllText(savePath, json);
        Debug.Log($"[Persistence] Saved model ({qTable.Count} entries) to {savePath}");
    }

    // Load into a flat Q-table
    public void LoadQTable(out Dictionary<(int state, int action), float> qTable)
    {
        qTable = new Dictionary<(int,int),float>();
        if (!File.Exists(loadPath))
        {
            Debug.Log($"[Persistence] No base model at {loadPath}; starting empty.");
            return;
        }

        string json = File.ReadAllText(loadPath);
        var data = JsonUtility.FromJson<QTableData>(json);

        foreach (var e in data.entries)
        {
            var key = (e.stateKey, e.actionKey);
            if (!qTable.ContainsKey(key))
                qTable[key] = e.value;
        }

        Debug.Log($"[Persistence] Loaded model ({qTable.Count} entries) from {loadPath}");
    }
}