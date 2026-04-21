using System;
using System.Collections.Generic;

namespace ClaudeLauncher.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string RepositoryPath { get; set; } = "";
    public List<string> RelevantFiles { get; set; } = new();
    public string CustomPrompt { get; set; } = "";
    public bool DangerouslySkipPermissions { get; set; } = true;
    public bool ResumeLastSession { get; set; } = true;
    public string SessionNotesPath { get; set; } = "";
}
