# Quanta User Manual

<p align="center">
  <a href="用户手册.md">中文</a> | <b>English</b> | <a href="Manual de Usuario.md">Español</a>
</p>

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Core Operations](#core-operations)
3. [Built-in Commands](#built-in-commands)
4. [Smart Input](#smart-input)
5. [Recording](#recording)
6. [Clipboard History](#clipboard-history)
7. [Custom Commands](#custom-commands)
8. [Settings Reference](#settings-reference)

---

## Quick Start

### Requirements

- Windows 10 / 11 (x64)
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Getting Started

1. Run `Quanta.exe` — it minimizes to the system tray automatically
2. Press `Alt+R` (default hotkey) to open the search box
3. Type a keyword — results appear in real time
4. Press `Enter` to execute, `Esc` to dismiss the window

> Tip: Right-click the tray icon for quick access to settings, language switching, and exit.

---

## Core Operations

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+R` | Show / hide main window (customizable in settings) |
| `Enter` | Execute the selected command |
| `Tab` | Enter parameter mode (append parameters to a command) |
| `Esc` | Exit parameter mode / hide window |
| `↑` / `↓` | Move selection up / down |
| `Ctrl+1~9` | Instantly execute the Nth result |
| `Backspace` | Return to normal search from parameter mode |

### Fuzzy Search

No need for exact spelling — Quanta supports prefix fuzzy matching:

- Type `rec` → matches `record`, `regedit`, etc.
- Type `ps` → matches `powershell`
- Type `tk` → matches `taskmgr` (Task Manager)

Search covers command keywords, names, and descriptions. Results are scored and grouped by category.

### Parameter Mode

For commands that accept a `{param}` placeholder (e.g., Google Search, Ping):

1. Find the target command (e.g., `g`)
2. Press `Tab` to enter parameter mode — the input box shows `g >`
3. Type your parameter (e.g., `rust programming`)
4. Press `Enter` — the parameter is substituted and the action runs

### Ctrl+Number Quick Execute

Each result shows an index number 1–9. Press `Ctrl+1` to run the first result directly, without moving the cursor.

---

## Built-in Commands

All built-in commands support fuzzy matching.

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
| `emptybin` | Empty Recycle Bin (immediate, no confirmation) |

### Network Diagnostics

| Keyword | Description |
|---------|-------------|
| `ipconfig` | View IP configuration (runs in PowerShell) |
| `ping` | Ping (supports parameter mode: `ping > 8.8.8.8`) |
| `tracert` | Trace Route |
| `nslookup` | DNS Lookup |
| `netstat` | View network connections |

### Power Management

| Keyword | Description |
|---------|-------------|
| `lock` | Lock screen immediately |
| `shutdown` | Shutdown in 10 seconds (cancel with `shutdown /a` in the opened window) |
| `restart` | Restart in 10 seconds |
| `sleep` | Enter sleep mode |

### Special Features

| Keyword | Description |
|---------|-------------|
| `record` | Launch Quanta recording panel |
| `clip` | Search clipboard history |

### Quanta System Commands

| Keyword | Description |
|---------|-------------|
| `setting` | Open settings |
| `about` | About Quanta |
| `exit` | Quit Quanta |
| `english` | Switch UI language to English immediately |
| `chinese` | Switch UI language to Chinese immediately |
| `spanish` | Switch UI language to Spanish immediately |

---

## Smart Input

Type directly into the search box — no command selection needed. Quanta auto-detects the input type and shows results in real time.

### Math Calculator

Type any math expression:

| Input | Result |
|-------|--------|
| `2+2*3` | `8` |
| `(100-32)/1.8` | `37.778` |
| `2^10` | `1024` |
| `15%4` | `3` |
| `3.14*5*5` | `78.5` |

Supported operators: `+`, `-`, `*`, `/`, `%` (modulo), `^` (power), with parentheses and decimals.

Press `Enter` to copy the result to the clipboard automatically.

### Unit Conversion

Format: `value source_unit to target_unit`

**Length**

| Input | Result |
|-------|--------|
| `100 km to miles` | `62.137 miles` |
| `5 ft to cm` | `152.4 cm` |
| `1 inch to mm` | `25.4 mm` |

**Weight**

| Input | Result |
|-------|--------|
| `1 kg to lbs` | `2.205 lbs` |
| `500 g to oz` | `17.637 oz` |

**Temperature**

| Input | Result |
|-------|--------|
| `100 c to f` | `212 °F` |
| `30 f to c` | `-1.111 °C` |

**Area / Volume**

| Input | Result |
|-------|--------|
| `1 acre to m2` | `4046.856 m²` |
| `1 gallon to l` | `3.785 l` |

### Currency Conversion

Format: `value source_currency to target_currency`

> Requires an API Key from [exchangerate-api.com](https://www.exchangerate-api.com). Set it in Settings → Exchange Rate.

| Input | Example Output |
|-------|----------------|
| `100 usd to cny` | `¥736.50 CNY` |
| `1000 cny to jpy` | `¥20,500 JPY` |
| `50 eur to gbp` | `£42.30 GBP` |

Supports 40+ currencies. Rates are cached locally for a configurable duration (default: 1 hour). Expired cache triggers an automatic API refresh.

### Color Conversion

Supports HEX, RGB, and HSL — auto-converts between all three formats with a live color preview swatch:

| Input | Output |
|-------|--------|
| `#E67E22` | `rgb(230,126,34)  hsl(28,79%,52%)` |
| `rgb(230,126,34)` | `#E67E22  hsl(28,79%,52%)` |
| `hsl(28,79%,52%)` | `#E67E22  rgb(230,126,34)` |
| `255,165,0` | `#FFA500  hsl(38,100%,50%)` |

Press `Enter` to copy the HEX value to the clipboard.

### Text Tools

| Prefix | Function | Example Input | Result |
|--------|----------|---------------|--------|
| `base64 text` | Base64 encode | `base64 hello` | `aGVsbG8=` |
| `base64d text` | Base64 decode | `base64d aGVsbG8=` | `hello` |
| `md5 text` | MD5 hash | `md5 hello` | `5d41402abc4b2a76b9719d911017c592` |
| `sha256 text` | SHA-256 hash | `sha256 hello` | `2cf24dba5fb0a30e...` |
| `url text` | URL encode (auto-decodes if already encoded) | `url hello world` | `hello%20world` |
| `json JSON` | Pretty-print JSON | `json {"a":1,"b":2}` | Formatted multi-line JSON |

Press `Enter` to copy the result to the clipboard.

### Google Search

Format: `g keyword`

```
g rust programming  →  Opens Google search in your default browser
```

You can also type `g`, press `Tab` to enter parameter mode, then type the search term.

### PowerShell Execution

Prefix `>` to run any PowerShell command directly:

```
> Get-Process       →  List all running processes
> dir C:\           →  List C: drive contents
> ping 8.8.8.8      →  Run ping
```

### QR Code Generation

Type text longer than the threshold (default: 20 characters, adjustable in settings) to auto-generate a QR code image and copy it to the clipboard. A toast notification confirms the copy.

```
https://github.com/yeal911/Quanta   →  QR code auto-generated and copied
```

---

## Recording

Type `record` to open the recording panel (displayed as a floating bar at the top of the screen).

### Recording Parameters

| Parameter | Options | Recommended |
|-----------|---------|-------------|
| Source | Microphone / Speaker (system audio) / Both | Depends on use case |
| Format | m4a (AAC) / mp3 | m4a (better quality) |
| Bitrate | 32 / 64 / 96 / 128 / 160 kbps | 32 kbps for meetings, 128 for music |
| Channels | Mono / Stereo | Mono for meetings |

> Meeting recording tip: m4a + 32 kbps + Mono → ~14 MB per hour.

### Recording Controls

1. Click **Start** to begin recording — the panel shows elapsed time
2. Click **Pause** to pause, click **Resume** to continue
3. Click **Stop** to end — the file saves automatically to the configured output directory
4. **Right-click any chip** (Source, Format, Bitrate, Channels) to change that parameter at any time, even during recording

### Output Files

The save directory is configured in Settings → Recording → Output Path. File names follow the pattern `Recording_YYYYMMDD_HHmmss.m4a` (or `.mp3`).

---

## Clipboard History

Quanta silently monitors and records all text you copy to the clipboard.

### How to Use

- Type `clip` → shows the 20 most recent clipboard entries
- Type `clip keyword` → filters history by keyword
- Click any result → copies that text back to the clipboard

### Notes

- Stores up to 50 entries; oldest entries are removed when the limit is reached
- History persists across restarts (saved locally)
- Only plain text is recorded — images and files are not captured

---

## Custom Commands

### Opening Command Management

Type `setting` or right-click the tray icon → Settings → **Commands** tab.

### Command Types

| Type | Description | Path Example |
|------|-------------|--------------|
| `Url` | Open a URL in the browser, supports `{param}` placeholder | `https://google.com/search?q={param}` |
| `Program` | Launch an executable | `C:\Windows\notepad.exe` or `notepad.exe` |
| `Shell` | Run a command in cmd, supports `{param}` | `ping {param}` |
| `Directory` | Open a folder in File Explorer | `C:\Users\Public\Downloads` |
| `Calculator` | Evaluate an expression (advanced use) | `{param}` |

### Adding Command Examples

**Add an "Open Project Folder" command:**
1. Click Add
2. Keyword: `proj`, Name: `My Project`, Type: `Directory`, Path: `D:\Projects`
3. Click Save
4. Type `proj` in the search box to open the folder instantly

**Add a "Bing Search" command:**
1. Keyword: `bing`, Name: `Bing Search`, Type: `Url`, Path: `https://www.bing.com/search?q={param}`
2. After saving, search `bing`, press `Tab`, type your query, press `Enter`

### Import / Export

In the Commands tab, export all custom commands as a JSON file for backup or migration across machines.

---

## Settings Reference

Open via: search box → `setting`, or right-click tray icon → Settings.

### General

| Setting | Description |
|---------|-------------|
| Hotkey | Global hotkey (default `Alt+R`) — double-click the input field, then press your desired key combination |
| Theme | Light / Dark |
| Language | Chinese / English / Español — takes effect immediately |
| Start with Windows | Auto-run Quanta on system startup |
| Max results | Maximum number of search results to display |
| QR code threshold | Character count that triggers QR code generation (default: 20) |

### Exchange Rate

| Setting | Description |
|---------|-------------|
| Exchange Rate API Key | Free API Key from [exchangerate-api.com](https://www.exchangerate-api.com) |
| Cache Duration | How long to use cached rates before fetching fresh data (in hours, default: 1) |

### Recording

| Setting | Description |
|---------|-------------|
| Record source | Microphone / Speaker / Both |
| Record format | m4a (AAC, recommended) / mp3 |
| Record bitrate | 32 / 64 / 96 / 128 / 160 kbps |
| Record channels | Mono / Stereo |
| Record output path | Directory for saved recordings — click Browse to choose |

---

## FAQ

**Q: Alt+R doesn't open the window — what do I do?**
A: Another application may be using Alt+R. Open Settings and change the hotkey to another combination (e.g., `Alt+Space`).

**Q: Currency conversion always shows "Please configure API Key"?**
A: Go to Settings → Exchange Rate panel, enter your free API Key from exchangerate-api.com, and save.

**Q: Where are my recordings saved?**
A: By default, in your Documents folder. You can change this in Settings → Recording → Output Path.

**Q: How do I cancel a scheduled shutdown?**
A: After running the `shutdown` command, a PowerShell window opens with a 10-second countdown. Run `shutdown /a` in that window before the countdown ends to cancel.

---

*Author: **yeal911** · yeal91117@gmail.com · MIT License*
