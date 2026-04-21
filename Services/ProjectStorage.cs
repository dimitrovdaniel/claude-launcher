using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ClaudeLauncher.Models;

namespace ClaudeLauncher.Services;

public static class ProjectStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string StorageDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeLauncher");

    public static string StoragePath => Path.Combine(StorageDirectory, "projects.json");

    public static List<Project> Load()
    {
        if (!File.Exists(StoragePath))
            return new List<Project>();

        try
        {
            var json = File.ReadAllText(StoragePath);
            return JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
        }
        catch (JsonException)
        {
            return new List<Project>();
        }
    }

    public static void Save(IEnumerable<Project> projects)
    {
        Directory.CreateDirectory(StorageDirectory);
        var json = JsonSerializer.Serialize(projects, JsonOptions);
        File.WriteAllText(StoragePath, json);
    }
}
