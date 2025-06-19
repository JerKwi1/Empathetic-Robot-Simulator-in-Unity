using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class KnowledgeBaseManager
{
    private static readonly string exeFolder = System.AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string foodKnowledgeFilePath = Path.Combine(exeFolder, "KnowledgeBase.txt");
    private static readonly string noFoodFilePath = Path.Combine(exeFolder, "NoFoodAreas.txt");

    private static List<Vector3> foodLocations = new List<Vector3>();
    private static List<Vector3> noFoodAreas = new List<Vector3>();

    // Clears current foodLocations and loads saved Vector3s from text file (comma‐separated x,y,z).
    public static void LoadKnowledgeBase()
    {
        foodLocations.Clear();
        if (File.Exists(foodKnowledgeFilePath))
        {
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

    // Writes all entries in foodLocations to the KnowledgeBase.txt file in “x,y,z” lines.
    public static void SaveKnowledgeBase()
    {
        List<string> lines = new List<string>();
        foreach (Vector3 loc in foodLocations)
        {
            lines.Add(loc.x + "," + loc.y + "," + loc.z);
        }
        File.WriteAllLines(foodKnowledgeFilePath, lines.ToArray());
    }

    // Adds a new food position to the in‐memory list and immediately persists to disk.
    public static void AddFoodLocation(Vector3 location)
    {
        foodLocations.Add(location);
        SaveKnowledgeBase();
    }

    // Returns the most recently added food location (or Vector3.zero if none exist).
    public static Vector3 GetBestFoodLocation()
    {
        if (foodLocations.Count > 0)
        {
            return foodLocations[foodLocations.Count - 1];
        }
        return Vector3.zero;
    }

    // Clears current noFoodAreas and loads saved Vector3s from NoFoodAreas.txt.
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

    // Writes all entries in noFoodAreas to the NoFoodAreas.txt file in “x,y,z” lines.
    public static void SaveNoFoodAreas()
    {
        List<string> lines = new List<string>();
        foreach (Vector3 loc in noFoodAreas)
        {
            lines.Add(loc.x + "," + loc.y + "," + loc.z);
        }
        File.WriteAllLines(noFoodFilePath, lines.ToArray());
    }

    // Adds a new no‐food area center to the in‐memory list and immediately persists to disk.
    public static void AddNoFoodArea(Vector3 location)
    {
        noFoodAreas.Add(location);
        SaveNoFoodAreas();
    }

    // Deletes any saved no‐food area within the given radius of the specified location, then updates disk.
    public static void RemoveNoFoodAreaCloseTo(Vector3 location, float radius)
    {
        noFoodAreas.RemoveAll(area => Vector3.Distance(area, location) < radius);
        SaveNoFoodAreas();
    }

    // Provides external access to the current list of no‐food area centers.
    public static List<Vector3> GetNoFoodAreas()
    {
        return noFoodAreas;
    }
}