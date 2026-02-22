# Quanta - Windows Quick Launcher

<p align="center">
  <a href="README.md">中文</a> | <b>English</b> | <a href="README_ES.md">Español</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  A lightweight Windows quick launcher. Summon it with a global hotkey, fuzzy search, instant execution.
</p>

---

## Features

### Core

- **Global Hotkey** - Default `Alt+R`, customizable in settings
- **Fuzzy Search** - Smart matching on keyword / name / description, supports prefix fuzzy match (type `rec` to match `record`)
- **Grouped Results** - Results organized by category, sorted by match score within each group
- **Custom Commands** - Supports Url, Program, Shell, Directory, Calculator types
- **Parameter Mode** - Press `Tab` to enter parameter input (e.g. `g > rust` to Google search)
- **Ctrl+Number** - `Ctrl+1~9` to instantly run the Nth result
- **Auto-hide on Blur** - Hides automatically when you click elsewhere

---

## Built-in Commands

All built-in commands support fuzzy search matching.

### Command Line

| Keyword | Description |
|---------|-------------|
| `cmd` | Open Command Prompt |
| `powershell` | Open PowerShell |

### Applications

| Keyword | Description |
|---------|-------------|
| `notepad` | Notepad |
| `calc` | Windows Calculator |
| `mspaint` | Paint |
| `explorer` | File Explorer |
| `winrecord` | Windows built-in Voice Recorder |

### System Management

| Keyword | Description |
|---------|-------------|
| `taskmgr` | Task Manager |
| `devmgmt` | Device Manager |
| `services` | Services |
| `regedit` | Registry Editor |
| `control` | Control Panel |
| `emptybin` | Empty Recycle Bin |

### Network Diagnostics

| Keyword | Description |
|---------|-------------|
| `ipconfig` | View IP configuration |
| `ping` | Ping (runs in PowerShell) |
| `tracert` | Trace Route |
| `nslookup` | DNS Lookup |
| `netstat` | View network connections |

### Power Management

| Keyword | Description |
|---------|-------------|
| `lock` | Lock screen |
| `shutdown` | Shutdown in 10 seconds |
| `restart` | Restart in 10 seconds |
| `sleep` | Enter sleep mode |

### Special Features

| Keyword | Description |
|---------|-------------|
| `record` | Launch Quanta recording |
| `clip` | Search clipboard history |

### Quanta System Commands

| Keyword | Description |
|---------|-------------|
| `setting` | Open settings |
| `about` | About |
| `exit` | Quit Quanta |
| `english` | Switch UI language to English |
| `chinese` | Switch UI language to Chinese |
| `spanish` | Switch UI language to Spanish |

---

## Smart Input

Type directly into the search box — no command selection needed. Results appear in real time.

### Math Calculator

