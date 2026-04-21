using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClaudeLauncher.Models;

namespace ClaudeLauncher.Services;

public static class NotesService
{
    public const string NotesSubPath = ".claude-launcher/notes";
    private const string Extension = ".md";

    public static string GetNotesDirectory(string repoPath) =>
        Path.Combine(repoPath, NotesSubPath);

    public static string GetRelativePath(string name) =>
        $"{NotesSubPath}/{EnsureExtension(name)}";

    public static IReadOnlyList<Note> List(string repoPath)
    {
        if (string.IsNullOrWhiteSpace(repoPath)) return Array.Empty<Note>();
        var dir = GetNotesDirectory(repoPath);
        if (!Directory.Exists(dir)) return Array.Empty<Note>();

        return Directory.EnumerateFiles(dir, "*" + Extension)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(f => new Note(Path.GetFileNameWithoutExtension(f), File.ReadAllText(f)))
            .ToList();
    }

    public static void Save(string repoPath, string name, string content)
    {
        var dir = GetNotesDirectory(repoPath);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, EnsureExtension(name)), content);
    }

    public static void Delete(string repoPath, string name)
    {
        var path = Path.Combine(GetNotesDirectory(repoPath), EnsureExtension(name));
        if (File.Exists(path)) File.Delete(path);
    }

    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return name.IndexOfAny(InvalidChars) < 0;
    }

    private static readonly char[] InvalidChars =
        Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }).Distinct().ToArray();

    private static string EnsureExtension(string name) =>
        name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) ? name : name + Extension;
}
