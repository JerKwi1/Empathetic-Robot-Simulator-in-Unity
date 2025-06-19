using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class KnowledgePersistence
{
    private readonly string loadPath;
    private readonly string savePath;

    // Initializes persistence with given filenames (located in the executable folder).
    // If saveFileName is null, saving is disabled.
    public KnowledgePersistence(string loadFileName, string saveFileName = null)
    {
        var exeFolder = System.AppDomain.CurrentDomain.BaseDirectory;
        loadPath = Path.Combine(exeFolder, loadFileName);
        savePath = saveFileName != null
            ? Path.Combine(exeFolder, saveFileName)
            : null;
    }

    // Serializes the provided Q‐table dictionary into JSON and writes it to savePath.
    // If savePath is null, this method exits immediately (no saving).
    public void SaveQTable(Dictionary<(int state, int action), float> qTable)
    {
        if (savePath == null) return;
        var data = new QTableData();
        foreach (var kv in qTable)
        {
            data.entries.Add(new QEntry
            {
                stateKey = kv.Key.state,
                actionKey = kv.Key.action,
                value = kv.Value
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[Persistence] Saved model ({qTable.Count} entries) to {savePath}");
    }

    // Reads JSON from loadPath (if it exists), deserializes into QTableData,
    // and populates the output dictionary. If no file is found, starts with an empty table.
    public void LoadQTable(out Dictionary<(int state, int action), float> qTable)
    {
        qTable = new Dictionary<(int, int), float>();
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