using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClaudeLauncher.Services;

public static class ClientColorStorage
{
    public const string DefaultColor = "#CBD5E1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string StoragePath =>
        Path.Combine(ProjectStorage.StorageDirectory, "client-colors.json");

    public static Dictionary<string, string> Load()
    {
        if (!File.Exists(StoragePath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var json = File.ReadAllText(StoragePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return loaded is null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void Save(IDictionary<string, string> colors)
    {
        Directory.CreateDirectory(ProjectStorage.StorageDirectory);
        var json = JsonSerializer.Serialize(colors, JsonOptions);
        File.WriteAllText(StoragePath, json);
    }
}
