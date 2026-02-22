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

- **全局快捷键** - 默认 `Alt+R`，可在设置中自定义
- **模糊搜索** - 关键字 / 名称 / 描述智能匹配，支持前缀模糊（输入 `rec` 自动匹配 `record`）
- **分组排序** - 结果按类别分组，组内按匹配度降序排列
- **自定义命令** - 支持 Url、Program、Shell、Directory、Calculator 五种类型
- **参数模式** - 按 `Tab` 进入参数输入（如 `g > rust` 执行 Google 搜索）
- **Ctrl+数字** - `Ctrl+1~9` 快速执行对应序号命令
- **失焦自动隐藏** - 点击其他窗口即自动收起

---

## 内置命令

所有内置命令支持模糊搜索匹配。

### 命令行工具

| 关键字 | 说明 |
|--------|------|
| `cmd` | 打开命令提示符 |
| `powershell` | 打开 PowerShell |

### 应用

| 关键字 | 说明 |
|--------|------|
| `notepad` | 记事本 |
| `calc` | Windows 计算器 |
| `mspaint` | 画图 |
| `explorer` | 资源管理器 |
| `winrecord` | Windows 内置录音机 |

### 系统管理

| 关键字 | 说明 |
|--------|------|
| `taskmgr` | 任务管理器 |
| `devmgmt` | 设备管理器 |
| `services` | 服务管理 |
| `regedit` | 注册表编辑器 |
| `control` | 控制面板 |
| `emptybin` | 清空回收站 |

### 网络诊断

| 关键字 | 说明 |
|--------|------|
| `ipconfig` | 查看 IP 配置 |
| `ping` | Ping（在 PowerShell 中运行） |
| `tracert` | 路由追踪 |
| `nslookup` | DNS 查询 |
| `netstat` | 查看网络连接状态 |

### 电源管理

| 关键字 | 说明 |
|--------|------|
| `lock` | 锁定屏幕 |
| `shutdown` | 10 秒后关机 |
| `restart` | 10 秒后重启 |
| `sleep` | 进入睡眠状态 |

### 特色功能

| 关键字 | 说明 |
|--------|------|
| `record` | 启动 Quanta 录音功能 |
| `clip` | 搜索剪贴板历史记录 |

### Quanta 系统命令

| 关键字 | 说明 |
|--------|------|
| `setting` | 打开设置界面 |
| `about` | 关于程序 |
| `exit` | 退出程序 |
| `english` | 切换界面语言为英文 |
| `chinese` | 切换界面语言为中文 |
| `spanish` | 切换界面语言为西班牙语 |

---

## 智能输入

无需选择命令，直接在搜索框输入，实时识别并展示结果。

### 数学计算

直接输入数学表达式即可计算：

```
2+2*3         → 8
(100-32)/1.8  → 37.778
2^10          → 1024
15%4          → 3
```

支持运算符：`+ - * / % ^`，支持括号和浮点数。

### 单位转换

格式：`数值 源单位 to 目标单位`

```
100 km to miles    → 62.137 miles
100 c to f         → 212 °F
30 f to c          → -1.111 °C
1 kg to lbs        → 2.205 lbs
500 g to oz        → 17.637 oz
1 gallon to l      → 3.785 l
1 acre to m2       → 4046.856 m²
```

支持类别：长度、重量、温度、面积、体积

### 汇率转换

格式：`数值 源货币 to 目标货币`（需在设置中配置 API Key）

```
100 usd to cny     → ¥736.50 CNY
1000 cny to jpy    → ¥20,500 JPY
50 eur to gbp      → £42.30 GBP
```

支持 40+ 种货币，汇率数据本地缓存（可设置缓存时长，默认 1 小时）。

### 颜色转换

支持 HEX、RGB、HSL 三种格式互转，并显示颜色预览：

```
#E67E22            → rgb(230,126,34)  hsl(28,79%,52%)
rgb(230,126,34)    → #E67E22  hsl(28,79%,52%)
hsl(28,79%,52%)    → #E67E22  rgb(230,126,34)
255,165,0          → #FFA500  hsl(38,100%,50%)
```

### 文本工具

| 前缀 | 功能 | 示例 |
|------|------|------|
| `base64 文本` | Base64 编码 | `base64 hello` → `aGVsbG8=` |
| `base64d 文本` | Base64 解码 | `base64d aGVsbG8=` → `hello` |
| `md5 文本` | MD5 哈希 | `md5 hello` → `5d41402a...` |
| `sha256 文本` | SHA-256 哈希 | `sha256 hello` → `2cf24dba...` |
| `url 文本` | URL 编码 / 自动解码 | `url hello world` → `hello%20world` |
| `json JSON字符串` | JSON 格式化美化 | `json {"a":1}` → 格式化输出 |

### Google 搜索

格式：`g 关键词`

```
g rust programming  → 在浏览器打开 Google 搜索
```

### PowerShell 执行

前缀 `>` 直接执行 PowerShell 命令：

