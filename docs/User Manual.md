# Quanta User Manual

## Table of Contents

1. [Product Introduction](#1-product-introduction)
2. [Installation and Running](#2-installation-and-running)
3. [Quick Start](#3-quick-start)
4. [Basic Operations](#4-basic-operations)
5. [Command Management](#5-command-management)
6. [Keyboard Shortcuts](#6-keyboard-shortcuts)
7. [Features](#7-features)
8. [System Tray](#8-system-tray)
9. [Theme Settings](#9-theme-settings)
10. [Language Switching](#10-language-switching)
11. [Advanced Features](#11-advanced-features)
12. [Troubleshooting](#12-troubleshooting)
13. [Technical Specifications](#13-technical-specifications)

---

## 1. Product Introduction

### 1.1 About Quanta

Quanta is a lightweight Windows quick launcher designed to help users quickly access frequently used programs, files, and system functions. Through a global hotkey to summon the search box, users can instantly locate and launch target programs or execute system commands, significantly improving work efficiency.

### 1.2 Core Features

- **Global Hotkey Activation**: Default Alt+Space to summon search box, customizable
- **Smart Fuzzy Search**: Supports keyword, name, and description matching, sorted by match score and usage frequency
- **Multiple Command Types**: Supports URL, Program, Shell Command, Directory Browser, and Calculator
- **Parameter Mode**: Supports dynamic parameter passing, such as entering parameters after a search command
- **Light/Dark Themes**: Supports light and dark themes with auto-save configuration
- **Multi-language Support**: Supports Chinese and English interfaces
- **System Tray**: Runs minimized in tray, right-click menu provides quick operations

### 1.3 Use Cases

- Quickly launch commonly used applications (browser, editor, terminal, etc.)
- Quickly open system functions (Control Panel, Task Manager, Services, etc.)
- Quickly execute Shell commands (ping, ipconfig, etc.)
- Quickly access commonly used folders
- Quickly perform math calculations and unit conversions

---

## 2. Installation and Running

### 2.1 System Requirements

| Item | Minimum | Recommended |
|------|---------|-------------|
| OS | Windows 10 (x64) | Windows 11 (x64) |
| Runtime | .NET 8.0 Runtime | .NET 8.0 Runtime |
| Disk Space | 50 MB | 100 MB |
| Memory | 50 MB | 100 MB |

### 2.2 Obtaining the Program

You can obtain Quanta through the following methods:

1. **Build from Source**: Compile from source code
   ```bash
   git clone <repo-url>
   cd Quanta
   dotnet build
   dotnet run
   ```

2. **Direct Run**: Use the compiled executable
   - Find `bin/Debug/net8.0-windows/Quanta.exe` or the release version

3. **Release Build**:
   ```bash
   dotnet publish -c Release -r win-x64
   ```
   Output is located at `bin/Release/net8.0-windows/win-x64/publish/`

### 2.3 First Run

1. Ensure .NET 8.0 Runtime is installed
2. Run Quanta.exe
3. The program will automatically:
   - Create system tray icon
   - Register global hotkey (default Alt+Space)
   - Load configuration file (config.json)
   - Initialize built-in command library

### 2.4 Exiting the Program

There are two ways to exit the program:

1. **Via Tray Icon**: Right-click tray icon → Select "Exit"
2. **Via Task Manager**: End Quanta.exe process

---

## 3. Quick Start

### 3.1 Basic Workflow

The basic usage workflow of Quanta is simple:

1. **Summon Search Box**: Press `Alt+Space` hotkey
2. **Enter Keywords**: Enter the command, program, or function to search
3. **Select Result**: Use arrow keys or mouse to select
4. **Execute Command**: Press Enter or click to execute

### 3.2 Summon and Hide

| Action | Method |
|--------|--------|
| Summon search box | Press `Alt+Space` (default) |
| Hide search box | Press `Esc` or click outside window |

### 3.3 Search Examples

Here are some common search examples:

| Input | Matching Result | Description |
|-------|-----------------|-------------|
| `cmd` | Command Prompt | Open CMD terminal |
| `notepad` | Notepad | Open Notepad |
| `calc` | Calculator | Open Calculator |
| `explorer` | File Explorer | Open File Explorer |
| `taskmgr` | Task Manager | Open Task Manager |
| `control` | Control Panel | Open Control Panel |
| `regedit` | Registry Editor | Open Registry Editor |

---

## 4. Basic Operations

### 4.1 Search Function

#### 4.1.1 Search Rules

Quanta uses a fuzzy matching algorithm with the following priority order:

| Priority | Match Type | Weight |
|----------|------------|--------|
| 1 | Exact keyword match | 1.00 |
| 2 | Keyword prefix match | 0.95 |
| 3 | Keyword contains match | 0.90 |
| 4 | Name contains match | 0.85 |
| 5 | Description contains match | 0.80 |

When multiple results match, commands with higher usage frequency are displayed first.

#### 4.1.2 Search Tips

1. **Exact Match**: Enter complete keyword to locate directly
2. **Partial Match**: Enter partial keyword to search related results
3. **Fuzzy Search**: Supports pinyin initials, English abbreviations, etc.

### 4.2 Result Selection

#### 4.2.1 Keyboard Selection

| Key | Function |
|-----|----------|
| ↑ | Select previous result |
| ↓ | Select next result |
| Enter | Execute selected command |
| Tab | Enter parameter mode |

#### 4.2.2 Mouse Selection

- **Single Click**: Select result
- **Double Click**: Execute command

#### 4.2.3 Quick Number Selection

Use `Ctrl+1` through `Ctrl+9` to directly execute commands with corresponding numbers.

### 4.3 Parameter Mode

When searching for commands that support parameters, press Tab to enter parameter input mode:

1. Enter command keyword (e.g., `g` matches Google Search)
2. Press Tab to enter parameter mode (shows `g > `)
3. Enter parameter (e.g., `rust`)
4. Press Enter to execute (will execute `https://www.google.com/search?q=rust`)

Press Backspace to return to normal search mode.

---

## 5. Command Management

### 5.1 Opening Command Settings

There are two ways to open the command settings interface:

1. **Via Tray Icon**: Right-click tray icon → Select "Settings"
2. **Via Search Box**: Right-click search box icon → Select "Settings"

### 5.2 Command Settings Interface

The command settings interface contains the following functional areas:

- **Command Table**: Displays all configured commands
- **Toolbar**: Add, Delete, Import, Export buttons
- **Search Box**: Filter command list
- **Hotkey Recording**: Record new global hotkey

### 5.3 Adding New Commands

1. Click the "Add" button in the toolbar
2. Fill in command information:
   - **Keyword**: Search trigger word
   - **Name**: Display name
   - **Type**: Command type (Url/Program/Shell/Directory/Calculator)
   - **Path**: Execution path or URL
3. Click "Save"

### 5.4 Command Type Description

| Type | Description | Example |
|------|-------------|---------|
| Url | Open webpage | `https://www.google.com/search?q={param}` |
| Program | Launch program | `notepad.exe` |
| Shell | Execute command | `ping {param}` |
| Directory | Open folder | `C:\Users\Public` |
| Calculator | Calculate expression | `{param}` |

### 5.5 Parameter Placeholders

The following placeholders can be used in command paths:

| Placeholder | Description |
|-------------|-------------|
| `{param}` | User-entered parameter |
| `{query}` | User-entered query |
| `{%p}` | User-entered parameter (short form) |

### 5.6 Command Properties

| Property | Description |
|----------|-------------|
| Keyword | Search trigger keyword |
| Name | Command display name |
| Type | Command type |
| Path | Execution path or URL |
| Arguments | Launch arguments |
| WorkingDirectory | Working directory |
| RunAsAdmin | Run as administrator |
| RunHidden | Run with hidden window |
| IconPath | Custom icon (supports emoji) |
| ParamPlaceholder | Custom parameter placeholder |
| Enabled | Enable/disable |
| Description | Command description |

### 5.7 Import and Export

#### 5.7.1 Exporting Commands

1. Click "Export" button in toolbar
2. Choose save location
3. Commands will be exported in JSON format

#### 5.7.2 Importing Commands

1. Click "Import" button in toolbar
2. Select JSON file to import
3. Commands will be added to the list

---

## 6. Keyboard Shortcuts

### 6.1 Global Hotkeys

| Hotkey | Function | Description |
|--------|----------|-------------|
| Alt+Space | Summon/Hide search box | Default hotkey, customizable |

### 6.2 In-Search Box Hotkeys

| Hotkey | Function | Description |
|--------|----------|-------------|
| Enter | Execute selected command | Execute current selected command |
| Esc | Hide window | Close search box |
| Tab | Enter parameter mode | When there are matching commands |
| Backspace | Return to normal search | Use in parameter mode |
| ↑ | Select previous result | Navigate up result list |
| ↓ | Select next result | Navigate down result list |
| Ctrl+1~9 | Quick execute | Directly execute corresponding number |

### 6.3 Customizing Hotkeys

You can modify global hotkeys in the settings interface:

1. Open command settings interface
2. Click "Record Hotkey" button
3. Press the hotkey combination to set
4. Hotkey will be saved automatically

---

## 7. Features

### 7.1 Built-in Commands

Quanta comes with the following commonly used system commands:

| Keyword | Description | Function |
|---------|-------------|----------|
| cmd | Command Prompt | Open CMD terminal |
| powershell | PowerShell | Open PowerShell |
| notepad | Notepad | Open Notepad |
| calc | Calculator | Open Calculator |
| explorer | File Explorer | Open File Explorer |
| taskmgr | Task Manager | Open Task Manager |
| control | Control Panel | Open Control Panel |
| regedit | Registry Editor | Open Registry Editor |
| services | Services | Open Services Manager |
| devmgmt | Device Manager | Open Device Manager |
| ping | Ping Command | Execute Ping test |
| ipconfig | IP Config | Display IP configuration |
| tracert | Trace Route | Execute route trace |
| nslookup | DNS Lookup | Execute DNS query |
| netstat | Network Status | Display network status |
| mspaint | Paint | Open Paint program |

### 7.2 Calculator Function

Enter mathematical expressions in the search box to calculate:

| Input | Result |
|-------|--------|
| `calc 2+2` | 4 |
| `calc 100*5` | 500 |
| `calc sqrt(16)` | 4 |
| `calc 2^10` | 1024 |

### 7.3 Unit Conversion Function

Supports various unit conversions:

| Input | Description |
|-------|-------------|
| `100 km to mile` | Kilometers to Miles |
| `100 cm to inch` | Centimeters to Inches |
| `100 kg to pound` | Kilograms to Pounds |
| `100 celsius to fahrenheit` | Celsius to Fahrenheit |

---

## 8. System Tray

### 8.1 Tray Icon

After Quanta starts, an icon will appear in the system tray area. Icon functionality:

- **Left single click**: No action
- **Left double click**: Show main window
- **Right single click**: Show context menu

### 8.2 Tray Menu

Right-click the tray icon to show the following menu:

| Menu Item | Function |
|-----------|----------|
| Settings | Open command settings interface |
| Language | Submenu: Chinese/English |
| About | Show author and contact info |
| Exit | Exit program |

### 8.3 Tray Behavior

You can configure the following tray behaviors in settings:

| Setting | Description |
|---------|-------------|
| StartWithWindows | Auto-start with system |
| MinimizeToTray | Minimize to tray |
| CloseToTray | Close to tray (don't exit program) |

---

## 9. Theme Settings

### 9.1 Switching Themes

Click the moon/sun icon in the upper right corner of the search box to switch themes:

- **Moon icon**: Currently light theme, click to switch to dark theme
- **Sun icon**: Currently dark theme, click to switch to light theme

### 9.2 Theme Types

| Theme | Description |
|-------|-------------|
| Light | Light theme, white background |
| Dark | Dark theme, dark gray background |

### 9.3 Theme Persistence

Theme settings are automatically saved to the configuration file and restored after restarting the program.

---

## 10. Language Switching

### 10.1 Supported Languages

Quanta supports the following languages:

| Language Code | Language Name |
|---------------|---------------|
| zh-CN | 简体中文 |
| en-US | English |

### 10.2 Switching Language

There are two ways to switch language:

1. **Via Tray Menu**: Right-click tray icon → Language → Select language
2. **Via Settings Interface**: Open Settings → Select language

### 10.3 Language Persistence

Language settings are automatically saved to the configuration file and restored after restarting the program.

---

## 11. Advanced Features

### 11.1 Configuration File

Quanta uses JSON format configuration file, mainly containing:

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
    "Language": "zh-CN"
  }
}
```

### 11.2 Configuration File Location

The configuration file is located in `config.json` in the program's running directory.

### 11.3 Usage Frequency Tracking

Quanta automatically records the usage count of each command. Commands with higher usage frequency are displayed first in search results.

Usage frequency data is stored in `usage.json` file.

### 11.4 Auto-start with System

You can enable auto-start with system in settings:

1. Open settings interface
2. Find "Start with Windows" option
3. Check to enable

When enabled, the program will automatically run when Windows starts.

---

## 12. Troubleshooting

### 12.1 Program Won't Start

**Problem**: Program doesn't respond after double-clicking Quanta.exe

**Solution**:
1. Confirm .NET 8.0 Runtime is installed
2. Check if other programs occupy the same port
3. Check log file for errors

### 12.2 Hotkey Conflict

**Problem**: Alt+Space is occupied by another program

**Solution**:
1. Close the program occupying the hotkey
2. Modify Quanta's hotkey in settings

### 12.3 No Search Results

**Problem**: No results displayed after entering keywords

**Solution**:
1. Confirm commands are configured correctly
2. Check keyword spelling
3. Try different keywords

### 12.4 Command Execution Failed

**Problem**: No response or error after executing command

**Solution**:
1. Check if command path is correct
2. Confirm program file exists
3. Try running as administrator

### 12.5 Tray Icon Not Displayed

**Problem**: No Quanta icon in system tray

**Solution**:
1. Check if program is running
2. Check if process exists in Task Manager
3. Restart program

### 12.6 Theme Not Applied

**Problem**: No change after switching theme

**Solution**:
1. Restart program
2. Check if configuration file has errors
3. Check logs for errors

---

## 13. Technical Specifications

### 13.1 Technology Stack

| Item | Technology |
|------|------------|
| Framework | .NET 8.0 / WPF |
| Architecture | MVVM |
| Language | C# |
| UI Framework | WPF (Windows Presentation Foundation) |

### 13.2 Project Structure

```
Quanta/
├── App.xaml / App.xaml.cs     # Entry file
├── Quanta.csproj              # Project configuration
├── config.json                # User configuration
├── quanta.ico                 # Application icon
├── Models/                    # Data models
├── ViewModels/                # View models
├── Views/                     # View files
├── Services/                  # Service layer
├── Helpers/                   # Helper utilities
└── Resources/                 # Resource files
```

### 13.3 Third-party Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM Framework |
| System.Drawing.Common | 8.0.0 | Graphics Processing |

### 13.4 Log Location

Program logs are located in the `logs` folder in the running directory.

### 13.5 Contact Support

- Author: yeal911
- Email: yeal91117@gmail.com

---

## Appendices

### Appendix A: Keyboard Shortcuts Quick Reference

| Shortcut | Function |
|----------|----------|
| Alt+Space | Summon/Hide search box |
| Enter | Execute selected command |
| Esc | Hide window |
| Tab | Enter parameter mode |
| Backspace | Return to normal search |
| ↑ | Select previous |
| ↓ | Select next |
| Ctrl+1~9 | Quick execute corresponding number |

### Appendix B: Command Type Description

| Type | Description | Example |
|------|-------------|---------|
| Url | Open webpage | `https://www.google.com` |
| Program | Launch program | `notepad.exe` |
| Shell | Execute command | `ping {param}` |
| Directory | Open directory | `C:\Users` |
| Calculator | Calculate expression | `{param}` |

---

*Document Version: 1.0*
*Last Updated: February 2026*
