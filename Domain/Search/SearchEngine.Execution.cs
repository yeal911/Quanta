// ============================================================================
// 文件名：SearchEngine.Execution.cs
// 文件用途：SearchEngine 执行与评分相关职责拆分（ResultExecutor + Ranker 入口）。
// ============================================================================

using Quanta.Helpers;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Quanta.Models;

namespace Quanta.Services;

public partial class SearchEngine
{
    /// <summary>
    /// 计算模糊匹配分数
    /// 用于评估查询字符串与目标字符串的相似程度。
    /// 匹配逻辑：完全包含(1.0) > 前缀匹配(0.9) > 逐字符顺序匹配(按匹配比例 * 0.7 计算)
    /// </summary>
    /// <param name="query">用户输入的搜索关键词</param>
    /// <param name="target">待匹配的目标字符串（如文件名、应用名称等）</param>
    /// <returns>匹配分数，范围 0.0 ~ 1.0，分数越高表示匹配度越好</returns>
    public static double CalculateFuzzyScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(target))
            return 0;

        query = query.ToLower();
        target = target.ToLower();

        // 完全包含匹配，得分最高
        if (target.Contains(query)) return 1.0;

        // 前缀匹配
        if (target.StartsWith(query)) return 0.9;

        // 逐字符顺序模糊匹配：按顺序在目标中查找查询的每个字符
        int matchedChars = 0;
        int targetIndex = 0;
        foreach (char c in query)
        {
            int foundIndex = target.IndexOf(c, targetIndex);
            if (foundIndex >= 0)
            {
                matchedChars++;
                targetIndex = foundIndex + 1;
            }
        }

        // 按匹配字符占比计算分数，乘以 0.7 作为模糊匹配的权重折扣
        return matchedChars > 0 ? (double)matchedChars / query.Length * 0.7 : 0;
    }

    /// <summary>
    /// 执行搜索结果对应的操作
    /// 根据结果类型分派到不同的执行逻辑：文件启动、自定义命令执行等。
    /// </summary>
    public async Task<bool> ExecuteResultAsync(SearchResult result, string param = "")
    {
        switch (result.Type)
        {
            case SearchResultType.Application:
            case SearchResultType.File:
            case SearchResultType.RecentFile:
                return await LaunchFileAsync(result);

            case SearchResultType.Window:
                // 激活（切换到）对应的系统窗口
                return _windowManager.ActivateWindow(result);

            case SearchResultType.Calculator:
                // 将计算结果复制到剪贴板
                var calcOutput = "";
                if (result.Data is CommandResult cr && cr.Success)
                    calcOutput = cr.Output;
                else if (!string.IsNullOrEmpty(result.Subtitle))
                    calcOutput = result.Subtitle;

                if (!string.IsNullOrEmpty(calcOutput))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        System.Windows.Clipboard.SetText(calcOutput));
                    ToastService.Instance.ShowSuccess(LocalizationService.Get("CopiedToClipboard"));
                }
                return true;

            case SearchResultType.Command:
            case SearchResultType.WebSearch:
                return true;

            case SearchResultType.CustomCommand:
                return await ExecuteCustomCommandAsync(result, param);

            case SearchResultType.QRCode:
                // 将二维码图片复制到剪贴板
                if (result.QRCodeImage != null)
                {
                    try
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // 将 BitmapImage 转换为 BitmapSource 并复制到剪贴板
                            System.Windows.Clipboard.SetImage(result.QRCodeImage);
                        });
                        ToastService.Instance.ShowSuccess(LocalizationService.Get("QRCodeCopied"));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to copy QRCode to clipboard: {ex.Message}");
                        ToastService.Instance.ShowError("复制失败");
                    }
                }
                return true;

            case SearchResultType.SystemAction:
                return ExecuteSystemAction(result.Path);

            case SearchResultType.RecordCommand:
                // RecordCommand 的执行由 MainWindow 直接处理（需要 UI 层配合）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.MainWindow is Views.MainWindow mw)
                        mw.StartRecordingFromResult(result);
                });
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 构建录音命令的搜索结果，加载当前录音配置
    /// </summary>
    private static SearchResult BuildRecordCommandResult(string filePrefix)
    {
        var config = ConfigLoader.Load();
        var recSettings = config.RecordingSettings ?? new Models.RecordingSettings();

        var outputDir = string.IsNullOrEmpty(recSettings.OutputPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : recSettings.OutputPath;

        var recordData = new Models.RecordCommandData
        {
            FilePrefix = filePrefix,
            Source = recSettings.Source,
            Format = recSettings.Format,
            SampleRate = recSettings.SampleRate,
            Bitrate = recSettings.Bitrate,
            Channels = recSettings.Channels,
            OutputPath = recSettings.OutputPath
        };

        return new SearchResult
        {
            Index = 1,
            Id = "cmd:record",
            Title = string.IsNullOrEmpty(filePrefix) ? "record" : $"record {filePrefix}",
            Subtitle = LocalizationService.Get("RecordCommandDesc"),
            Path = "record",
            IconText = "🎙",
            Type = SearchResultType.RecordCommand,
            MatchScore = 1.0,
            GroupLabel = LocalizationService.Get("GroupFeature"),
            GroupOrder = GetGroupOrder("GroupFeature"),
            QueryMatch = "record",
            RecordData = recordData
        };
    }

    /// <summary>
    /// 执行系统操作（设置、关于、切换语言）
    /// </summary>
    private bool ExecuteSystemAction(string action)
    {
        var app = System.Windows.Application.Current;
        var mainWindow = app.MainWindow;

        // 先检查是否是语言切换关键字
        var langCode = SearchEngineHelper.GetLanguageCodeFromKeyword(action ?? "");
        if (!string.IsNullOrEmpty(langCode))
        {
            LocalizationService.CurrentLanguage = langCode;
            app.Dispatcher.Invoke(() =>
            {
                if (mainWindow is Views.MainWindow mw)
                {
                    var config = Helpers.ConfigLoader.Load();
                    mw.RefreshLocalization();
                    mw.ApplyTheme(config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false);
                }
            });
            ToastService.Instance.ShowSuccess(LocalizationService.Get("LanguageChanged"));
            return true;
        }

        switch (action?.ToLower())
        {
            case "setting":
                // 打开设置窗口
                app.Dispatcher.Invoke(() =>
                {
                    var settingsWin = new Views.CommandSettingsWindow(this) { Owner = mainWindow };
                    // 获取当前主题状态
                    var config = ConfigLoader.Load();
                    bool isDark = config.Theme?.Equals("Dark", StringComparison.OrdinalIgnoreCase) ?? false;
                    settingsWin.SetDarkTheme(isDark);
                    settingsWin.ShowDialog();
                });
                return true;

            case "about":
                // 显示关于信息（使用 Toast）
                app.Dispatcher.Invoke(() =>
                {
                    ToastService.Instance.ShowInfo($"{LocalizationService.Get("Author")}: yeal911\n{LocalizationService.Get("Email")}: yeal91117@gmail.com", 3.0);
                });
                return true;

            case "exit":
                // 退出程序
                app.Dispatcher.Invoke(() =>
                {
                    app.Shutdown();
                });
                return true;

            case "winrecord":
                // 打开 Windows 内置录音机
                Logger.Debug("[winrecord] 开始尝试打开 Windows 录音机...");

                bool started = false;

                // 方法1: 使用 explorer.exe 打开 AppsFolder 中的录音机
                // 这是最可靠的方法，兼容性最好
                try
                {
                    Logger.Debug("[winrecord] 方法1: explorer.exe shell:AppsFolder...");
                    var psi1 = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = "shell:AppsFolder\\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe!App",
                        UseShellExecute = false
                    };
                    System.Diagnostics.Process.Start(psi1);
                    Logger.Debug("[winrecord] 方法1 启动成功!");
                    started = true;
                }
                catch (Exception ex)
                {
                    Logger.Debug($"[winrecord] 方法1 失败: {ex.Message}");
                }

                // 方法2: 尝试 ms-voicesRecorder: URI
                if (!started)
                {
                    try
                    {
                        Logger.Debug("[winrecord] 方法2: ms-voicesRecorder:");
                        var psi2 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ms-voicesRecorder:",
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi2);
                        Logger.Debug("[winrecord] 方法2 启动成功!");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"[winrecord] 方法2 失败: {ex.Message}");
                    }
                }

                // 方法3: 尝试 ms-soundrecorder: URI
                if (!started)
                {
                    try
                    {
                        Logger.Debug("[winrecord] 方法3: ms-soundrecorder:");
                        var psi3 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ms-soundrecorder:",
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi3);
                        Logger.Debug("[winrecord] 方法3 启动成功!");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"[winrecord] 方法3 失败: {ex.Message}");
                    }
                }

                // 方法4: 通过 cmd start 命令
                if (!started)
                {
                    try
                    {
                        Logger.Debug("[winrecord] 方法4: cmd /c start shell:AppsFolder...");
                        var psi4 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c start shell:AppsFolder\\Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe!App",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        System.Diagnostics.Process.Start(psi4);
                        Logger.Debug("[winrecord] 方法4 启动成功!");
                        started = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"[winrecord] 方法4 失败: {ex.Message}");
                    }
                }

                if (!started)
                {
                    Logger.Debug("[winrecord] 所有方法都失败，显示提示");
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        ToastService.Instance.ShowWarning("Windows 录音机未安装，请从 Microsoft Store 搜索「录音机」下载"));
                }
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// 执行自定义命令
    /// 根据命令类型（url/program/directory/shell/calculator）执行不同的操作逻辑。
    /// 支持参数占位符替换（{param}、{query}、{%p}），支持管理员权限运行和隐藏窗口模式。
    /// </summary>
    /// <param name="result">包含命令配置的搜索结果</param>
    /// <param name="param">用户传入的参数，用于替换命令路径和参数中的占位符</param>
    /// <returns>命令执行是否成功</returns>

}
