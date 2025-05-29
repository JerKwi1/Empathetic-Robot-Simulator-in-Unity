using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class KnowledgeBaseManager
{
    // Files for persistent knowledge.
    private static string foodKnowledgeFilePath = Path.Combine(Application.dataPath, "KnowledgeBase.txt");
    private static string noFoodFilePath = Path.Combine(Application.dataPath, "NoFoodAreas.txt");

    // In-memory lists
    private static List<Vector3> foodLocations = new List<Vector3>();
    private static List<Vector3> noFoodAreas = new List<Vector3>();

    // Food Locations Methods

    public static void LoadKnowledgeBase()
    {
        foodLocations.Clear();
        if (File.Exists(foodKnowledgeFilePath))
        {
            // Each line is expected to be "x,y,z"
            string[] lines = File.ReadAllLines(foodKnowledgeFilePath);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(',');
                if (tokens.Length == 3 &&
                    float.TryParse(tokens[0], out float x) &&
                    float.TryParse(tokens[1], out float y) &&
                    float.TryParse(tokens[2], out float z))
                {
                    foodLocations.Add(new Vector3(x, y, z));
                }
            }
        }
    }

    public static void SaveKnowledgeBase()
    {
        List<string> lines = new List<string>();
        foreach (Vector3 loc in foodLocations)
        {
            lines.Add(loc.x + "," + loc.y + "," + loc.z);
        }
        File.WriteAllLines(foodKnowledgeFilePath, lines.ToArray());
    }

    public static void AddFoodLocation(Vector3 location)
    {
        // Optionally avoid duplicates using a proximity check.
        foodLocations.Add(location);
        SaveKnowledgeBase();
    }

    public static Vector3 GetBestFoodLocation()
    {
        if (foodLocations.Count > 0)
        {
            // Example: simply return the most recent location.
            return foodLocations[foodLocations.Count - 1];
        }
        return Vector3.zero;
    }

    // No-Food Areas Methods

    public static void LoadNoFoodAreas()
    {
        noFoodAreas.Clear();
        if (File.Exists(noFoodFilePath))
        {
            string[] lines = File.ReadAllLines(noFoodFilePath);
            foreach (string line in lines)
            {
                string[] tokens = line.Split(',');
                if (tokens.Length == 3 &&
                    float.TryParse(tokens[0], out float x) &&
                    float.TryParse(tokens[1], out float y) &&
                    float.TryParse(tokens[2], out float z))
                {
                    noFoodAreas.Add(new Vector3(x, y, z));
                }
            }
        }
    }

    public static void SaveNoFoodAreas()
    {
        List<string> lines = new List<string>();
        foreach (Vector3 loc in noFoodAreas)
        {
            lines.Add(loc.x + "," + loc.y + "," + loc.z);
        }
        File.WriteAllLines(noFoodFilePath, lines.ToArray());
    }

    public static void AddNoFoodArea(Vector3 location)
    {
        // Optionally check for duplicates or merge nearby points.
        noFoodAreas.Add(location);
        SaveNoFoodAreas();
    }

    public static void RemoveNoFoodAreaCloseTo(Vector3 location, float radius)
    {
        noFoodAreas.RemoveAll(area => Vector3.Distance(area, location) < radius);
        SaveNoFoodAreas();
    }

    public static List<Vector3> GetNoFoodAreas()
    {
        return noFoodAreas;
    }
}