```
> Get-Process      → 显示所有进程
> dir C:\          → 列出 C 盘文件
```

### 二维码生成

输入超过阈值字符数（默认 20）的文本，自动生成二维码并复制到剪贴板：

```
https://github.com/yeal911/Quanta   → 自动生成二维码
```

---

## 录音功能

输入 `record` 启动录音面板：

- **音源**：麦克风 / 扬声器（系统音频）/ 麦克风 + 扬声器
- **格式**：m4a（AAC）/ mp3
- **码率**：32 / 64 / 96 / 128 / 160 kbps
- **声道**：单声道 / 立体声
- **操作**：开始 → 暂停 / 恢复 → 停止，录音文件保存到指定目录
- **右键芯片**：可随时切换各项参数

---

## 剪贴板历史

输入 `clip` 或 `clip 关键字` 搜索剪贴板历史：

- 自动记录复制到剪贴板的文本，最多保存 50 条
- 支持关键字过滤，显示最近 20 条
- 点击结果自动复制到剪贴板
- 历史数据持久化保存

---

## 界面与体验

- **明暗主题** - 设置中切换 Light / Dark，配置自动保存
- **多语言** - 中文 / English / Español，切换即时生效
- **系统托盘** - 最小化驻留托盘，右键菜单快速操作
- **开机自启** - 设置中一键开启
- **Toast 通知** - 操作成功 / 失败即时反馈

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

## 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Alt+R` | 唤出 / 隐藏主窗口 |
| `Enter` | 执行选中命令 |
| `Tab` | 进入参数模式 |
| `Esc` | 退出参数模式 / 隐藏窗口 |
| `↑` / `↓` | 上 / 下移动选择 |
| `Ctrl+1~9` | 快速执行对应序号命令 |
| `Backspace` | 参数模式下退回普通搜索 |

---

## 命令管理

在设置界面（`setting` 命令或右键托盘）的「命令管理」选项卡中：

- 表格内直接编辑关键字、名称、类型、路径
- 支持 JSON 导入 / 导出，便于备份和迁移

### 命令类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `Url` | 浏览器打开网址（支持 `{param}` 参数） | `https://google.com/search?q={param}` |
| `Program` | 启动程序 | `notepad.exe` |
| `Shell` | 在 cmd 中执行命令 | `ping {param}` |
| `Directory` | 资源管理器打开文件夹 | `C:\Users\Public` |
| `Calculator` | 计算表达式 | `{param}` |

---

## 设置

| 设置项 | 说明 |
|--------|------|
| 快捷键 | 自定义全局热键（默认 `Alt+R`） |
| 主题 | Light / Dark |
| 语言 | 中文 / English / Español |
| 开机启动 | 开机自动运行 |
| 最多显示 | 搜索结果数量限制 |
| 长文本转二维码 | 触发二维码生成的字符数阈值 |
| 汇率 API Key | exchangerate-api.com 的 API Key |
| 汇率缓存时长 | 缓存有效期（小时），超出后重新调用接口 |
| 录音音源 | 麦克风 / 扬声器 / 两者 |
| 录音格式 | m4a / mp3 |
| 录音码率 | 32 / 64 / 96 / 128 / 160 kbps |
| 录音声道 | 单声道 / 立体声 |
| 录音输出路径 | 录音文件保存目录 |

---

## 技术架构

### 项目结构

```
Quanta/
├── App.xaml / App.xaml.cs          # 入口，托盘、主题、生命周期
├── Views/                           # 视图层
│   ├── MainWindow.xaml              # 主搜索窗口
│   ├── RecordingOverlayWindow.xaml  # 录音悬浮窗
│   └── CommandSettingsWindow.xaml   # 设置窗口（含 5 个 partial 文件）
├── Domain/
│   ├── Search/                      # 搜索引擎、Provider、结果类型
│   │   ├── SearchEngine.cs
│   │   ├── ExchangeRateService.cs
│   │   ├── ApplicationSearchProvider.cs
│   │   ├── FileSearchProvider.cs
│   │   └── RecentFileSearchProvider.cs
│   └── Commands/                    # 命令路由、各类 Handler
│       ├── CommandRouter.cs
│       └── CommandHandlers.cs
├── Core/
│   ├── Config/                      # AppConfig、ConfigLoader
│   └── Services/                    # RecordingService、ToastService
├── Infrastructure/
│   ├── System/                      # 剪贴板历史、窗口管理
│   └── Logging/                     # 日志
├── Presentation/
│   └── Helpers/                     # LocalizationService
└── Resources/
    └── Themes/                      # LightTheme.xaml、DarkTheme.xaml
```

### 技术栈

| 项目 | 技术 |
|------|------|
| 框架 | .NET 8.0 / WPF |
| 架构 | MVVM（CommunityToolkit.Mvvm） |
| 语言 | C# |
| 录音 | NAudio（WASAPI） |
| 二维码 | QRCoder |
| 汇率 | exchangerate-api.com |

---

## 作者

**yeal911** · yeal91117@gmail.com

## 许可证

MIT License
