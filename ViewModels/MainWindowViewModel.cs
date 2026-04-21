using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ClaudeLauncher.Models;
using ClaudeLauncher.Services;
using ClaudeLauncher.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClaudeLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<ProjectViewModel> Projects { get; } = new();
    public ObservableCollection<ClientGroupViewModel> ClientGroups { get; } = new();

    [ObservableProperty] private ProjectViewModel? _selectedProject;
    [ObservableProperty] private object? _selectedTreeItem;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string _newFileInput = "";

    [ObservableProperty] private bool _isEditingNote;
    [ObservableProperty] private string _editingNoteName = "";
    [ObservableProperty] private string _editingNoteContent = "";
    private string? _editingNoteOriginalName;

    private readonly Dictionary<string, string> _clientColors;

    public MainWindowViewModel()
    {
        _clientColors = ClientColorStorage.Load();
        foreach (var p in ProjectStorage.Load())
            AttachProject(new ProjectViewModel(p));
        RebuildGroups();
        SelectedProject = Projects.FirstOrDefault();
    }

    private string GetClientColor(string clientName)
    {
        var key = (clientName ?? "").Trim();
        return _clientColors.TryGetValue(key, out var c) && !string.IsNullOrWhiteSpace(c)
            ? c
            : ClientColorStorage.DefaultColor;
    }

    partial void OnSelectedProjectChanged(ProjectViewModel? value)
    {
        CancelNote();
        if (value is null) return;
        var group = ClientGroups.FirstOrDefault(g => g.Projects.Contains(value));
        if (group is not null) group.IsExpanded = true;
        if (!ReferenceEquals(SelectedTreeItem, value))
            SelectedTreeItem = value;
    }

    partial void OnSelectedTreeItemChanged(object? value)
    {
        if (value is ProjectViewModel pvm) SelectedProject = pvm;
    }

    private void AttachProject(ProjectViewModel vm)
    {
        vm.PropertyChanged += OnProjectPropertyChanged;
        Projects.Add(vm);
    }

    private void DetachProject(ProjectViewModel vm)
    {
        vm.PropertyChanged -= OnProjectPropertyChanged;
        Projects.Remove(vm);
    }

    private void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectViewModel.ClientName))
            RebuildGroups();
    }

    private void RebuildGroups()
    {
        var expansion = ClientGroups.ToDictionary(
            g => g.ClientName,
            g => g.IsExpanded,
            StringComparer.OrdinalIgnoreCase);

        ClientGroups.Clear();

        var ordered = Projects
            .GroupBy(p => (p.ClientName ?? "").Trim())
            .OrderBy(g => string.IsNullOrEmpty(g.Key) ? 1 : 0)
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var grp in ordered)
        {
            var color = GetClientColor(grp.Key);
            var cg = new ClientGroupViewModel(grp.Key, color);
            if (expansion.TryGetValue(grp.Key, out var wasExpanded))
                cg.IsExpanded = wasExpanded;
            foreach (var p in grp.OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                p.ClientColor = color;
                cg.Projects.Add(p);
            }
            ClientGroups.Add(cg);
        }

        if (SelectedProject is not null)
        {
            var group = ClientGroups.FirstOrDefault(g => g.Projects.Contains(SelectedProject));
            if (group is not null)
            {
                group.IsExpanded = true;
                SelectedTreeItem = SelectedProject;
            }
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        var vm = new ProjectViewModel(new Project { Name = "New Project" });
        AttachProject(vm);
        RebuildGroups();
        SelectedProject = vm;
        PersistAll();
    }

    [RelayCommand]
    private async Task DeleteProjectAsync()
    {
        if (SelectedProject is null) return;
        var owner = GetTopLevel() as Window;
        if (owner is null) return;

        var confirmed = await ConfirmDialog.ShowAsync(
            owner,
            "Delete project?",
            $"Are you sure you want to delete the project '{SelectedProject.DisplayName}'? " +
            "This only removes it from the launcher — files on disk are not affected.");
        if (!confirmed) return;

        DetachProject(SelectedProject);
        SelectedProject = Projects.FirstOrDefault();
        RebuildGroups();
        PersistAll();
    }

    [RelayCommand]
    private void Save()
    {
        PersistAll();
        StatusMessage = $"Saved {Projects.Count} project(s).";
    }

    [RelayCommand]
    private void SetClientColor(string? color)
    {
        if (SelectedProject is null) return;
        var key = (SelectedProject.ClientName ?? "").Trim();
        _clientColors[key] = string.IsNullOrWhiteSpace(color) ? ClientColorStorage.DefaultColor : color;
        ClientColorStorage.Save(_clientColors);
        RebuildGroups();
    }

    [RelayCommand]
    private async Task BrowseSessionNotes()
    {
        if (SelectedProject is null) return;
        var top = GetTopLevel();
        if (top is null) return;

        var result = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select existing session notes file",
            AllowMultiple = false
        });

        if (result.Count > 0 && result[0].TryGetLocalPath() is { } path)
            SelectedProject.SessionNotesPath = ToRelative(SelectedProject.RepositoryPath, path);
    }

    [RelayCommand]
    private async Task BrowseRepo()
    {
        if (SelectedProject is null) return;
        var top = GetTopLevel();
        if (top is null) return;

        var result = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select repository folder",
            AllowMultiple = false
        });

        if (result.Count > 0 && result[0].TryGetLocalPath() is { } path)
            SelectedProject.RepositoryPath = path;
    }

    [RelayCommand]
    private async Task AddFileFromPicker()
    {
        if (SelectedProject is null) return;
        var top = GetTopLevel();
        if (top is null) return;

        var result = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select relevant file",
            AllowMultiple = true
        });

        foreach (var file in result)
        {
            if (file.TryGetLocalPath() is not { } path) continue;
            var relative = ToRelative(SelectedProject.RepositoryPath, path);
            if (!SelectedProject.RelevantFiles.Contains(relative))
                SelectedProject.RelevantFiles.Add(relative);
        }
        PersistAll();
    }

    [RelayCommand]
    private void AddFileFromInput()
    {
        if (SelectedProject is null || string.IsNullOrWhiteSpace(NewFileInput)) return;
        var value = NewFileInput.Trim();
        if (!SelectedProject.RelevantFiles.Contains(value))
            SelectedProject.RelevantFiles.Add(value);
        NewFileInput = "";
        PersistAll();
    }

    [RelayCommand]
    private async Task RemoveFileAsync(string? file)
    {
        if (SelectedProject is null || file is null) return;
        var owner = GetTopLevel() as Window;
        if (owner is null) return;

        var confirmed = await ConfirmDialog.ShowAsync(
            owner,
            "Remove file?",
            $"Remove '{file}' from this project's relevant files list? " +
            "The file on disk is not affected.",
            "Remove");
        if (!confirmed) return;

        SelectedProject.RelevantFiles.Remove(file);
        PersistAll();
    }

    [RelayCommand]
    private void StartAddNote()
    {
        if (SelectedProject is null) return;
        if (string.IsNullOrWhiteSpace(SelectedProject.RepositoryPath))
        {
            StatusMessage = "Set the repository path before adding notes.";
            return;
        }
        _editingNoteOriginalName = null;
        EditingNoteName = "";
        EditingNoteContent = "";
        IsEditingNote = true;
    }

    [RelayCommand]
    private void StartEditNote(Note? note)
    {
        if (note is null || SelectedProject is null) return;
        _editingNoteOriginalName = note.Name;
        EditingNoteName = note.Name;
        EditingNoteContent = note.Content;
        IsEditingNote = true;
    }

    [RelayCommand]
    private void SaveNote()
    {
        if (SelectedProject is null) return;
        var name = EditingNoteName.Trim();
        if (!NotesService.IsValidName(name))
        {
            StatusMessage = "Note name is empty or contains invalid characters.";
            return;
        }
        if (_editingNoteOriginalName is not null && _editingNoteOriginalName != name)
            NotesService.Delete(SelectedProject.RepositoryPath, _editingNoteOriginalName);
        NotesService.Save(SelectedProject.RepositoryPath, name, EditingNoteContent);
        SelectedProject.RefreshNotes();
        IsEditingNote = false;
        StatusMessage = $"Saved note '{name}'.";
    }

    [RelayCommand]
    private void CancelNote()
    {
        IsEditingNote = false;
        _editingNoteOriginalName = null;
        EditingNoteName = "";
        EditingNoteContent = "";
    }

    [RelayCommand]
    private async Task DeleteNoteAsync(Note? note)
    {
        if (note is null || SelectedProject is null) return;
        var owner = GetTopLevel() as Window;
        if (owner is null) return;

        var confirmed = await ConfirmDialog.ShowAsync(
            owner,
            "Delete note?",
            $"Delete note '{note.Name}'? This permanently removes the file from the repo's " +
            ".claude-launcher/notes/ folder and cannot be undone.");
        if (!confirmed) return;

        NotesService.Delete(SelectedProject.RepositoryPath, note.Name);
        SelectedProject.RefreshNotes();
        StatusMessage = $"Deleted note '{note.Name}'.";
    }

    [RelayCommand]
    private void Launch()
    {
        if (SelectedProject is null) return;
        try
        {
            PersistAll();
            var result = ClaudeLauncherService.Launch(SelectedProject.Model);
            var suffix = result.ResumeSkippedNoPriorSession
                ? " No prior session found — started fresh."
                : "";
            StatusMessage = $"Launched Claude Code for '{SelectedProject.Name}'.{suffix}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Launch failed: {ex.Message}";
        }
    }

    private void PersistAll() =>
        ProjectStorage.Save(Projects.Select(p => p.Model));

    private static string ToRelative(string basePath, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(basePath)) return fullPath;
        try
        {
            var rel = System.IO.Path.GetRelativePath(basePath, fullPath);
            return rel.StartsWith("..") ? fullPath : rel;
        }
        catch
        {
            return fullPath;
        }
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
