# Quanta - Windows Quick Launcher

<p align="center">
  <a href="README.md">中文</a> | <b>English</b>
</p>

<p align="center">
  <img src="quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  A lightweight Windows launcher — summon with a hotkey, fuzzy-search, execute instantly.
</p>

---

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Usage](#usage)
- [Configuration](#configuration)
- [Project Design](#project-design)
- [Development Guide](#development-guide)

---

## Features

### Core
- **Global hotkey** — default `Alt+Space`, fully customizable
- **Fuzzy search** — matches keyword, name, and description; ranked by score + usage frequency
- **Custom commands** — supports Url, Program, Shell, Directory, Calculator types
- **Parameter mode** — press `Tab` to pass arguments to commands (e.g., `g > hello world`)
- **Ctrl+Number** — `Ctrl+1~9` to instantly execute by position
- **Single-click execution** — click any result to run it
- **Auto-hide on blur** — clicking another window hides Quanta immediately

### Built-in System Commands

No configuration needed — just type the keyword:

| Keyword | Description | Keyword | Description |
|---------|-------------|---------|-------------|
| `cmd` | Command Prompt | `powershell` | PowerShell |
| `notepad` | Notepad | `calc` | Calculator |
| `explorer` | File Explorer | `taskmgr` | Task Manager |
| `control` | Control Panel | `regedit` | Registry Editor |
| `services` | Services | `devmgmt` | Device Manager |
| `ping` | Ping | `ipconfig` | IP Config |
| `tracert` | Traceroute | `nslookup` | DNS Lookup |
| `netstat` | Network Status | `mspaint` | Paint |

### UI & Experience
- **Light / Dark theme** — toggle with the top-right icon; persisted to config
- **Multi-language** — Chinese / English, switches instantly
- **System tray** — runs minimized; right-click for menu
- **Auto-start** — configure `StartWithWindows` to write a registry Run key
- **Command icons** — auto emoji by type, custom icon via `IconPath`
- **Toast notifications** — feedback for save, import, errors, etc.
- **Smooth animations** — fade + scale on show/hide

### Command Management
- **Settings window** — add, edit, delete, search/filter commands
- **Hotkey recorder** — press the new combo directly in the settings UI
- **Import / Export** — JSON format, portable and shareable
- **MaxResults** — configurable result limit

---

## Quick Start

### Requirements

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build & Run

```bash
git clone <repo-url>
cd Quanta
dotnet build
dotnet run
```

### Publish (single file)

```bash
dotnet publish -c Release -r win-x64
```

Output: `bin/Release/net8.0-windows/win-x64/publish/`

---

## Usage

### Basic Flow

1. Launch Quanta — it hides to the system tray
2. Press `Alt+Space` to open the search box
3. Type a keyword (e.g., `g`, `notepad`, `cmd`)
4. Navigate with arrow keys or `Ctrl+Number`
5. Press `Enter` to execute, `Esc` to hide

### Parameter Mode

After matching a command, press `Tab` to pass arguments:

```
Search box: g           → matches "Google Search"
Press Tab → g >         → enter parameter mode
Type      → g > rust    → executes Google Search for "rust"
```

`Backspace` (when param is empty) exits back to normal search.

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+Space` | Show / hide main window (customizable) |
| `Enter` | Execute selected command |
| `Tab` | Enter parameter mode |
| `Esc` | Exit parameter mode / hide window |
| `↑` / `↓` | Navigate results |
| `Ctrl+1~9` | Execute result at that position |
| `Backspace` | In param mode: step back to normal search |

### Command Management

- **Right-click** the search icon or tray icon → open Command Settings
- Edit keyword, name, type, path directly in the table
- Import / Export as JSON for backup and sharing

---

## Configuration

The config file is `config.json` in the application directory. It is loaded at startup and hot-reloaded when the settings window closes.

### Full Schema

```json
{
  "Version": "1.0",
  "Hotkey": {
    "Modifier": "Alt",
    "Key": "Space"
  },
  "Theme": "Light",
  "Commands": [],
  "CommandGroups": [],
  "AppSettings": {
    "StartWithWindows": false,
    "MinimizeToTray": true,
    "CloseToTray": true,
    "ShowInTaskbar": false,
    "MaxResults": 10,
    "Language": "zh-CN",
    "AutoUpdate": true
  },
  "PluginSettings": {
    "Enabled": true,
    "PluginDirectory": "Plugins"
  }
}
```

### Command Fields

| Field | Type | Required | Description |
|-------|------|:--------:|-------------|
| `Keyword` | string | ✓ | Search trigger word |
| `Name` | string | ✓ | Display name |
| `Type` | string | ✓ | `Url` / `Program` / `Shell` / `Directory` / `Calculator` |
| `Path` | string | ✓ | URL or executable path; supports `{param}` |
| `Arguments` | string | | Launch arguments; supports `{param}` |
| `WorkingDirectory` | string | | Working directory |
| `RunAsAdmin` | bool | | Run with elevated privileges |
| `RunHidden` | bool | | Hide the console/window |
| `IconPath` | string | | Custom icon — emoji string (e.g., `"🚀"`) |
| `ParamPlaceholder` | string | | Custom placeholder, default `{param}` |
| `Enabled` | bool | | Enable/disable without deleting |
| `Description` | string | | Matched during fuzzy search |

### Command Type Examples

| Type | Path Example | Result |
|------|-------------|--------|
| `Url` | `https://google.com/search?q={param}` | Opens in default browser |
| `Program` | `notepad.exe` / `C:\Tools\app.exe` | Launches executable |
| `Shell` | `ping {param}` | Runs via `cmd.exe /c` |
| `Directory` | `C:\Users\Public` | Opens in File Explorer |
| `Calculator` | `{param}` | Evaluates math expression |

### Placeholders

`{param}`, `{query}`, and `{%p}` are equivalent — all replaced with the user's input at execution time. You can set a custom placeholder via `ParamPlaceholder`.

### AppSettings

| Field | Default | Description |
|-------|---------|-------------|
| `StartWithWindows` | `false` | Writes to `HKCU\...\Run` registry key |
| `MinimizeToTray` | `true` | Minimize to tray instead of taskbar |
| `CloseToTray` | `true` | Closing hides to tray rather than exiting |
| `MaxResults` | `10` | Max number of search results |
| `Language` | `"zh-CN"` | `zh-CN` or `en-US` |

---

## Project Design

### Architecture Overview

Quanta follows the **MVVM** pattern across four layers:

```
┌─────────────────────────────────────────────┐
│                  View Layer                  │
│   MainWindow  ·  CommandSettingsWindow        │
└────────────────────┬────────────────────────┘
                     │ Data Binding / Commands
┌────────────────────▼────────────────────────┐
│              ViewModel Layer                 │
│             MainViewModel                    │
│  SearchText · Results · SelectedResult        │
│  IsParamMode · CommandKeyword · CommandParam  │
└────────────────────┬────────────────────────┘
                     │ Calls
┌────────────────────▼────────────────────────┐
│               Services Layer                 │
│  SearchEngine  CommandRouter  UsageTracker   │
│  HotkeyManager  TrayService  ToastService    │
│  LocalizationService  ThemeService  Logger   │
└────────────────────┬────────────────────────┘
                     │ Read / Write
┌────────────────────▼────────────────────────┐
│             Models / Data Layer              │
│  AppConfig  CommandConfig  SearchResult      │
│  UsageData  ·  config.json  ·  usage.json   │
└─────────────────────────────────────────────┘
```

### Core Modules

#### SearchEngine

The central search and execution engine:

1. **In-memory command store** — loads user commands + built-in commands at startup; hot-reloads when the settings window closes
2. **Scoring** — five-level priority for each candidate:

   ```
   Exact keyword match          → 1.00
   Keyword prefix match         → 0.95
   Keyword contains query       → 0.90
   Name contains query          → 0.85
   Description contains query   → 0.80
   ```

3. **Ranking** — sorted by `(MatchScore ↓, UsageCount ↓)`, returns top N
4. **Execution dispatch** by type:
   - `Url` → `Process.Start` (default browser)
   - `Program` → `ProcessStartInfo` (admin / hidden mode supported)
   - `Shell` → `cmd.exe /c`
   - `Directory` → `explorer.exe`
   - `Calculator` → `DataTable.Compute`

#### MainViewModel

Powered by `CommunityToolkit.Mvvm`:

- **Debounced search** — 30 ms delay, cancels previous in-flight request via `CancellationTokenSource`
- **Parameter mode** — tri-state: `IsParamMode` / `CommandKeyword` / `CommandParam`
- **Circular navigation** — `SelectNext` / `SelectPrevious` wraps around
- **ClearSearch** — resets all state after successful execution

#### Search & Execution Data Flow

```
User types
  │
  ▼
SearchBox (TwoWay Binding)
  │
  ▼
MainViewModel.OnSearchTextChanged
  │  30 ms debounce
  ▼
SearchEngine.SearchAsync(query)
  ├── SearchCustomCommands(query)   // custom + built-in commands
  └── CommandRouter.TryHandleAsync  // special commands (calc / web search)
  │
  ▼
Sort results + assign 1-based index
  │
  ▼
ObservableCollection<SearchResult> → ListBox renders
```

#### HotkeyManager

Uses Win32 `RegisterHotKey` / `UnregisterHotKey` with a `HwndSource` hook:

```csharp
RegisterHotKey(hwnd, HOTKEY_ID, modifiers, key);
HwndSource.AddHook(WndProc);  // intercept WM_HOTKEY
```

The hotkey can be re-recorded in the settings UI and re-registered immediately.

#### UsageTracker

- Records `commandId → timestamp` on every execution
- Persisted to `LocalApplicationData/Quanta/usage.json`
- Auto-saves every 30 seconds to reduce IO
- Provides a usage count used as the secondary sort key

#### Parameter Mode State Machine

```
Normal search mode
    │ Tab (command matched)
    ▼
Parameter input mode  ──Backspace (input empty)──▶  Normal mode (keyword retained)
    │ Enter
    ▼
Execute command (replace {param})
    │ Success
    ▼
ClearSearch → HideWindow
```

#### Single-Instance Guarantee

```csharp
var mutex = new Mutex(true, "Quanta_SingleInstance_Mutex", out bool createdNew);
if (!createdNew)
{
    SetForegroundWindow(existingProcess.MainWindowHandle);
    Current.Shutdown();
}
```

#### Auto-Hide on Blur

```csharp
private void Window_Deactivated(object sender, EventArgs e)
{
    // Don't hide if a child window (e.g., Settings) is open
    if (OwnedWindows.Count > 0) return;
    HideWindow();
}
```

### File Structure

```
Quanta/
├── App.xaml / App.xaml.cs             # Entry point, single-instance, auto-start
├── Quanta.csproj                       # Project (.NET 8, WPF, x64, single-file publish)
├── config.json                         # User config (app directory)
├── quanta.ico                          # App icon (tray + window)
│
├── Models/
│   ├── AppConfig.cs                    # Root config, hotkey, commands, app settings
│   ├── SearchResult.cs                 # Search result model + type enum
│   └── UsageData.cs                    # Usage frequency data structure
│
├── ViewModels/
│   └── MainViewModel.cs                # Search state, param mode, theme flag
│
├── Views/
│   ├── MainWindow.xaml / .cs           # Search box, keyboard events, animations
│   ├── CommandSettingsWindow.xaml/.cs  # Command CRUD, hotkey recorder
│   └── SettingsWindow.xaml / .cs       # General settings (reserved for expansion)
│
├── Services/
│   ├── SearchEngine.cs                 # Search, scoring, command execution
│   ├── CommandRouter.cs                # Special command routing (calc, web search)
│   ├── CommandService.cs               # JSON import / export
│   ├── HotkeyManager.cs                # Win32 global hotkey registration
│   ├── UsageTracker.cs                 # Frequency tracking and ranking weight
│   ├── LocalizationService.cs          # i18n (zh-CN / en-US)
│   ├── TrayService.cs                  # System tray icon and context menu
│   ├── ToastService.cs                 # In-app toast notifications
│   ├── ThemeService.cs                 # Light / dark theme switching
│   ├── WindowManager.cs                # Window state management
│   ├── PluginManager.cs                # Plugin extensibility (reserved)
│   └── Logger.cs                       # Debug console logging
│
├── Helpers/
│   └── ConfigLoader.cs                 # Config load / save / migrate / cache
│
└── Resources/
    └── Themes/
        ├── DarkTheme.xaml              # Dark theme resource dictionary
        └── LightTheme.xaml             # Light theme resource dictionary
```

### Key Design Decisions

| Decision | Approach | Rationale |
|----------|----------|-----------|
| Command execution | Fire-and-forget (`Task.Run`) | Non-blocking UI; hide only on success |
| Search trigger | 30 ms debounce + `CancellationToken` | Avoid redundant searches; eventual consistency |
| Built-in commands | Hard-coded in `SearchEngine` | Cannot be deleted; don't pollute user config |
| Config reads | In-memory cache + explicit `Reload` | Minimize IO; force refresh after settings close |
| Icons | Emoji characters | No image assets needed; consistent cross-font |
| Auto-hide | `Deactivated` + `OwnedWindows` check | Avoid false hide when child window opens |
| Single instance | Named `Mutex` | System-wide, cross-process guarantee |

---

## Development Guide

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `CommunityToolkit.Mvvm` | 8.2.2 | MVVM framework (ObservableObject, RelayCommand) |
| `System.Drawing.Common` | 8.0.0 | Tray icon image handling |

### Adding a New Command Type

1. Add the type name to `TypeColumn.ItemsSource` in `CommandSettingsWindow.xaml`
2. Add a new `case` in `SearchEngine.ExecuteCustomCommandAsync`
3. Add the corresponding emoji in `SearchEngine.GetIconText`

### Adding a New Translation Key

Add entries in **both** language dictionaries in `LocalizationService.cs`:

```csharp
["zh-CN"] = new() { ["MyKey"] = "中文", ... },
["en-US"] = new() { ["MyKey"] = "English", ... },
```

Then use `LocalizationService.Get("MyKey")` in UI code.

### Debugging

```bash
dotnet run
```

All log output goes to the VS debug console via `Logger.Log/Warn/Error`.

---

## Author

**yeal911** · yeal91117@gmail.com

## License

MIT License
