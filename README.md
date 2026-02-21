# Quanta - Windows 快速启动器

<p align="center">
  <b>中文</b> | <a href="README_EN.md">English</a> | <a href="README_ES.md">Español</a>
</p>

<p align="center">
  <img src="Resources\imgs\quanta.ico" width="64" alt="Quanta"/>
</p>

<p align="center">
  一款轻量级 Windows 快速启动器，全局快捷键唤出，模糊搜索，秒级执行。
</p>

---

## 功能特性

### 核心功能

- **全局快捷键** - 默认 `Alt+R`，可自定义修改
- **模糊搜索** - 关键字、名称、描述智能匹配，按匹配度 + 使用频率排序
- **自定义命令** - 支持 Url、Program、Shell、Directory、Calculator 五种类型
- **参数模式** - 按 `Tab` 进入参数输入（如 `g > rust` 执行搜索）
- **Ctrl+数字** - `Ctrl+1~9` 直接执行对应序号命令
- **鼠标单击执行** - 搜索结果支持单击直接执行
- **失焦自动隐藏** - 点击其他窗口即自动收起

### 快捷命令

| 关键字 | 说明 | 关键字 | 说明 |
|--------|------|--------|------|
| `cmd` | 命令提示符 | `powershell` | PowerShell |
| `notepad` | 记事本 | `calc` | 计算器 |
| `explorer` | 资源管理器 | `taskmgr` | 任务管理器 |
| `control` | 控制面板 | `regedit` | 注册表 |
| `services` | 服务管理 | `devmgmt` | 设备管理器 |
| `ping` | Ping | `ipconfig` | IP 配置 |
| `tracert` | 路由追踪 | `nslookup` | DNS 查询 |
| `netstat` | 网络状态 | `mspaint` | 画图 |
| `lock` | 锁屏 | `shutdown` | 关机 |
| `restart` | 重启 | `sleep` | 睡眠 |
| `emptybin` | 清空回收站 | `winrecord` | Windows 录音机 |
| `b` | 百度搜索 | `g` | Google 搜索 |
| `gh` | GitHub 搜索 | | |

### 特色功能

- **数学计算器** - 直接输入数学表达式计算（如 `2+2`、`sqrt(16)*3`）
- **单位转换** - 支持长度、重量、温度、面积、体积等（如 `100 km to miles`、`100 c to f`、`1 kg to lbs`）
- **汇率转换** - 输入汇率查询（如 `100 usd to cny`），需配置 API Key
- **剪贴板历史** - 输入 `clip` 查看剪贴板历史，支持搜索过滤
- **屏幕录音** - 输入 `record` 启动录音，支持麦克风 / 扬声器录制，可选 m4a/mp3 格式
- **二维码生成** - 输入超过 20 字符的长文本自动生成二维码，二维码自动复制到剪贴板

### 界面与体验

- **明暗主题** - 点击右上角图标切换，配置自动保存
- **多语言** - 中文 / English / Español，切换即时生效
- **系统托盘** - 最小化驻留托盘，右键菜单管理
- **开机自启** - 配置后开机自动运行
- **Toast 通知** - 操作成功 / 失败反馈

---

## 快速开始

### 环境要求

- Windows 10 / 11（x64）
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### 运行程序

```bash
dotnet run
```

### 发布单文件

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## 使用说明

### 基本操作

1. 启动后程序驻留系统托盘
2. 按 `Alt+R` 唤出搜索框
3. 输入命令关键字（如 `g`、`notepad`、`clip`）
4. 方向键选择或 `Ctrl+数字` 直接定位
5. `Enter` 执行，`Esc` 隐藏

### 参数模式

搜索到带参数的命令后按 `Tab` 进入参数模式：

```
输入: g          → 匹配到 Google 搜索
按 Tab → g >     → 进入参数输入
输入: g > rust   → 执行 Google 搜索 "rust"
```

### 计算器与转换

```
输入: 2+2*3     → 计算结果: 8
输入: sqrt(16)  → 计算结果: 4
输入: 100 km to miles → 单位转换
输入: 100 c to f     → 温度转换: 212 °F
输入: 1 kg to lbs    → 重量转换: 2.205 lbs
输入: 100 usd to cny → 汇率转换
```

### 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Alt+R` | 唤出 / 隐藏主窗口 |
| `Enter` | 执行选中命令 |
| `Tab` | 进入参数模式 |
| `Esc` | 退出参数模式 / 隐藏窗口 |
| `↑` / `↓` | 选择上 / 下一条结果 |
| `Ctrl+1~9` | 快速执行对应序号命令 |
| `Backspace` | 参数模式下退回普通搜索 |

### 命令管理

- **右键托盘图标** → 打开命令设置
- 表格内直接编辑关键字、名称、类型、路径
- 支持 JSON 导入 / 导出，便于备份

### 设置项

| 设置项 | 说明 |
|--------|------|
| 快捷键 | 自定义全局热键 |
| 主题 | Light / Dark |
| 语言 | 中文 / English / Español |
| 开机启动 | 开机自动运行 |
| 最多显示 | 搜索结果数量限制 |
| 长文本转二维码 | 触发阈值字符数 |

---

## 配置文件

配置文件为程序目录下的 `config.json`。

### 命令字段说明

| 字段 | 说明 |
|------|------|
| `Keyword` | 搜索触发关键字 |
| `Name` | 命令显示名称 |
| `Type` | Url / Program / Shell / Directory / Calculator |
| `Path` | 执行路径或 URL，支持 `{param}` 占位符 |
| `Arguments` | 启动参数 |
| `RunAsAdmin` | 管理员权限运行 |
| `RunHidden` | 隐藏窗口运行 |

### 命令类型

| 类型 | 示例 | 效果 |
|------|------|------|
| `Url` | `https://google.com/search?q={param}` | 浏览器打开 |
| `Program` | `notepad.exe` | 启动程序 |
| `Shell` | `ping {param}` | cmd 执行 |
| `Directory` | `C:\Users\Public` | 资源管理器打开 |
| `Calculator` | `{param}` | 计算表达式 |

### 配置示例

```json
{
  "Hotkey": {
    "Modifier": "Alt",
    "Key": "R"
  },
  "Theme": "Light",
  "Language": "zh-CN",
  "Commands": [
    {
      "Keyword": "g",
      "Name": "Google 搜索",
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

## 技术架构

### 项目结构

```
Quanta/
├── App.xaml / App.xaml.cs     # 入口文件
├── Quanta.csproj              # 项目配置
├── config.json                # 用户配置
├── Views/                     # 视图层
│   ├── MainWindow.xaml        # 主搜索窗口
│   ├── SettingsWindow.xaml    # 设置窗口
│   └── CommandSettingsWindow.xaml  # 命令管理窗口
├── ViewModels/                # 视图模型
├── Models/                    # 数据模型
├── Services/                  # 服务层
├── Helpers/                   # 辅助工具
├── Infrastructure/            # 基础设施
│   ├── Storage/              # 配置存储
│   ├── Logging/              # 日志服务
│   └── System/               # 系统集成
└── Resources/                 # 资源文件
    └── Themes/                # 主题文件
```

### 技术栈

| 项目 | 技术 |
|------|------|
| 框架 | .NET 8.0 / WPF |
| 架构 | MVVM |
| 语言 | C# |
| UI | WPF |

### 第三方依赖

| 包 | 版本 | 用途 |
|----|------|------|
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 框架 |
| System.Drawing.Common | 8.0.0 | 图形处理 |

---

## 作者

**yeal911** · yeal91117@gmail.com

## 许可证

MIT License
