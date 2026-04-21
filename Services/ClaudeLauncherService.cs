using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ClaudeLauncher.Models;

namespace ClaudeLauncher.Services;

public readonly record struct LaunchResult(bool ResumeSkippedNoPriorSession);

public static class ClaudeLauncherService
{
    private static readonly string[] LinuxTerminals =
        { "x-terminal-emulator", "gnome-terminal", "konsole", "xfce4-terminal", "xterm" };

    public static LaunchResult Launch(Project project)
    {
        if (string.IsNullOrWhiteSpace(project.RepositoryPath))
            throw new InvalidOperationException("Repository path is required.");

        if (!Directory.Exists(project.RepositoryPath))
            throw new DirectoryNotFoundException($"Repository folder not found: {project.RepositoryPath}");

        var hasPriorSession = HasPriorSession(project.RepositoryPath);
        var flags = BuildFlags(project, resume: project.ResumeLastSession && hasPriorSession);
        var prompt = BuildPrompt(project);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            LaunchWindows(project, flags, prompt);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            LaunchMac(project, flags, prompt);
        else
            LaunchLinux(project, flags, prompt);

        return new LaunchResult(project.ResumeLastSession && !hasPriorSession);
    }

    private static IReadOnlyList<string> BuildFlags(Project p, bool resume)
    {
        var flags = new List<string>();
        if (p.DangerouslySkipPermissions) flags.Add("--dangerously-skip-permissions");
        if (resume) flags.Add("--continue");
        return flags;
    }

    // Claude Code keeps per-repo sessions under ~/.claude/projects/<encoded>/*.jsonl.
    // The encoded name replaces path separators, the drive colon, and spaces with '-'.
    private static bool HasPriorSession(string repoPath)
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home)) return false;
            var dir = Path.Combine(home, ".claude", "projects", EncodeProjectPath(repoPath));
            return Directory.Exists(dir) && Directory.EnumerateFiles(dir, "*.jsonl").Any();
        }
        catch
        {
            return false;
        }
    }

    private static string EncodeProjectPath(string path)
    {
        var full = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var chars = full.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (c == '\\' || c == '/' || c == ':' || c == ' ')
                chars[i] = '-';
        }
        return new string(chars);
    }

    private static string BuildPrompt(Project project)
    {
        var sb = new StringBuilder();

        var notes = NotesService.List(project.RepositoryPath);
        if (notes.Count > 0)
        {
            sb.AppendLine("Read these persistent project notes first — they contain guidelines to follow:");
            foreach (var note in notes)
                sb.AppendLine($"- {NotesService.GetRelativePath(note.Name)}");
            sb.AppendLine();
        }

        if (project.RelevantFiles.Count > 0)
        {
            sb.AppendLine("Please read the following project files to familiarize yourself with the codebase:");
            foreach (var file in project.RelevantFiles)
                sb.AppendLine($"- {file}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(project.SessionNotesPath))
        {
            var sessionNotes = project.SessionNotesPath.Trim();
            sb.AppendLine(
                $"If `{sessionNotes}` exists, read it first to pick up context from prior sessions. " +
                "When I send \"checkpoint\" as my entire next message, append a concise dated summary of our work so far — " +
                $"decisions made, files changed, open questions — to `{sessionNotes}`. " +
                "Create the file if it doesn't exist. Do not do this until I explicitly say \"checkpoint\".");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(project.CustomPrompt))
            sb.Append(project.CustomPrompt.Trim());

        return sb.ToString().Trim();
    }

    private static void LaunchWindows(Project project, IReadOnlyList<string> flags, string prompt)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "claude",
            WorkingDirectory = project.RepositoryPath,
            UseShellExecute = true,
        };
        foreach (var flag in flags)
            psi.ArgumentList.Add(flag);
        if (!string.IsNullOrEmpty(prompt))
            psi.ArgumentList.Add(prompt);

        Process.Start(psi);
    }

    private static void LaunchMac(Project project, IReadOnlyList<string> flags, string prompt)
    {
        var shellCmd = BuildShellCommand(project, flags, prompt);
        var osaCmd = $"tell application \"Terminal\" to do script \"{EscapeForAppleScript(shellCmd)}\"";

        Process.Start(new ProcessStartInfo
        {
            FileName = "osascript",
            ArgumentList = { "-e", osaCmd },
            UseShellExecute = false
        });
    }

    private static void LaunchLinux(Project project, IReadOnlyList<string> flags, string prompt)
    {
        var script = $"#!/usr/bin/env bash\n{BuildShellCommand(project, flags, prompt)}\n";
        var tempSh = Path.Combine(Path.GetTempPath(), $"claude-launcher-{Guid.NewGuid():N}.sh");
        File.WriteAllText(tempSh, script);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                File.SetUnixFileMode(tempSh,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        foreach (var term in LinuxTerminals)
        {
            var args = term == "xfce4-terminal"
                ? $"-e \"bash -c '{tempSh}; exec bash'\""
                : $"-- bash -c '{tempSh}; exec bash'";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = term,
                    Arguments = args,
                    UseShellExecute = false
                });
                return;
            }
            catch (Win32Exception) { }
        }
        throw new InvalidOperationException(
            $"No supported terminal emulator found. Tried: {string.Join(", ", LinuxTerminals)}.");
    }

    private static string BuildShellCommand(Project project, IReadOnlyList<string> flags, string prompt)
    {
        var flagStr = string.Join(' ', flags);
        var escapedRepo = EscapeForShellSingleQuote(project.RepositoryPath);
        var escapedPrompt = EscapeForShellSingleQuote(prompt);
        var claudeArgs = string.IsNullOrEmpty(flagStr) ? escapedPrompt : $"{flagStr} {escapedPrompt}";
        return $"cd {escapedRepo} && claude {claudeArgs}";
    }

    private static string EscapeForShellSingleQuote(string input) =>
        $"'{input.Replace("'", "'\\''")}'";

    private static string EscapeForAppleScript(string input) =>
        input.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
