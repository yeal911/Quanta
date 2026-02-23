// ============================================================================
// 文件名: LocalizationService.cs
// 描述: 本地化（国际化）服务，提供多语言文本翻译功能。
//       支持简体中文（zh-CN）、英文（en-US）、西班牙语（es-ES）等多种语言。
//       语言设置会自动持久化到应用配置文件中。
// ============================================================================

using System.Collections.Generic;
using System.Linq;
using Quanta.Helpers;
using Quanta.Models;

namespace Quanta.Services;

/// <summary>
/// 本地化服务（静态类），提供应用程序的多语言翻译支持。
/// 通过内置的翻译字典管理多语言文本，支持语言切换和从配置文件加载语言设置。
/// 当找不到当前语言的翻译时，会自动回退到中文（zh-CN）。
/// </summary>
public static class LocalizationService
{
    /// <summary>
    /// 多语言翻译字典，外层 Key 为语言代码（如 zh-CN、en-US），
    /// 内层 Key 为翻译键名，Value 为对应的翻译文本
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["zh-CN"] = new Dictionary<string, string>
        {
            ["TrayShow"] = "显示界面",
            ["TraySettings"] = "设置",
            ["TrayAbout"] = "关于",
            ["TrayExit"] = "退出",
            ["TrayLanguage"] = "语言",
            ["TrayChinese"] = "中文",
            ["TrayEnglish"] = "English",
            ["TraySpanish"] = "Español",
            ["LanguageChanged"] = "语言已切换",
            ["GroupQuickCommands"] = "快捷命令",
            ["SearchPlaceholder"] = "请输入命令关键字 | Esc 隐藏",
            ["SettingsTitle"] = "设置",
            ["HotkeyLabel"] = "界面呼出快捷键",
            ["AddCommand"] = "添加",
            ["DeleteCommand"] = "删除",
            ["SaveCommand"] = "保存",
            ["CustomCommands"] = "自定义命令",
            ["Index"] = "序号",
            ["Keyword"] = "关键字",
            ["Name"] = "名称",
            ["Type"] = "类型",
            ["Path"] = "路径/URL",
            ["Arguments"] = "参数",
            ["Enabled"] = "启用",
            ["About"] = "关于",
            ["Author"] = "作者",
            ["Email"] = "邮箱",
            ["Footer"] = "提示：命令参数占位符{%p}",
            ["Added"] = "已添加",
            ["Deleted"] = "已删除",
            ["Saved"] = "已保存",
            ["HotkeyPress"] = "请按下快捷键...",
            ["CommandExists"] = "命令已存在",
            ["AddedCount"] = "添加了 {0} 个命令",
            ["ImportCommand"] = "导入命令",
            ["ExportCommand"] = "导出命令",
            ["ExportSuccess"] = "导出成功",
            ["ExportFailed"] = "导出失败",
            ["ImportResult"] = "成功导入 {0} 条，跳过 {1} 条（关键字重复）",
            ["ImportFailed"] = "导入失败",
            ["HotkeyRegisterFailed"] = "快捷键注册失败，可能已被其他程序占用",
            ["CommandSearchPlaceholder"] = "输入关键字搜索命令",
            ["Admin"] = "管理员",
            ["CopiedToClipboard"] = "结果已复制到剪贴板",
            ["CopyFailed"] = "复制失败",
            ["ColorCopied"] = "已复制: {0}",
            ["TestCommand"] = "测试",
            ["TestSelectFirst"] = "请先选中要测试的命令",
            ["TestUnavailable"] = "测试功能不可用",
            ["TestExecuted"] = "命令已执行",
            ["TestFailed"] = "命令执行失败",
            ["StartupSettingFailed"] = "开机启动设置失败: {0}",
            ["GroupCommand"] = "命令",
            ["GroupApp"] = "应用",
            ["GroupFile"] = "文件",
            ["GroupWindow"] = "窗口",
            ["GroupCalc"] = "计算",
            ["GroupWeb"] = "网络",
            ["GroupText"] = "文本工具",
            ["GroupClip"] = "剪贴板历史",
            ["TextBase64Encode"] = "Base64 编码",
            ["TextBase64Decode"] = "Base64 解码",
            ["TextBase64DecodeFail"] = "Base64 解码失败",
            ["TextBase64DecodeFailHint"] = "输入不是有效的 Base64 或无法解码为 UTF-8 文本",
            ["TextUrlEncode"] = "URL 编码",
            ["TextUrlDecode"] = "URL 解码",
            ["TextJsonLines"] = "JSON ({0} 行)",
            ["TextJsonError"] = "JSON 格式错误",
            ["QRCodeTooLong"] = "文本超过2000字符，无法生成二维码",
            ["SettingsTitle"] = "设置",
            ["GeneralSettings"] = "通用设置",
            ["MenuGeneral"] = "通用配置",
            ["MenuRecording"] = "录音配置",
            ["MenuExchangeRate"] = "汇率接口",
            ["MenuCommands"] = "命令管理",
            ["MenuBuiltInCommands"] = "内置命令",
            ["BuiltInCommandsSettings"] = "内置命令一览",
            ["BuiltInCommandGroup"] = "命令分组",
            ["BuiltInCommand"] = "命令",
            ["BuiltInCommandFunction"] = "命令功能",
            ["BuiltInCommandUsage"] = "使用说明",
            ["CommandManagement"] = "命令管理",
            ["Hotkey"] = "快捷键：",
            ["Import"] = "导入",
            ["Export"] = "导出",
            ["StartWithWindows"] = "开机启动",
            ["MaxResults"] = "最多显示：",
            ["MaxResultsSuffix"] = " 条结果",
            ["QRCodeThreshold"] = "长文本生成二维码：",
            ["QRCodeThresholdSuffix"] = " 字符",
            ["CommandManagement"] = "命令管理",
            ["SearchCommands"] = "输入关键字搜索命令",
            ["Add"] = "添加",
            ["Delete"] = "删除",
            ["Save"] = "保存",
            ["Footer"] = "提示：命令参数占位符{%p}",
            ["LanguageLabel"] = "语言：",
            // 语言选项（ComboBox 显示用）
            ["LanguageChinese"] = "中文",
            ["LanguageEnglish"] = "English",
            ["LanguageSpanish"] = "Español",
            // ── 录音设置 ──────────────────────────────────────────────
            ["RecordingSettings"] = "录音设置",
            ["RecordSource"] = "录制源",
            ["RecordFormat"] = "输出格式",
            ["RecordSampleRate"] = "采样率",
            ["RecordBitrate"] = "码率",
            ["RecordChannels"] = "声道",
            ["RecordOutputPath"] = "输出路径",
            ["RecordOutputPathDefault"] = "默认（桌面）",
            ["RecordChannelStereo"] = "立体声",
            ["RecordChannelMono"] = "单声道",
            // 录音格式选项
            ["RecordFormatM4a"] = "m4a",
            ["RecordFormatMp3"] = "mp3",
            // 码率选项
            ["RecordBitrate32"] = "32 kbps",
            ["RecordBitrate64"] = "64 kbps",
            ["RecordBitrate96"] = "96 kbps",
            ["RecordBitrate128"] = "128 kbps",
            ["RecordBitrate160"] = "160 kbps",
            ["RecordBrowse"] = "浏览",
            ["RecordEstimatedSize"] = "预估每分钟大小",
            ["RecordEstimatedSizeUnit"] = "KB/分钟",
            ["RecordEstimatedSizeUnitMb"] = "MB/分钟",
            ["ExchangeRateApiHint"] = "免费 API: https://exchangerate-api.com",
            ["RecordCommandDesc"] = "启动录音",
            ["RecordOutputFile"] = "输出文件",
            ["RecordPreview"] = "录音预览",
            ["RecordStart"] = "开始录音",
            ["RecordPause"] = "暂停录音",
            ["RecordResume"] = "继续录音",
            ["RecordStop"] = "停止录音",
            ["RecordDrop"] = "放弃录音",
            ["RecordDropping"] = "正在放弃...",
            ["RecordHide"] = "隐藏到后台",
            ["RecordDuration"] = "时长",
            ["RecordFileSize"] = "大小",
            ["RecordClickToOpen"] = "点击打开目录",
            ["RecordAlreadyRecording"] = "录音正在进行中，请先停止当前录音",
            ["RecordNoDevice"] = "未检测到录音设备",
            ["RecordDeviceBusy"] = "录音设备被占用，请关闭其他录音程序后重试",
            ["RecordNoPermission"] = "无写入权限，请检查输出目录",
            ["RecordDiskFull"] = "磁盘空间不足，请清理磁盘后重试",
            ["RecordError"] = "录音异常，已自动停止",
            ["RecordStarted"] = "录音已开始",
            ["RecordPaused"] = "录音已暂停",
            ["RecordResumed"] = "录音已继续",
            ["RecordStopped"] = "录音已完成",
            ["RecordSaving"] = "正在保存录音文件...",
            ["RecordSaved"] = "录音文件已保存",
            ["RecordEncoding"] = "正在转码",
            ["RecordRightClickSource"] = "右键点击切换录制源",
            ["RecordRightClickFormat"] = "右键点击切换格式",
            ["RecordRightClickBitrate"] = "右键点击切换码率",
            ["RecordRightClickChannels"] = "右键点击切换声道",
            ["RecordStarting"] = "启动录音中...",
            ["RecordingInProgress"] = "录音中...",
            ["WinRecordNotInstalled"] = "Windows 录音机未安装，请从 Microsoft Store 搜索「录音机」下载",
            // 汇率换算
            ["ExchangeRateFetching"] = "正在获取汇率...",
            ["ExchangeRateNoApiKey"] = "请在设置中配置 Exchangerate-API Key",
            ["ExchangeRateApiError"] = "汇率获取失败，请检查网络或API Key",
            ["ExchangeRateNotSupported"] = "不支持兑换 {0}",
            ["ExchangeRateInvalidAmount"] = "无效的金额",
            ["ExchangeRateFromCache"] = "缓存",
            ["ExchangeRateToday"] = "今天",
            ["ExchangeRateYesterday"] = "昨天",
            // 设置界面
            ["ExchangeRateApiLabel"] = "汇率 API：",
            ["ExchangeRateApiKeyPlaceholder"] = "输入 API Key",
            // 设置界面其他
            ["DarkTheme"] = "暗色主题",
            ["HotkeyClearHint"] = "双击清除快捷键",
            // 主题切换
            ["ThemeSwitch"] = "切换主题",
            // 右键复制
            ["RightClickCopy"] = "右键复制",
            // 录音命令提示
            ["RecordChipHint"] = "右键点击可切换参数",
            // 录音悬浮窗按钮提示
            ["RecordPauseTooltip"] = "暂停录音",
            ["RecordResumeTooltip"] = "继续录音",
            ["RecordStopTooltip"] = "停止录音",
            ["RecordDropTooltip"] = "放弃录音",
            ["RecordHideTooltip"] = "隐藏到后台",
            ["RecordClickToOpenDir"] = "点击打开录音目录",
            // 录音设备选项
            ["RecordSourceMic"] = "麦克风",
            ["RecordSourceSpeaker"] = "扬声器",
            ["RecordSourceMicSpeaker"] = "麦克风+扬声器",
            // 声道选项
            ["RecordChannelsStereo"] = "立体声",
            ["RecordChannelsMono"] = "单声道",
            // API Key 标签
            ["ExchangeRateApiKeyLabel"] = "API Key (ExchangeRate-API)",
            ["ExchangeRateCacheLabel"] = "缓存时长",
            ["ExchangeRateCacheUnit"] = "小时",
            ["ExchangeRateCacheHint"] = "超过此时长将重新调接口更新汇率",
            // ── 内置命令名称 ──────────────────────────────────────────
            ["BuiltinCmd_cmd"] = "命令提示符",
            ["BuiltinCmd_powershell"] = "PowerShell",
            ["BuiltinCmd_notepad"] = "记事本",
            ["BuiltinCmd_calc"] = "计算器",
            ["BuiltinCmd_mspaint"] = "画图",
            ["BuiltinCmd_explorer"] = "资源管理器",
            ["BuiltinCmd_taskmgr"] = "任务管理器",
            ["BuiltinCmd_devmgmt"] = "设备管理器",
            ["BuiltinCmd_services"] = "服务",
            ["BuiltinCmd_regedit"] = "注册表",
            ["BuiltinCmd_control"] = "控制面板",
            ["BuiltinCmd_ipconfig"] = "IP配置",
            ["BuiltinCmd_ping"] = "Ping",
            ["BuiltinCmd_tracert"] = "路由追踪",
            ["BuiltinCmd_nslookup"] = "DNS查询",
            ["BuiltinCmd_netstat"] = "网络状态",
            ["BuiltinCmd_lock"] = "锁屏",
            ["BuiltinCmd_shutdown"] = "关机",
            ["BuiltinCmd_restart"] = "重启",
            ["BuiltinCmd_sleep"] = "睡眠",
            ["BuiltinCmd_emptybin"] = "清空回收站",
            ["BuiltinCmd_setting"] = "打开设置",
            ["BuiltinCmd_exit"] = "退出程序",
            ["BuiltinCmd_about"] = "关于",
            ["BuiltinCmd_english"] = "切换到英文",
            ["BuiltinCmd_chinese"] = "切换到中文",
            ["BuiltinCmd_spanish"] = "切换到西班牙语",
            ["BuiltinCmd_winrecord"] = "Windows 录音机",
            // 内置命令描述
            ["BuiltinDesc_cmd"] = "打开CMD",
            ["BuiltinDesc_powershell"] = "打开PowerShell",
            ["BuiltinDesc_notepad"] = "打开记事本",
            ["BuiltinDesc_calc"] = "打开计算器",
            ["BuiltinDesc_mspaint"] = "打开画图",
            ["BuiltinDesc_explorer"] = "打开资源管理器",
            ["BuiltinDesc_taskmgr"] = "打开任务管理器",
            ["BuiltinDesc_devmgmt"] = "打开设备管理器",
            ["BuiltinDesc_services"] = "打开服务",
            ["BuiltinDesc_regedit"] = "打开注册表",
            ["BuiltinDesc_control"] = "打开控制面板",
            ["BuiltinDesc_ipconfig"] = "查看IP配置",
            ["BuiltinDesc_ping"] = "Ping命令",
            ["BuiltinDesc_tracert"] = "追踪路由",
            ["BuiltinDesc_nslookup"] = "DNS查询",
            ["BuiltinDesc_netstat"] = "查看网络状态",
            ["BuiltinDesc_lock"] = "锁定计算机",
            ["BuiltinDesc_shutdown"] = "10秒后关机",
            ["BuiltinDesc_restart"] = "10秒后重启",
            ["BuiltinDesc_sleep"] = "进入睡眠状态",
            ["BuiltinDesc_emptybin"] = "清空回收站",
            ["BuiltinDesc_setting"] = "打开设置界面",
            ["BuiltinDesc_exit"] = "退出 Quanta",
            ["BuiltinDesc_about"] = "关于程序",
            ["BuiltinDesc_english"] = "切换界面语言为英文",
            ["BuiltinDesc_chinese"] = "切换界面语言为中文",
            ["BuiltinDesc_spanish"] = "切换界面语言为西班牙语",
            ["BuiltinDesc_winrecord"] = "打开 Windows 内置录音机",
            // ── 新增分组 key ─────────────────────────────────────────
            ["GroupSystem"] = "系统",
            ["GroupNetwork"] = "网络",
            ["GroupPower"] = "电源",
            ["GroupQuanta"] = "Quanta",
            ["GroupFeature"] = "功能",
            ["GroupQRCode"] = "二维码",
            // 二维码 Toast 提示
            ["QRCodeGenerate"] = "生成二维码",
            ["QRCodeCopied"] = "二维码已复制到剪贴板",
            // record / clip 内置命令
            ["BuiltinCmd_record"] = "录音",
            ["BuiltinDesc_record"] = "开始一段新录音",
            ["BuiltinCmd_clip"] = "剪贴板历史",
            ["BuiltinDesc_clip"] = "搜索剪贴板历史记录",
            // ── 文件搜索设置 ───────────────────────────────────────────
            ["MenuFileSearch"] = "文件搜索",
            ["FileSearchSettings"] = "文件搜索设置",
            ["FileSearchEnabled"] = "启用文件搜索",
            ["FileSearchDirectories"] = "搜索目录：",
            ["FileSearchDirectoriesHint"] = "每行一个目录，留空使用默认（桌面+下载）",
            ["FileSearchMaxFiles"] = "每个目录最多扫描：",
            ["FileSearchMaxFilesSuffix"] = " 个文件",
            ["FileSearchMaxResults"] = "最多显示：",
            ["FileSearchMaxResultsSuffix"] = " 条结果",
            ["FileSearchRecursive"] = "递归搜索子目录",
            ["FileSearchBrowse"] = "浏览",
            // ── 内置命令一览 ───────────────────────────────────────────
            ["MenuBuiltInCommands"] = "内置命令",
            ["BuiltInCommandsSettings"] = "内置命令一览",
            ["BuiltInCommandGroup"] = "命令分组",
            ["BuiltInCommand"] = "命令",
            ["BuiltInCommandFunction"] = "功能说明",
            ["BuiltInCommandUsage"] = "使用说明"
        },
        ["en-US"] = new Dictionary<string, string>
        {
            ["TrayShow"] = "Show",
            ["TraySettings"] = "Settings",
            ["TrayAbout"] = "About",
            ["TrayExit"] = "Exit",
            ["TrayLanguage"] = "Language",
            ["TrayChinese"] = "中文",
            ["TrayEnglish"] = "English",
            ["TraySpanish"] = "Español",
            ["LanguageChanged"] = "Language changed",
            ["GroupQuickCommands"] = "Quick Commands",
            ["SearchPlaceholder"] = "Enter keyword | Esc to hide",
            ["SettingsTitle"] = "Settings",
            ["HotkeyLabel"] = "Window Hotkey",
            ["AddCommand"] = "Add",
            ["DeleteCommand"] = "Delete",
            ["SaveCommand"] = "Save",
            ["CustomCommands"] = "Custom Commands",
            ["Index"] = "#",
            ["Keyword"] = "Keyword",
            ["Name"] = "Name",
            ["Type"] = "Type",
            ["Path"] = "Path/URL",
            ["Arguments"] = "Args",
            ["Enabled"] = "Enabled",
            ["About"] = "About",
            ["Author"] = "Author",
            ["Email"] = "Email",
            ["Footer"] = "Hint: Use {%p} as parameter placeholder",
            ["Added"] = "Added",
            ["Deleted"] = "Deleted",
            ["Saved"] = "Saved",
            ["HotkeyPress"] = "Press hotkey...",
            ["CommandExists"] = "Command already exists",
            ["AddedCount"] = "Added {0} commands",
            ["ImportCommand"] = "Import Commands",
            ["ExportCommand"] = "Export Commands",
            ["ExportSuccess"] = "Export successful",
            ["ExportFailed"] = "Export failed",
            ["ImportResult"] = "Imported {0}, skipped {1} (duplicate keywords)",
            ["ImportFailed"] = "Import failed",
            ["HotkeyRegisterFailed"] = "Hotkey registration failed, may be occupied by another program",
            ["CommandSearchPlaceholder"] = "Search commands by keyword",
            ["Admin"] = "Admin",
            ["CopiedToClipboard"] = "Result copied to clipboard",
            ["CopyFailed"] = "Copy Failed",
            ["ColorCopied"] = "Copied: {0}",
            ["TestCommand"] = "Test",
            ["TestSelectFirst"] = "Please select a command to test",
            ["TestUnavailable"] = "Test function unavailable",
            ["TestExecuted"] = "Command executed",
            ["TestFailed"] = "Command execution failed",
            ["StartupSettingFailed"] = "Startup setting failed: {0}",
            ["GroupCommand"] = "Commands",
            ["GroupApp"] = "Apps",
            ["GroupFile"] = "Files",
            ["GroupWindow"] = "Windows",
            ["GroupCalc"] = "Calculator",
            ["GroupWeb"] = "Web",
            ["GroupText"] = "Text Tools",
            ["GroupClip"] = "Clipboard History",
            ["TextBase64Encode"] = "Base64 Encode",
            ["TextBase64Decode"] = "Base64 Decode",
            ["TextBase64DecodeFail"] = "Base64 Decode Failed",
            ["TextBase64DecodeFailHint"] = "Invalid Base64 input or cannot decode as UTF-8",
            ["TextUrlEncode"] = "URL Encode",
            ["TextUrlDecode"] = "URL Decode",
            ["TextJsonLines"] = "JSON ({0} lines)",
            ["TextJsonError"] = "JSON Format Error",
            ["QRCodeTooLong"] = "Text exceeds 2000 characters, cannot generate QR code",
            ["SettingsTitle"] = "Settings",
            ["GeneralSettings"] = "General",
            ["MenuGeneral"] = "General",
            ["MenuRecording"] = "Recording",
            ["MenuExchangeRate"] = "Exchange Rate",
            ["MenuCommands"] = "Commands",
            ["CommandManagement"] = "Commands",
            ["Hotkey"] = "Hotkey:",
            ["Import"] = "Import",
            ["Export"] = "Export",
            ["StartWithWindows"] = "Start with Windows",
            ["MaxResults"] = "Max results:",
            ["MaxResultsSuffix"] = " results",
            ["QRCodeThreshold"] = "QR code threshold:",
            ["QRCodeThresholdSuffix"] = " chars",
            ["CommandManagement"] = "Commands",
            ["SearchCommands"] = "Search commands...",
            ["Add"] = "Add",
            ["Delete"] = "Delete",
            ["Save"] = "Save",
            ["Footer"] = "Hint: Use {%p} as parameter placeholder",
            ["LanguageLabel"] = "Language:",
            // Language options (for ComboBox display)
            ["LanguageChinese"] = "Chinese",
            ["LanguageEnglish"] = "English",
            ["LanguageSpanish"] = "Spanish",
            // ── Recording Settings ─────────────────────────────────────
            ["RecordingSettings"] = "Recording Settings",
            ["RecordSource"] = "Source",
            ["RecordFormat"] = "Format",
            ["RecordSampleRate"] = "Sample Rate",
            ["RecordBitrate"] = "Bitrate",
            ["RecordChannels"] = "Channels",
            ["RecordOutputPath"] = "Output Path",
            ["RecordOutputPathDefault"] = "Default (Desktop)",
            ["RecordChannelStereo"] = "Stereo",
            ["RecordChannelMono"] = "Mono",
            // Recording format options
            ["RecordFormatM4a"] = "m4a",
            ["RecordFormatMp3"] = "mp3",
            // Bitrate options
            ["RecordBitrate32"] = "32 kbps",
            ["RecordBitrate64"] = "64 kbps",
            ["RecordBitrate96"] = "96 kbps",
            ["RecordBitrate128"] = "128 kbps",
            ["RecordBitrate160"] = "160 kbps",
            ["RecordBrowse"] = "Browse",
            ["RecordEstimatedSize"] = "Est. Size per Minute",
            ["RecordEstimatedSizeUnit"] = "KB/min",
            ["RecordEstimatedSizeUnitMb"] = "MB/min",
            ["ExchangeRateApiHint"] = "Free API: https://exchangerate-api.com",
            ["RecordCommandDesc"] = "Start Recording",
            ["RecordOutputFile"] = "Output File",
            ["RecordPreview"] = "Recording Preview",
            ["RecordStart"] = "Start Recording",
            ["RecordPause"] = "Pause",
            ["RecordResume"] = "Resume",
            ["RecordStop"] = "Stop",
            ["RecordDrop"] = "Discard",
            ["RecordDropping"] = "Discarding...",
            ["RecordHide"] = "Hide to tray",
            ["RecordDuration"] = "Duration",
            ["RecordFileSize"] = "Size",
            ["RecordClickToOpen"] = "Click to open folder",
            ["RecordAlreadyRecording"] = "Recording in progress, please stop the current recording first",
            ["RecordNoDevice"] = "No recording device detected",
            ["RecordDeviceBusy"] = "Recording device is in use, please close other recording programs",
            ["RecordNoPermission"] = "No write permission, please check the output directory",
            ["RecordDiskFull"] = "Disk is full, please free up space",
            ["RecordError"] = "Recording error, stopped automatically",
            ["RecordStarted"] = "Recording started",
            ["RecordPaused"] = "Recording paused",
            ["RecordResumed"] = "Recording resumed",
            ["RecordStopped"] = "Recording completed",
            ["RecordSaving"] = "Saving recording file...",
            ["RecordSaved"] = "Recording file saved",
            ["RecordEncoding"] = "Encoding",
            ["RecordRightClickSource"] = "Right-click to switch source",
            ["RecordRightClickFormat"] = "Right-click to switch format",
            ["RecordRightClickBitrate"] = "Right-click to switch bitrate",
            ["RecordRightClickChannels"] = "Right-click to switch channels",
            ["RecordStarting"] = "Starting recording...",
            ["RecordingInProgress"] = "Recording...",
            ["WinRecordNotInstalled"] = "Windows Voice Recorder is not installed. Search for \"Voice Recorder\" on Microsoft Store",
            // 汇率换算
            ["ExchangeRateFetching"] = "Fetching exchange rate...",
            ["ExchangeRateNoApiKey"] = "Please configure Exchangerate-API Key in settings",
            ["ExchangeRateApiError"] = "Failed to fetch rate, check network or API Key",
            ["ExchangeRateNotSupported"] = "Currency {0} not supported",
            ["ExchangeRateInvalidAmount"] = "Invalid amount",
            ["ExchangeRateFromCache"] = "Cache",
            ["ExchangeRateToday"] = "Today",
            ["ExchangeRateYesterday"] = "Yesterday",
            // Settings UI
            ["ExchangeRateApiLabel"] = "Exchange Rate API:",
            ["ExchangeRateApiKeyPlaceholder"] = "Enter API Key",
            // Settings UI Other
            ["DarkTheme"] = "Dark Theme",
            ["HotkeyClearHint"] = "Double-click to clear hotkey",
            // Theme switch
            ["ThemeSwitch"] = "Toggle theme",
            // Right-click copy
            ["RightClickCopy"] = "Right-click to copy",
            // Recording chip hint
            ["RecordChipHint"] = "Right-click to change settings",
            // Recording overlay button tooltips
            ["RecordPauseTooltip"] = "Pause recording",
            ["RecordResumeTooltip"] = "Resume recording",
            ["RecordStopTooltip"] = "Stop recording",
            ["RecordDropTooltip"] = "Discard recording",
            ["RecordHideTooltip"] = "Hide to tray",
            ["RecordClickToOpenDir"] = "Click to open recording folder",
            // Recording device options
            ["RecordSourceMic"] = "Microphone",
            ["RecordSourceSpeaker"] = "Speaker",
            ["RecordSourceMicSpeaker"] = "Microphone+Speaker",
            // Channel options
            ["RecordChannelsStereo"] = "Stereo",
            ["RecordChannelsMono"] = "Mono",
            // API Key label
            ["ExchangeRateApiKeyLabel"] = "API Key (ExchangeRate-API)",
            ["ExchangeRateCacheLabel"] = "Cache Duration",
            ["ExchangeRateCacheUnit"] = "hours",
            ["ExchangeRateCacheHint"] = "Rates older than this duration will be refreshed from API",
            // ── Built-in Command Names ─────────────────────────────────────
            ["BuiltinCmd_cmd"] = "Command Prompt",
            ["BuiltinCmd_powershell"] = "PowerShell",
            ["BuiltinCmd_notepad"] = "Notepad",
            ["BuiltinCmd_calc"] = "Calculator",
            ["BuiltinCmd_mspaint"] = "Paint",
            ["BuiltinCmd_explorer"] = "File Explorer",
            ["BuiltinCmd_taskmgr"] = "Task Manager",
            ["BuiltinCmd_devmgmt"] = "Device Manager",
            ["BuiltinCmd_services"] = "Services",
            ["BuiltinCmd_regedit"] = "Registry Editor",
            ["BuiltinCmd_control"] = "Control Panel",
            ["BuiltinCmd_ipconfig"] = "IP Configuration",
            ["BuiltinCmd_ping"] = "Ping",
            ["BuiltinCmd_tracert"] = "Route Trace",
            ["BuiltinCmd_nslookup"] = "DNS Lookup",
            ["BuiltinCmd_netstat"] = "Network Status",
            ["BuiltinCmd_lock"] = "Lock Screen",
            ["BuiltinCmd_shutdown"] = "Shutdown",
            ["BuiltinCmd_restart"] = "Restart",
            ["BuiltinCmd_sleep"] = "Sleep",
            ["BuiltinCmd_emptybin"] = "Empty Recycle Bin",
            ["BuiltinCmd_setting"] = "Open Settings",
            ["BuiltinCmd_exit"] = "Exit Program",
            ["BuiltinCmd_about"] = "About",
            ["BuiltinCmd_english"] = "Switch to English",
            ["BuiltinCmd_chinese"] = "Switch to Chinese",
            ["BuiltinCmd_spanish"] = "Switch to Spanish",
            ["BuiltinCmd_winrecord"] = "Windows Recorder",
            // Built-in Command Descriptions
            ["BuiltinDesc_cmd"] = "Open CMD",
            ["BuiltinDesc_powershell"] = "Open PowerShell",
            ["BuiltinDesc_notepad"] = "Open Notepad",
            ["BuiltinDesc_calc"] = "Open Calculator",
            ["BuiltinDesc_mspaint"] = "Open Paint",
            ["BuiltinDesc_explorer"] = "Open File Explorer",
            ["BuiltinDesc_taskmgr"] = "Open Task Manager",
            ["BuiltinDesc_devmgmt"] = "Open Device Manager",
            ["BuiltinDesc_services"] = "Open Services",
            ["BuiltinDesc_regedit"] = "Open Registry Editor",
            ["BuiltinDesc_control"] = "Open Control Panel",
            ["BuiltinDesc_ipconfig"] = "View IP Configuration",
            ["BuiltinDesc_ping"] = "Ping Command",
            ["BuiltinDesc_tracert"] = "Trace Route",
            ["BuiltinDesc_nslookup"] = "DNS Lookup",
            ["BuiltinDesc_netstat"] = "View Network Status",
            ["BuiltinDesc_lock"] = "Lock Computer",
            ["BuiltinDesc_shutdown"] = "Shutdown in 10 seconds",
            ["BuiltinDesc_restart"] = "Restart in 10 seconds",
            ["BuiltinDesc_sleep"] = "Enter Sleep Mode",
            ["BuiltinDesc_emptybin"] = "Empty Recycle Bin",
            ["BuiltinDesc_setting"] = "Open Settings Window",
            ["BuiltinDesc_exit"] = "Exit Quanta",
            ["BuiltinDesc_about"] = "About Program",
            ["BuiltinDesc_english"] = "Switch language to English",
            ["BuiltinDesc_chinese"] = "Switch language to Chinese",
            ["BuiltinDesc_spanish"] = "Switch language to Spanish",
            ["BuiltinDesc_winrecord"] = "Open Windows Recorder",
            // ── New group keys ──────────────────────────────────────────
            ["GroupSystem"] = "System",
            ["GroupNetwork"] = "Network",
            ["GroupPower"] = "Power",
            ["GroupQuanta"] = "Quanta",
            ["GroupFeature"] = "Features",
            ["GroupQRCode"] = "QR Code",
            // QR Code toast
            ["QRCodeGenerate"] = "Generate QR Code",
            ["QRCodeCopied"] = "QR code copied to clipboard",
            // record / clip built-in commands
            ["BuiltinCmd_record"] = "Recording",
            ["BuiltinDesc_record"] = "Start a new recording",
            ["BuiltinCmd_clip"] = "Clipboard History",
            ["BuiltinDesc_clip"] = "Search clipboard history",
            // ── File Search Settings ─────────────────────────────────────────
            ["MenuFileSearch"] = "File Search",
            ["FileSearchSettings"] = "File Search Settings",
            ["FileSearchEnabled"] = "Enable File Search",
            ["FileSearchDirectories"] = "Search Directories:",
            ["FileSearchDirectoriesHint"] = "One path per line, leave empty to use default (Desktop+Downloads)",
            ["FileSearchMaxFiles"] = "Max files per directory:",
            ["FileSearchMaxFilesSuffix"] = " files",
            ["FileSearchMaxResults"] = "Max results:",
            ["FileSearchMaxResultsSuffix"] = " results",
            ["FileSearchRecursive"] = "Search subdirectories recursively",
            ["FileSearchBrowse"] = "Browse",
            // ── Built-in Commands List ───────────────────────────────────────
            ["MenuBuiltInCommands"] = "Built-in Commands",
            ["BuiltInCommandsSettings"] = "Built-in Commands List",
            ["BuiltInCommandGroup"] = "Group",
            ["BuiltInCommand"] = "Command",
            ["BuiltInCommandFunction"] = "Function",
            ["BuiltInCommandUsage"] = "Usage"
        },
        ["es-ES"] = new Dictionary<string, string>
        {
            // 托盘菜单
            ["TrayShow"] = "Mostrar",
            ["TraySettings"] = "Ajustes",
            ["TrayAbout"] = "Acerca de",
            ["TrayExit"] = "Salir",
            ["TrayLanguage"] = "Idioma",
            ["TrayChinese"] = "中文",
            ["TrayEnglish"] = "English",
            ["TraySpanish"] = "Español",
            // 分组
            ["GroupQuickCommands"] = "Comandos Rápidos",
            ["GroupCommand"] = "Comandos",
            ["GroupApp"] = "Aplicaciones",
            ["GroupFile"] = "Archivos",
            ["GroupWindow"] = "Ventanas",
            ["GroupCalc"] = "Calculadora",
            ["GroupWeb"] = "Web",
            ["GroupText"] = "Herramientas de Texto",
            ["GroupClip"] = "Historial del Portapapeles",
            ["TextBase64Encode"] = "Codificar Base64",
            ["TextBase64Decode"] = "Decodificar Base64",
            ["TextBase64DecodeFail"] = "Error al Decodificar Base64",
            ["TextBase64DecodeFailHint"] = "Entrada Base64 inválida o no se puede decodificar como UTF-8",
            ["TextUrlEncode"] = "Codificar URL",
            ["TextUrlDecode"] = "Decodificar URL",
            ["TextJsonLines"] = "JSON ({0} líneas)",
            ["TextJsonError"] = "Error de Formato JSON",
            // 搜索
            ["SearchPlaceholder"] = "Ingrese palabra clave | Esc para ocultar",
            // 设置界面
            ["SettingsTitle"] = "Ajustes",
            ["HotkeyLabel"] = "Atajo de Teclado",
            ["AddCommand"] = "Agregar",
            ["DeleteCommand"] = "Eliminar",
            ["SaveCommand"] = "Guardar",
            ["CustomCommands"] = "Comandos Personalizados",
            ["Index"] = "#",
            ["Keyword"] = "Palabra Clave",
            ["Name"] = "Nombre",
            ["Type"] = "Tipo",
            ["Path"] = "Ruta/URL",
            ["Arguments"] = "Argumentos",
            ["Enabled"] = "Habilitado",
            ["About"] = "Acerca de",
            ["Author"] = "Autor",
            ["Email"] = "Correo",
            ["Footer"] = "Consejo: Use {%p} como marcador de posición",
            ["Added"] = "Agregado",
            ["Deleted"] = "Eliminado",
            ["Saved"] = "Guardado",
            ["HotkeyPress"] = "Presione el atajo...",
            ["CommandExists"] = "El comando ya existe",
            ["AddedCount"] = "Se agregaron {0} comandos",
            ["ImportCommand"] = "Importar Comandos",
            ["ExportCommand"] = "Exportar Comandos",
            ["ExportSuccess"] = "Exportación exitosa",
            ["ExportFailed"] = "Error en la exportación",
            ["ImportResult"] = "Importados {0}, omitidos {1} (palabras clave duplicadas)",
            ["ImportFailed"] = "Error en la importación",
            ["HotkeyRegisterFailed"] = "Error al registrar el atajo, puede estar ocupado por otro programa",
            ["CommandSearchPlaceholder"] = "Buscar comandos...",
            ["Admin"] = "Administrador",
            ["CopiedToClipboard"] = "Resultado copiado al portapapeles",
            ["CopyFailed"] = "Error al Copiar",
            ["ColorCopied"] = "Copiado: {0}",
            ["TestCommand"] = "Probar",
            ["TestSelectFirst"] = "Seleccione un comando para probar",
            ["TestUnavailable"] = "Función de prueba no disponible",
            ["TestExecuted"] = "Comando ejecutado",
            ["TestFailed"] = "Error al ejecutar el comando",
            ["StartupSettingFailed"] = "Error al configurar el inicio automático: {0}",
            ["QRCodeTooLong"] = "El texto supera los 2000 caracteres, no se puede generar código QR",
            ["GeneralSettings"] = "General",
            ["MenuGeneral"] = "General",
            ["MenuRecording"] = "Grabación",
            ["MenuExchangeRate"] = "Tipo de Cambio",
            ["MenuCommands"] = "Comandos",
            ["CommandManagement"] = "Gestión de Comandos",
            ["Hotkey"] = "Atajo:",
            ["Import"] = "Importar",
            ["Export"] = "Exportar",
            ["StartWithWindows"] = "Iniciar con Windows",
            ["MaxResults"] = "Máx. resultados:",
            ["MaxResultsSuffix"] = " resultados",
            ["QRCodeThreshold"] = "Umbral de código QR:",
            ["QRCodeThresholdSuffix"] = " caracteres",
            ["SearchCommands"] = "Buscar comandos...",
            ["Add"] = "Agregar",
            ["Delete"] = "Eliminar",
            ["Save"] = "Guardar",
            ["LanguageLabel"] = "Idioma:",
            // Language options (for ComboBox display)
            ["LanguageChinese"] = "Chino",
            ["LanguageEnglish"] = "Inglés",
            ["LanguageSpanish"] = "Español",
            // 录音设置
            ["RecordingSettings"] = "Ajustes de Grabación",
            ["RecordSource"] = "Fuente",
            ["RecordFormat"] = "Formato",
            ["RecordSampleRate"] = "Tasa de Muestreo",
            ["RecordBitrate"] = "Tasa de Bits",
            ["RecordChannels"] = "Canales",
            ["RecordOutputPath"] = "Ruta de Salida",
            ["RecordOutputPathDefault"] = "Predeterminado (Escritorio)",
            ["RecordChannelStereo"] = "Estéreo",
            ["RecordChannelMono"] = "Mono",
            // Recording format options
            ["RecordFormatM4a"] = "m4a",
            ["RecordFormatMp3"] = "mp3",
            // Bitrate options
            ["RecordBitrate32"] = "32 kbps",
            ["RecordBitrate64"] = "64 kbps",
            ["RecordBitrate96"] = "96 kbps",
            ["RecordBitrate128"] = "128 kbps",
            ["RecordBitrate160"] = "160 kbps",
            ["RecordBrowse"] = "Examinar",
            ["RecordEstimatedSize"] = "Tamaño Est. por Minuto",
            ["RecordEstimatedSizeUnit"] = "KB/min",
            ["RecordEstimatedSizeUnitMb"] = "MB/min",
            ["ExchangeRateApiHint"] = "API gratuita: https://exchangerate-api.com",
            ["RecordCommandDesc"] = "Iniciar Grabación",
            ["RecordOutputFile"] = "Archivo de Salida",
            ["RecordPreview"] = "Vista Previa",
            ["RecordStart"] = "Iniciar Grabación",
            ["RecordPause"] = "Pausar",
            ["RecordResume"] = "Reanudar",
            ["RecordStop"] = "Detener",
            ["RecordDrop"] = "Descartar",
            ["RecordDropping"] = "Descartando...",
            ["RecordHide"] = "Ocultar",
            ["RecordDuration"] = "Duración",
            ["RecordFileSize"] = "Tamaño",
            ["RecordClickToOpen"] = "Clic para abrir carpeta",
            ["RecordAlreadyRecording"] = "Grabación en progreso, deténgala primero",
            ["RecordNoDevice"] = "No se detectó dispositivo de grabación",
            ["RecordDeviceBusy"] = "Dispositivo en uso, cierre otros programas",
            ["RecordNoPermission"] = "Sin permiso de escritura, verifique la ruta",
            ["RecordDiskFull"] = "Disco lleno, libere espacio",
            ["RecordError"] = "Error de grabación, se detuvo automáticamente",
            ["RecordStarted"] = "Grabación iniciada",
            ["RecordPaused"] = "Grabación pausada",
            ["RecordResumed"] = "Grabación reanudada",
            ["RecordStopped"] = "Grabación completada",
            ["RecordSaving"] = "Guardando archivo...",
            ["RecordSaved"] = "Archivo guardado",
            ["RecordEncoding"] = "Codificando",
            ["RecordRightClickSource"] = "Clic derecho para cambiar fuente",
            ["RecordRightClickFormat"] = "Clic derecho para cambiar formato",
            ["RecordRightClickBitrate"] = "Clic derecho para cambiar tasa de bits",
            ["RecordRightClickChannels"] = "Clic derecho para cambiar canales",
            ["RecordStarting"] = "Iniciando grabación...",
            ["RecordingInProgress"] = "Grabando...",
            ["WinRecordNotInstalled"] = "La Grabadora de Voz de Windows no está instalada. Búscala en Microsoft Store",
            // 汇率换算
            ["ExchangeRateFetching"] = "Obteniendo tipo de cambio...",
            ["ExchangeRateNoApiKey"] = "Configure la clave API en ajustes",
            ["ExchangeRateApiError"] = "Error al obtener rate, verifique red o clave API",
            ["ExchangeRateNotSupported"] = "Moneda {0} no soportada",
            ["ExchangeRateInvalidAmount"] = "Monto inválido",
            ["ExchangeRateFromCache"] = "Caché",
            ["ExchangeRateToday"] = "Hoy",
            ["ExchangeRateYesterday"] = "Ayer",
            // 设置界面
            ["ExchangeRateApiLabel"] = "API de Tipo de Cambio:",
            ["ExchangeRateApiKeyPlaceholder"] = "Ingrese Clave API",
            // 设置界面其他
            ["DarkTheme"] = "Tema Oscuro",
            ["HotkeyClearHint"] = "Doble clic para borrar atajo",
            // Theme switch
            ["ThemeSwitch"] = "Cambiar tema",
            // Right-click copy
            ["RightClickCopy"] = "Clic derecho para copiar",
            // Recording overlay button tooltips
            // Sugerencia de chip de grabación
            ["RecordChipHint"] = "Clic derecho para cambiar ajustes",
            ["RecordPauseTooltip"] = "Pausar grabación",
            ["RecordResumeTooltip"] = "Reanudar grabación",
            ["RecordStopTooltip"] = "Detener grabación",
            ["RecordDropTooltip"] = "Descartar grabación",
            ["RecordHideTooltip"] = "Ocultar",
            ["RecordClickToOpenDir"] = "Clic para abrir carpeta de grabación",
            // 录音设备选项
            ["RecordSourceMic"] = "Micrófono",
            ["RecordSourceSpeaker"] = "Altavoz",
            ["RecordSourceMicSpeaker"] = "Micrófono+Altavoz",
            // 声道选项
            ["RecordChannelsStereo"] = "Estéreo",
            ["RecordChannelsMono"] = "Mono",
            // API Key 标签
            ["ExchangeRateApiKeyLabel"] = "Clave API (ExchangeRate-API)",
            ["ExchangeRateCacheLabel"] = "Duración de Caché",
            ["ExchangeRateCacheUnit"] = "horas",
            ["ExchangeRateCacheHint"] = "Las tasas más antiguas serán actualizadas desde la API",
            // ── 内置命令名称 ──────────────────────────────────────────
            ["BuiltinCmd_cmd"] = "Símbolo del Sistema",
            ["BuiltinCmd_powershell"] = "PowerShell",
            ["BuiltinCmd_notepad"] = "Bloc de Notas",
            ["BuiltinCmd_calc"] = "Calculadora",
            ["BuiltinCmd_mspaint"] = "Paint",
            ["BuiltinCmd_explorer"] = "Explorador de Archivos",
            ["BuiltinCmd_taskmgr"] = "Administrador de Tareas",
            ["BuiltinCmd_devmgmt"] = "Administrador de Dispositivos",
            ["BuiltinCmd_services"] = "Servicios",
            ["BuiltinCmd_regedit"] = "Editor del Registro",
            ["BuiltinCmd_control"] = "Panel de Control",
            ["BuiltinCmd_ipconfig"] = "Configuración IP",
            ["BuiltinCmd_ping"] = "Ping",
            ["BuiltinCmd_tracert"] = "Trace Route",
            ["BuiltinCmd_nslookup"] = "Búsqueda DNS",
            ["BuiltinCmd_netstat"] = "Estado de Red",
            ["BuiltinCmd_lock"] = "Bloquear Pantalla",
            ["BuiltinCmd_shutdown"] = "Apagar",
            ["BuiltinCmd_restart"] = "Reiniciar",
            ["BuiltinCmd_sleep"] = "Suspender",
            ["BuiltinCmd_emptybin"] = "Vaciar Papelera",
            ["BuiltinCmd_setting"] = "Abrir Ajustes",
            ["BuiltinCmd_exit"] = "Salir del Programa",
            ["BuiltinCmd_about"] = "Acerca de",
            ["BuiltinCmd_english"] = "Cambiar a Inglés",
            ["BuiltinCmd_chinese"] = "Cambiar a Chino",
            ["BuiltinCmd_spanish"] = "Cambiar a Español",
            ["BuiltinCmd_winrecord"] = "Grabadora de Windows",
            // 内置命令描述
            ["BuiltinDesc_cmd"] = "Abrir Símbolo del Sistema",
            ["BuiltinDesc_powershell"] = "Abrir PowerShell",
            ["BuiltinDesc_notepad"] = "Abrir Bloc de Notas",
            ["BuiltinDesc_calc"] = "Abrir Calculadora",
            ["BuiltinDesc_mspaint"] = "Abrir Paint",
            ["BuiltinDesc_explorer"] = "Abrir Explorador de Archivos",
            ["BuiltinDesc_taskmgr"] = "Abrir Administrador de Tareas",
            ["BuiltinDesc_devmgmt"] = "Abrir Administrador de Dispositivos",
            ["BuiltinDesc_services"] = "Abrir Servicios",
            ["BuiltinDesc_regedit"] = "Abrir Editor del Registro",
            ["BuiltinDesc_control"] = "Abrir Panel de Control",
            ["BuiltinDesc_ipconfig"] = "Ver Configuración IP",
            ["BuiltinDesc_ping"] = "Comando Ping",
            ["BuiltinDesc_tracert"] = "Rastrear Ruta",
            ["BuiltinDesc_nslookup"] = "Búsqueda DNS",
            ["BuiltinDesc_netstat"] = "Ver Estado de Red",
            ["BuiltinDesc_lock"] = "Bloquear Computadora",
            ["BuiltinDesc_shutdown"] = "Apagar en 10 segundos",
            ["BuiltinDesc_restart"] = "Reiniciar en 10 segundos",
            ["BuiltinDesc_sleep"] = "Entrar en Modo Suspendido",
            ["BuiltinDesc_emptybin"] = "Vaciar Papelera de Reciclaje",
            ["BuiltinDesc_setting"] = "Abrir Ventana de Ajustes",
            ["BuiltinDesc_exit"] = "Salir de Quanta",
            ["BuiltinDesc_about"] = "Acerca del Programa",
            ["BuiltinDesc_english"] = "Cambiar idioma a Inglés",
            ["BuiltinDesc_chinese"] = "Cambiar idioma a Chino",
            ["BuiltinDesc_spanish"] = "Cambiar idioma a Español",
            ["BuiltinDesc_winrecord"] = "Abrir Grabadora de Windows",
            // ── Nuevas claves de grupo ──────────────────────────────────
            ["GroupSystem"] = "Sistema",
            ["GroupNetwork"] = "Red",
            ["GroupPower"] = "Energía",
            ["GroupQuanta"] = "Quanta",
            ["GroupFeature"] = "Funciones",
            ["GroupQRCode"] = "Código QR",
            // Toast QR Code
            ["QRCodeGenerate"] = "Generar Código QR",
            ["QRCodeCopied"] = "Código QR copiado al portapapeles",
            // Comandos internos record / clip
            ["BuiltinCmd_record"] = "Grabación",
            ["BuiltinDesc_record"] = "Iniciar una nueva grabación",
            ["BuiltinCmd_clip"] = "Historial del portapapeles",
            ["BuiltinDesc_clip"] = "Buscar historial del portapapeles",
            // ── Configuración de Búsqueda de Archivos ─────────────────────────────
            ["MenuFileSearch"] = "Búsqueda de Archivos",
            ["FileSearchSettings"] = "Configuración de Búsqueda de Archivos",
            ["FileSearchEnabled"] = "Habilitar búsqueda de archivos",
            ["FileSearchDirectories"] = "Directorios de búsqueda:",
            ["FileSearchDirectoriesHint"] = "Una ruta por línea, leave vacío para usar predeterminado (Escritorio+Descargas)",
            ["FileSearchMaxFiles"] = "Máx. archivos por directorio:",
            ["FileSearchMaxFilesSuffix"] = " archivos",
            ["FileSearchMaxResults"] = "Máx. resultados:",
            ["FileSearchMaxResultsSuffix"] = " resultados",
            ["FileSearchRecursive"] = "Buscar subdirectorios recursivamente",
            ["FileSearchBrowse"] = "Examinar",
            // ── Lista de Comandos Integrados ───────────────────────────────
            ["MenuBuiltInCommands"] = "Comandos Integrados",
            ["BuiltInCommandsSettings"] = "Lista de Comandos Integrados",
            ["BuiltInCommandGroup"] = "Grupo",
            ["BuiltInCommand"] = "Comando",
            ["BuiltInCommandFunction"] = "Función",
            ["BuiltInCommandUsage"] = "Uso"
        }
    };

    /// <summary>
    /// 当前使用的语言代码，默认为简体中文
    /// </summary>
    private static string _currentLanguage = "zh-CN";

    /// <summary>
    /// 获取或设置当前语言代码。
    /// 设置时会验证语言是否受支持，并自动将选择持久化到配置文件中。
    /// </summary>
    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_translations.ContainsKey(value))
            {
                _currentLanguage = value;
                // 将语言设置保存到配置文件
                var config = ConfigLoader.Load();
                if (config.AppSettings == null) config.AppSettings = new AppSettings();
                config.AppSettings.Language = value;
                ConfigLoader.Save(config);
            }
        }
    }

    /// <summary>
    /// 从应用配置文件中加载语言设置。
    /// 在应用启动时调用，以恢复用户之前选择的语言。
    /// </summary>
    public static void LoadFromConfig()
    {
        var config = ConfigLoader.Load();
        if (config.AppSettings != null && !string.IsNullOrEmpty(config.AppSettings.Language))
        {
            _currentLanguage = config.AppSettings.Language;
        }
    }

    /// <summary>
    /// 根据翻译键名获取当前语言的翻译文本。
    /// 如果当前语言中找不到对应翻译，会回退到中文（zh-CN）。
    /// 如果中文中也找不到，则直接返回键名本身。
    /// </summary>
    /// <param name="key">翻译键名</param>
    /// <returns>对应的翻译文本</returns>
    public static string Get(string key)
    {
        if (_translations.TryGetValue(_currentLanguage, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        // 回退到中文翻译
        if (_translations["zh-CN"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }
        return key;
    }

    /// <summary>
    /// 根据翻译键名获取当前语言的翻译文本，并使用参数进行格式化。
    /// 适用于包含占位符（如 {0}、{1}）的翻译模板。
    /// </summary>
    /// <param name="key">翻译键名</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的翻译文本</returns>
    public static string Get(string key, params object[] args)
    {
        var template = Get(key);
        return string.Format(template, args);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // 动态语言支持 - 统一语言管理
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 获取所有支持的语言列表
    /// </summary>
    public static IReadOnlyList<LanguageInfo> GetSupportedLanguages()
    {
        return LanguageManager.SupportedLanguages;
    }

    /// <summary>
    /// 获取语言显示名称的翻译键名
    /// </summary>
    public static string GetLanguageDisplayKey(string languageCode)
    {
        return languageCode switch
        {
            "zh-CN" => "TrayChinese",
            "en-US" => "TrayEnglish",
            "es-ES" => "TraySpanish",
            _ => "TrayLanguage"
        };
    }

    /// <summary>
    /// 设置语言（带验证）
    /// </summary>
    public static bool TrySetLanguage(string languageCode)
    {
        if (!_translations.ContainsKey(languageCode))
            return false;

        CurrentLanguage = languageCode;
        return true;
    }
}