Type any math expression:

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
```

Supported operators: `+ - * / % ^`, parentheses and decimals supported.

### Unit Conversion

Format: `value source_unit to target_unit`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

Supported categories: length, weight, temperature, area, volume

### Currency Conversion

Format: `value source_currency to target_currency` (API Key required in settings)

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

Supports 40+ currencies. Rates cached locally (configurable duration, default 1 hour).

### Color Conversion

Supports HEX, RGB, HSL — auto-converts between all formats with a live color preview:

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79%,52%)    → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### Text Tools

| Prefix | Function | Example |
|--------|----------|---------|
| `base64 text` | Base64 encode | `base64 hello` → `aGVsbG8=` |
| `base64d text` | Base64 decode | `base64d aGVsbG8=` → `hello` |
| `md5 text` | MD5 hash | `md5 hello` → `5d41402a...` |
| `sha256 text` | SHA-256 hash | `sha256 hello` → `2cf24dba...` |
| `url text` | URL encode / auto-decode | `url hello world` → `hello%20world` |
| `json JSON` | Pretty-print JSON | `json {"a":1}` → formatted output |

### Google Search

Format: `g keyword`

```
g rust programming  → Opens Google search in browser
```

### PowerShell Execution

Prefix `>` to run a PowerShell command directly:

```
> Get-Process      → List all processes
> dir C:\          → List C: drive
```

### QR Code Generation

Type text longer than the threshold (default 20 characters) to auto-generate a QR code copied to clipboard:

```
https://github.com/yeal911/Quanta   → QR code auto-generated
```

---

## Recording

Type `record` to open the recording panel:

- **Source**: Microphone / Speaker (system audio) / Microphone + Speaker
- **Format**: m4a (AAC) / mp3
- **Bitrate**: 32 / 64 / 96 / 128 / 160 kbps
- **Channels**: Mono / Stereo
- **Controls**: Start → Pause / Resume → Stop; output saved to configured directory
- **Right-click chips** to switch any parameter at any time

---

## Clipboard History

Type `clip` or `clip keyword` to search clipboard history:

- Automatically records copied text, stores up to 50 entries
- Keyword filtering, shows the 20 most recent matches
- Click any result to copy it back to the clipboard
- History is persisted across restarts

---

## UI & Experience

- **Light / Dark Theme** - Switch in settings, saved automatically
- **Multilingual** - Chinese / English / Español, takes effect immediately
- **System Tray** - Stays in the tray, right-click for quick actions
- **Start with Windows** - Enable in settings
- **Toast Notifications** - Instant feedback on success / failure

---

## Quick Start

### Requirements

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run

```bash
dotnet run
```

### Publish Single File

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+R` | Show / hide main window |
| `Enter` | Execute selected command |
| `Tab` | Enter parameter mode |
| `Esc` | Exit parameter mode / hide window |
| `↑` / `↓` | Move selection up / down |
| `Ctrl+1~9` | Instantly run the Nth result |
| `Backspace` | Return to normal search from parameter mode |

---

## Command Management

Open settings (`setting` or right-click tray icon) → **Commands** tab:

- Edit keyword, name, type, path directly in the grid
- Import / export as JSON for backup and migration

### Command Types

| Type | Description | Example |
|------|-------------|---------|
| `Url` | Open URL in browser (supports `{param}`) | `https://google.com/search?q={param}` |
| `Program` | Launch a program | `notepad.exe` |
| `Shell` | Run command in cmd | `ping {param}` |
| `Directory` | Open folder in Explorer | `C:\Users\Public` |
| `Calculator` | Evaluate an expression | `{param}` |

---

## Settings Reference

| Setting | Description |
|---------|-------------|
| Hotkey | Global hotkey (default `Alt+R`) |
| Theme | Light / Dark |
| Language | Chinese / English / Español |
| Start with Windows | Auto-run on startup |
| Max results | Limit number of search results |
| QR code threshold | Character count to trigger QR code generation |
| Exchange Rate API Key | API Key from exchangerate-api.com |
| Exchange Rate Cache | Cache duration in hours (default 1) |
| Record source | Microphone / Speaker / Both |
| Record format | m4a / mp3 |
| Record bitrate | 32 / 64 / 96 / 128 / 160 kbps |
| Record channels | Mono / Stereo |
| Record output path | Directory for saved recordings |

---

## Technical Architecture

### Project Structure

```
Quanta/
├── App.xaml / App.xaml.cs          # Entry point, tray, theme, lifecycle
├── Views/                           # UI layer
│   ├── MainWindow.xaml              # Main search window
│   ├── RecordingOverlayWindow.xaml  # Recording overlay
│   └── CommandSettingsWindow.xaml   # Settings (5 partial files)
├── Domain/
│   ├── Search/                      # Search engine, providers, result types
│   └── Commands/                    # Command router and handlers
├── Core/
│   ├── Config/                      # AppConfig, ConfigLoader
│   └── Services/                    # RecordingService, ToastService
├── Infrastructure/
│   ├── System/                      # Clipboard history, window management
│   └── Logging/
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml, DarkTheme.xaml
```

### Tech Stack

| Item | Technology |
|------|-----------|
| Framework | .NET 8.0 / WPF |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Language | C# |
| Audio | NAudio (WASAPI) |
| QR Code | QRCoder |
| Exchange Rate | exchangerate-api.com |

---

## Author

**yeal911** · yeal91117@gmail.com

## License

MIT License
