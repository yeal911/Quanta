# Quanta User Manual

## Introduction

Quanta is a Windows quick launcher. Press `Alt+R` to summon the search box, then quickly launch programs or execute commands.

---

## Quick Start

1. Run the program → minimizes to system tray
2. Press `Alt+R` to open search box
3. Type a keyword to search
4. Press `Enter` to execute, `Esc` to hide

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Alt+R` | Show/hide search box |
| `Enter` | Execute selected command |
| `Tab` | Enter parameter mode |
| `Esc` | Hide window |
| `↑` `↓` | Navigate results |
| `Ctrl+1~9` | Quick execute result N |

---

## Built-in Commands

| Keyword | Action |
|---------|--------|
| `cmd` | Command Prompt |
| `powershell` | PowerShell |
| `notepad` | Notepad |
| `calc` | Calculator |
| `explorer` | File Explorer |
| `taskmgr` | Task Manager |
| `control` | Control Panel |
| `regedit` | Registry |
| `lock` | Lock Screen |
| `shutdown` | Shutdown |
| `restart` | Restart |
| `g` | Google Search |
| `b` | Baidu Search |

---

## Special Features

### Calculator
Type math expressions directly:

```
2+2*3     → 8
sqrt(16)  → 4
100%3     → 1
```

### Unit Conversion
```
100 km to miles  → 62.137 miles
100 c to f      → 212 °F
1 kg to lbs     → 2.205 lbs
```

### Currency Conversion
```
100 usd to cny   → RMB amount
```
(Requires API Key in settings)

### Clipboard History
Type `clip` to view clipboard history

### Screen Recording
Type `record` to start recording, supports mic/speaker

### QR Code
Text over 20 characters automatically generates QR code

---

## Parameter Mode

After matching a command with parameters, press `Tab`:

```
Type: g
Press Tab → g >
Type: g > rust → executes search "rust"
```

---

## Command Management

Right-click tray icon → Settings → Add/Edit/Delete commands

### Command Types

| Type | Description |
|------|-------------|
| Url | Open webpage |
| Program | Launch program |
| Shell | Execute command |
| Directory | Open folder |
| Calculator | Calculate expression |

---

## Settings

Right-click tray icon → Settings:
- Hotkey
- Theme (Light/Dark)
- Language (中文/English/Español)
- Auto-start

---

## Tray Menu

Right-click tray icon:
- Settings
- Language
- About
- Exit

---

*Questions? Contact: yeal91117@gmail.com*
