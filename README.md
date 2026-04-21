# Claude Launcher

Skip the setup. Define your Claude Code projects once — repo, files, prompt — and launch them instantly. Works on Windows, macOS, and Linux.

---

## What is Claude Launcher?

Claude Launcher is a cross-platform desktop app built with Avalonia UI that lets you define and save per-project configurations for Claude Code sessions. Instead of manually navigating to your repo, selecting context files, and typing out the same start prompt every time, you configure it once and launch with a single click.

Built for developers who use Claude Code across multiple projects and want to get into flow faster.

---

## Features

- **Project management** — define and save multiple projects, each with their own configuration
- **Repo directory** — set the working directory Claude Code launches into per project
- **Relevant files** — specify context files to include at the start of each session
- **Custom start prompt** — define a project-specific prompt so Claude knows exactly what you're working on from the first message
- **Cross-platform** — runs on Windows, macOS, and Linux

---

## Requirements

- [Claude Code](https://github.com/anthropics/claude-code) installed and authenticated
- .NET 8 or later
- Node.js (required by Claude Code)

---

## Installation

### Download a release

Head to the [Releases](../../releases) page and download the build for your platform.

### Build from source

```bash
git clone https://github.com/yourusername/claude-launcher.git
cd claude-launcher
dotnet build
dotnet run
```

---

## Usage

Claude Launcher is a small desktop front-end for the Claude Code CLI that keeps per-repository configuration in one place so you can jump into a coding session without retyping flags, paths, or context every time.

You define projects (name + repository path), optionally group them under a client with a colour indicator for easy scanning in the sidebar, pick the files Claude should read on startup, and write persistent project notes stored inside the repo under `.claude-launcher/notes/` so they travel with the code.

Optionally point at a session-notes file that Claude reads on launch and appends a dated summary to when you type `checkpoint` in the chat. Flags like `--continue` (auto-skipped when no prior session exists) and `--dangerously-skip-permissions` are toggles in the UI.

Hit **Launch Claude** and the app opens your platform's terminal in the repo with `claude` already running and your assembled prompt primed.

---

## Tech Stack

- **.NET 10** (C#, nullable enabled)
- **Avalonia UI 12.0.1** — cross-platform XAML desktop UI (Windows / macOS / Linux), FluentTheme, Inter font
- **CommunityToolkit.Mvvm 8.4.1** — MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`)
- **System.Text.Json** — JSON persistence to `%APPDATA%/ClaudeLauncher/` (`projects.json`, `client-colors.json`); per-repo notes stored alongside the user's code under `.claude-launcher/notes/`
- **AvaloniaUI.DiagnosticsSupport** — debug-only dev tools
- Launches the external Claude Code CLI (`claude`) via `Process.Start` on Windows, `osascript`-driven Terminal.app on macOS, or the first available terminal emulator on Linux (`gnome-terminal`, `konsole`, `xfce4-terminal`, `xterm`, …)

---

## Contributing

Contributions are welcome. Open an issue first if you're planning something significant so we can discuss the approach before you build it.

---

## License

MIT
