using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using ClaudeLauncher.Models;
using ClaudeLauncher.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClaudeLauncher.ViewModels;

public partial class ProjectViewModel : ViewModelBase
{
    public Project Model { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _clientName;
    [ObservableProperty] private string _clientColor = ClientColorStorage.DefaultColor;
    [ObservableProperty] private string _repositoryPath;
    [ObservableProperty] private string _customPrompt;
    [ObservableProperty] private bool _dangerouslySkipPermissions;
    [ObservableProperty] private bool _resumeLastSession;
    [ObservableProperty] private string _sessionNotesPath;

    public ObservableCollection<string> RelevantFiles { get; }
    public ObservableCollection<Note> Notes { get; } = new();

    public ProjectViewModel(Project model)
    {
        Model = model;
        _name = model.Name;
        _clientName = model.ClientName;
        _repositoryPath = model.RepositoryPath;
        _customPrompt = model.CustomPrompt;
        _dangerouslySkipPermissions = model.DangerouslySkipPermissions;
        _resumeLastSession = model.ResumeLastSession;
        _sessionNotesPath = model.SessionNotesPath;
        RelevantFiles = new ObservableCollection<string>(model.RelevantFiles);
        RelevantFiles.CollectionChanged += (_, _) => SyncFilesToModel();
        RefreshNotes();
    }

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "(unnamed project)" : Name;

    public IBrush ClientAccentBrush
    {
        get
        {
            try { return new SolidColorBrush(Avalonia.Media.Color.Parse(ClientColor)); }
            catch { return new SolidColorBrush(Avalonia.Media.Color.Parse(ClientColorStorage.DefaultColor)); }
        }
    }

    public void RefreshNotes()
    {
        Notes.Clear();
        foreach (var note in NotesService.List(RepositoryPath))
            Notes.Add(note);
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
        OnPropertyChanged(nameof(DisplayName));
    }

    partial void OnClientNameChanged(string value) => Model.ClientName = value;

    partial void OnClientColorChanged(string value) => OnPropertyChanged(nameof(ClientAccentBrush));

    partial void OnRepositoryPathChanged(string value)
    {
        Model.RepositoryPath = value;
        RefreshNotes();
    }

    partial void OnCustomPromptChanged(string value) => Model.CustomPrompt = value;
    partial void OnDangerouslySkipPermissionsChanged(bool value) => Model.DangerouslySkipPermissions = value;
    partial void OnResumeLastSessionChanged(bool value) => Model.ResumeLastSession = value;
    partial void OnSessionNotesPathChanged(string value) => Model.SessionNotesPath = value;

    private void SyncFilesToModel() => Model.RelevantFiles = RelevantFiles.ToList();
}
