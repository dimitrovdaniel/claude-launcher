using System.Collections.ObjectModel;
using Avalonia.Media;
using ClaudeLauncher.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClaudeLauncher.ViewModels;

public partial class ClientGroupViewModel : ViewModelBase
{
    public string ClientName { get; }
    public string Color { get; }
    public ObservableCollection<ProjectViewModel> Projects { get; } = new();

    [ObservableProperty] private bool _isExpanded = true;

    public IBrush AccentBrush { get; }
    public IBrush AccentBackgroundBrush { get; }

    public ClientGroupViewModel(string clientName, string color)
    {
        ClientName = clientName;
        Color = color;

        var parsed = ParseOrDefault(color);
        AccentBrush = new SolidColorBrush(parsed);
        AccentBackgroundBrush = new SolidColorBrush(
            Avalonia.Media.Color.FromArgb(0x2A, parsed.R, parsed.G, parsed.B));
    }

    public bool IsUnassigned => string.IsNullOrWhiteSpace(ClientName);

    public string DisplayName => IsUnassigned ? "Unassigned" : ClientName;

    public string Icon => IsUnassigned ? "◇" : "◆";

    private static Color ParseOrDefault(string hex)
    {
        try { return Avalonia.Media.Color.Parse(hex); }
        catch { return Avalonia.Media.Color.Parse(ClientColorStorage.DefaultColor); }
    }
}
