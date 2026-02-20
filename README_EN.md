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

### Special Features
- **Clipboard history** — type `clip` to view clipboard history
- **Screen recording** — type `record` to start recording (mic/speaker)
- **QR code generation** — long text automatically generates QR code

### UI & Experience
- **Light / Dark theme** — toggle with the top-right icon
- **Multi-language** — Chinese / English, switches instantly
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
dotnet publish -c Release -r win-x64
```

---

## Usage

### Basic Flow

1. Launch Quanta — it hides to the system tray
2. Press `Alt+R` to open the search box
3. Type a keyword (e.g., `g`, `notepad`)
4. Navigate with arrow keys or `Ctrl+Number`
5. Press `Enter` to execute, `Esc` to hide

### Parameter Mode

After matching a command, press `Tab` to pass arguments:

```
Type: g          → matches "Google Search"
Press Tab → g >     → enter parameter mode
Type: g > rust   → executes Google Search for "rust"
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

---

## Author

**yeal911** · yeal91117@gmail.com

## License

MIT License
