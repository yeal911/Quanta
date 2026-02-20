// ============================================================================
// 文件名: LocalizationService.cs
// 文件描述: 本地化（国际化）服务，提供多语言文本翻译功能。
//           目前支持简体中文（zh-CN）和英文（en-US）两种语言。
//           语言设置会自动持久化到应用配置文件中。
// ============================================================================

using System.Collections.Generic;
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
            ["TestCommand"] = "测试",
            ["TestExecuted"] = "命令已执行",
            ["TestFailed"] = "命令执行失败",
            ["GroupCommand"] = "命令",
            ["GroupApp"] = "应用",
            ["GroupFile"] = "文件",
            ["GroupWindow"] = "窗口",
            ["GroupCalc"] = "计算",
            ["GroupWeb"] = "网络",
            ["GroupText"] = "文本工具",
            ["GroupClip"] = "剪贴板历史",
            ["QRCodeTooLong"] = "文本超过2000字符，无法生成二维码",
            ["SettingsTitle"] = "设置",
            ["GeneralSettings"] = "通用设置",
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
            ["RecordBrowse"] = "浏览",
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
            ["RecordRightClickChannels"] = "右键点击切换声道"
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
            ["TestCommand"] = "Test",
            ["TestExecuted"] = "Command executed",
            ["TestFailed"] = "Command execution failed",
            ["GroupCommand"] = "Commands",
            ["GroupApp"] = "Apps",
            ["GroupFile"] = "Files",
            ["GroupWindow"] = "Windows",
            ["GroupCalc"] = "Calculator",
            ["GroupWeb"] = "Web",
            ["GroupText"] = "Text Tools",
            ["GroupClip"] = "Clipboard History",
            ["QRCodeTooLong"] = "Text exceeds 2000 characters, cannot generate QR code",
            ["SettingsTitle"] = "Settings",
            ["GeneralSettings"] = "General",
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
            ["RecordBrowse"] = "Browse",
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
            ["RecordRightClickChannels"] = "Right-click to switch channels"
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
}
