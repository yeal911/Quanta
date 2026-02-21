# Quanta - Windows Quick Launcher

<p align="center">
  <a href="README.md">中文</a> | <b>English</b> | <a href="README_ES.md">Español</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  A lightweight Windows launcher — summon with a hotkey, fuzzy-search, execute instantly.
</p>

---

## Features

### Core

- **Global hotkey** — default `Alt+R`, fully customizable
- **Fuzzy search** — matches keyword, name, and description; ranked by score + usage
- **Custom commands** — supports Url, Program, Shell, Directory, Calculator types
- **Parameter mode** — press `Tab` to pass arguments (e.g., `g > rust`)
- **Ctrl+Number** — `Ctrl+1~9` to instantly execute by position
- **Single-click execution** — click any result to run it
- **Auto-hide on blur** — clicking another window hides Quanta

### Built-in Commands

| Keyword | Description | Keyword | Description |
|---------|-------------|---------|-------------|
| `cmd` | Command Prompt | `powershell` | PowerShell |
| `notepad` | Notepad | `calc` | Calculator |
| `explorer` | File Explorer | `taskmgr` | Task Manager |
| `control` | Control Panel | `regedit` | Registry |
| `services` | Services | `devmgmt` | Device Manager |
| `ping` | Ping | `ipconfig` | IP Config |
| `tracert` | Traceroute | `nslookup` | DNS Lookup |
| `netstat` | Network Status | `mspaint` | Paint |
| `lock` | Lock Screen | `shutdown` | Shutdown |
| `restart` | Restart | `sleep` | Sleep |
| `emptybin` | Empty Recycle Bin | `winrecord` | Voice Recorder |
| `b` | Baidu Search | `g` | Google Search |
| `gh` | GitHub Search | | |

### Special Features

- **Calculator** — type math expressions directly (e.g., `2+2`, `sqrt(16)*3`)
- **Unit conversion** — supports length, weight, temperature, area, volume (e.g., `100 km to miles`, `100 c to f`, `1 kg to lbs`)
- **Currency conversion** — query exchange rates (e.g., `100 usd to cny`), requires API Key
- **Clipboard history** — type `clip` to view clipboard history with search filtering
- **Screen recording** — type `record` to start recording (mic/speaker), supports m4a/mp3
- **QR code generation** — text over 20 chars automatically generates QR, copied to clipboard

### UI & Experience

- **Light / Dark theme** — toggle with the top-right icon, settings auto-saved
- **Multi-language** — Chinese / English / Español, switches instantly
- **System tray** — runs minimized; right-click for menu
- **Auto-start** — configure to run on Windows startup
- **Toast notifications** — success/failure feedback

---

## Quick Start

### Requirements

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run

```bash
dotnet run
```

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Usage

### Basic Flow

1. Launch Quanta — it hides to the system tray
2. Press `Alt+R` to open the search box
3. Type a keyword (e.g., `g`, `notepad`, `clip`)
4. Navigate with arrow keys or `Ctrl+Number`
5. Press `Enter` to execute, `Esc` to hide

### Parameter Mode

After matching a command, press `Tab` to pass arguments:

```
Type: g          → matches "Google Search"
Press Tab → g >     → enter parameter mode
Type: g > rust   → executes Google Search for "rust"
```

### Calculator & Conversions

```
Type: 2+2*3      → Result: 8
Type: sqrt(16)   → Result: 4
Type: 100 km to miles → Unit conversion
Type: 100 c to f     → Temperature: 212 °F
Type: 1 kg to lbs    → Weight: 2.205 lbs
Type: 100 usd to cny → Currency conversion
```

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+R` | Show / hide main window |
| `Enter` | Execute selected command |
| `Tab` | Enter parameter mode |
| `Esc` | Exit parameter mode / hide window |
| `↑` / `↓` | Navigate results |
| `Ctrl+1~9` | Execute result at that position |
| `Backspace` | In param mode: step back to normal search |

### Command Management

- **Right-click tray icon** → open Command Settings
- Edit keyword, name, type, path directly in the table
- Import / Export as JSON for backup

### Settings

| Setting | Description |
|---------|-------------|
| Hotkey | Custom global hotkey |
| Theme | Light / Dark |
| Language | 中文 / English / Español |
| Auto-start | Run on Windows startup |
| Max Results | Search result limit |
| QR Threshold | Characters to trigger QR |

---

## Configuration

The config file is `config.json` in the application directory.

### Command Fields

| Field | Description |
|-------|-------------|
| `Keyword` | Search trigger word |
| `Name` | Display name |
| `Type` | Url / Program / Shell / Directory / Calculator |
| `Path` | URL or executable path; supports `{param}` |
| `Arguments` | Launch arguments |
| `RunAsAdmin` | Run with elevated privileges |
| `RunHidden` | Hide the console/window |

### Command Types

| Type | Example | Result |
|------|---------|--------|
| `Url` | `https://google.com/search?q={param}` | Opens in browser |
| `Program` | `notepad.exe` | Launches executable |
| `Shell` | `ping {param}` | Runs via cmd |
| `Directory` | `C:\Users\Public` | Opens in Explorer |
| `Calculator` | `{param}` | Evaluates expression |

### Config Example

```json
{
  "Hotkey": {
    "Modifier": "Alt",
    "Key": "R"
  },
  "Theme": "Light",
  "Language": "en-US",
  "Commands": [
    {
      "Keyword": "g",
      "Name": "Google Search",
      "Type": "Url",
      "Path": "https://www.google.com/search?q={param}"
    },
    {
      "Keyword": "code",
      "Name": "VS Code",
      "Type": "Program",
      "Path": "C:\\Users\\YourName\\AppData\\Local\\Programs\\Microsoft VS Code\\Code.exe"
    }
  ]
}
```

---

## Architecture

### Project Structure

```
Quanta/
├── App.xaml / App.xaml.cs     # Entry point
├── Quanta.csproj              # Project config
├── config.json                # User config
├── Views/                     # UI layer
│   ├── MainWindow.xaml        # Main search window
│   ├── SettingsWindow.xaml    # Settings window
│   └── CommandSettingsWindow.xaml  # Command management
├── ViewModels/                # View models
├── Models/                    # Data models
├── Services/                  # Service layer
├── Helpers/                   # Utilities
├── Infrastructure/            # Infrastructure
│   ├── Storage/              # Config storage
│   ├── Logging/              # Logging service
│   └── System/               # System integration
└── Resources/                 # Resources
    └── Themes/                # Theme files
```

### Tech Stack

| Item | Technology |
|------|------------|
| Framework | .NET 8.0 / WPF |
| Architecture | MVVM |
| Language | C# |
| UI | WPF |

### Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM Framework |
| System.Drawing.Common | 8.0.0 | Graphics Processing |

---

## Author

**yeal911** · yeal91117@gmail.com

## License

MIT License
