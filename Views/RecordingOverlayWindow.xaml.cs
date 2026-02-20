// ============================================================================
// 文件名: RecordingOverlayWindow.xaml.cs
// 文件用途: 录音悬浮图标窗口，显示动态 GIF 录制图标，
//          并持续显示暂停/继续/停止控制按钮及录制信息。
//          窗口始终置顶、不抢焦点、可拖动。
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfImage = System.Windows.Controls.Image;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseButtonState = System.Windows.Input.MouseButtonState;
using Quanta.Services;
using GdiImage = System.Drawing.Image;
using GdiPixelFormat = System.Drawing.Imaging.PixelFormat;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace Quanta.Views;

/// <summary>
/// 录音悬浮图标窗口，显示 GIF 动态图标，控制按钮始终可见。
/// </summary>
public partial class RecordingOverlayWindow : Window
{
    // ── Win32 API：禁止窗口激活 ──────────────────────────────────────
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    // ── GIF 动画（System.Drawing 正确合成帧 + WriteableBitmap 零闪烁显示）──
    private const int GifSize = 54;                    // GIF 显示尺寸（像素），与 XAML Width/Height 一致
    private WriteableBitmap? _frameBuffer;             // 唯一的 Image.Source，永不替换
    private List<byte[]>? _gifFramePixels;             // 预渲染的每帧像素数据（Pbgra32）
    private int[] _gifFrameDelays = Array.Empty<int>();// 每帧延迟（毫秒）
    private DispatcherTimer? _gifTimer;
    private int _gifFrameIndex;
    private bool _gifLoaded;

    // ── 录音服务引用 ─────────────────────────────────────────────────
    private readonly RecordingService _recordingService;
    private string _outputDirectory = "";
    private bool _isDiscarding;  // 标记当前是放弃操作，防止 Stopping 状态覆盖文本

    // ── 系统托盘图标 ─────────────────────────────────────────────────
    private NotifyIcon? _notifyIcon;

    // ── 资源路径 ─────────────────────────────────────────────────────
    private static string ResourcePath(string fileName)
        => Path.Combine(AppContext.BaseDirectory, "Resources", "imgs", fileName);

    public RecordingOverlayWindow(RecordingService recordingService, string outputDirectory)
    {
        _recordingService = recordingService;
        _outputDirectory = outputDirectory;

        InitializeComponent();

        _recordingService.ProgressUpdated += OnProgressUpdated;
        _recordingService.StateChanged += OnRecordingStateChanged;
        _recordingService.ErrorOccurred += OnRecordingError;
        _recordingService.RecordingSaved += OnRecordingSaved;

        PositionWindow();

        Logger.Debug("RecordingOverlayWindow: Created, outputDirectory=" + outputDirectory);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置 WS_EX_NOACTIVATE 防止窗口抢焦点
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);

        // 创建 WriteableBitmap 并设置为 Image.Source（整个生命周期只设这一次）
        _frameBuffer = new WriteableBitmap(GifSize, GifSize, 96, 96, PixelFormats.Pbgra32, null);
        RecordingIcon.Source = _frameBuffer;

        // 加载按钮图标
        LoadButtonImages();

        // 启动 GIF 动画
        if (!_gifLoaded)
        {
            Logger.Debug("RecordingOverlayWindow: Window_Loaded loading GIF frames");
            LoadGifFrames(ResourcePath("recording.gif"));
        }
        StartGifTimer();

        // 更新 Tooltip 文本
        PauseTooltip.Content  = LocalizationService.Get("RecordPause");
        ResumeTooltip.Content = LocalizationService.Get("RecordResume");
        StopTooltip.Content   = LocalizationService.Get("RecordStop");
        DropTooltip.Content   = LocalizationService.Get("RecordDrop");
        HideTooltip.Content   = LocalizationService.Get("RecordHide");
        InfoTooltip.Content   = LocalizationService.Get("RecordClickToOpen");

        // 初始状态：录音中，Pause/Stop/Drop 可用，Resume/Hide 启用
        PauseButton.IsEnabled  = true;
        ResumeButton.IsEnabled = false;
        StopButton.IsEnabled   = true;
        DropButton.IsEnabled   = true;
        HideButton.IsEnabled   = true;

        // 初始化系统托盘图标
        InitNotifyIcon();

        Logger.Debug("RecordingOverlayWindow: Loaded");
    }

    private void PositionWindow()
    {
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - Width - 100;
        Top  = (screen.Height - Height) / 2;
    }

    // ── WriteableBitmap 像素操作 ─────────────────────────────────────

    /// <summary>将 pixels（Pbgra32，GifSize×GifSize）原子写入 _frameBuffer，不触发布局。</summary>
    private void WriteFrameToBuffer(byte[] pixels)
    {
        if (_frameBuffer == null) return;
        _frameBuffer.Lock();
        _frameBuffer.WritePixels(new Int32Rect(0, 0, GifSize, GifSize), pixels, GifSize * 4, 0);
        _frameBuffer.Unlock();
    }

    /// <summary>将 _frameBuffer 清为全透明。</summary>
    private void ClearFrameBuffer()
    {
        if (_frameBuffer == null) return;
        var blank = new byte[GifSize * GifSize * 4];
        _frameBuffer.Lock();
        _frameBuffer.WritePixels(new Int32Rect(0, 0, GifSize, GifSize), blank, GifSize * 4, 0);
        _frameBuffer.Unlock();
    }

    // ── GIF 动画 ─────────────────────────────────────────────────────

    /// <summary>
    /// 使用 System.Drawing 正确解码 GIF 每帧（处理 disposal method），
    /// 预渲染为 Pbgra32 像素数组，一次性加载，后续只操作定时器。
    /// </summary>
    private void LoadGifFrames(string gifPath)
    {
        if (_gifLoaded) return;
        if (!File.Exists(gifPath))
        {
            Logger.Warn($"RecordingOverlayWindow: GIF not found: {gifPath}");
            return;
        }

        try
        {
            Logger.Debug($"RecordingOverlayWindow: Loading GIF via System.Drawing: {gifPath}");

            using var gif = GdiImage.FromFile(gifPath);
            var frameDim = new FrameDimension(gif.FrameDimensionsList[0]);
            int frameCount = gif.GetFrameCount(frameDim);
            Logger.Debug($"RecordingOverlayWindow: GIF frames={frameCount}");
            if (frameCount == 0) return;

            // 读取每帧延迟
            _gifFrameDelays = new int[frameCount];
            try
            {
                var delayProp = gif.GetPropertyItem(0x5100); // PropertyTagFrameDelay
                for (int i = 0; i < frameCount; i++)
                {
                    int delay = BitConverter.ToInt32(delayProp.Value!, i * 4) * 10; // 1/100s → ms
                    _gifFrameDelays[i] = Math.Max(delay, 40); // 最低 40ms
                }
            }
            catch
            {
                for (int i = 0; i < frameCount; i++)
                    _gifFrameDelays[i] = 40;
            }

            // 逐帧提取：System.Drawing 的 SelectActiveFrame 会正确合成（处理 disposal）
            var frames = new List<byte[]>(frameCount);
            int stride = GifSize * 4;

            for (int i = 0; i < frameCount; i++)
            {
                gif.SelectActiveFrame(frameDim, i);

                // 绘制到 GifSize×GifSize 的 ARGB Bitmap
                using var bmp = new Bitmap(GifSize, GifSize, GdiPixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.Clear(System.Drawing.Color.Transparent);
                    g.DrawImage(gif, 0, 0, GifSize, GifSize);
                }

                // 锁定位提取像素，转换 ARGB → PBGRA（WPF 预乘 Alpha）
                var lockBits = bmp.LockBits(
                    new Rectangle(0, 0, GifSize, GifSize),
                    ImageLockMode.ReadOnly,
                    GdiPixelFormat.Format32bppArgb);

                var rawPixels = new byte[GifSize * stride];
                System.Runtime.InteropServices.Marshal.Copy(lockBits.Scan0, rawPixels, 0, rawPixels.Length);
                bmp.UnlockBits(lockBits);

                // ARGB → PBGRA 预乘转换
                var pbgra = new byte[rawPixels.Length];
                for (int p = 0; p < rawPixels.Length; p += 4)
                {
                    byte b = rawPixels[p];     // B (GDI BGRA layout in memory)
                    byte g2 = rawPixels[p + 1]; // G
                    byte r = rawPixels[p + 2];  // R
                    byte a = rawPixels[p + 3];  // A

                    if (a == 255)
                    {
                        pbgra[p] = b; pbgra[p + 1] = g2; pbgra[p + 2] = r; pbgra[p + 3] = a;
                    }
                    else if (a == 0)
                    {
                        // 全透明
                    }
                    else
                    {
                        pbgra[p]     = (byte)(b * a / 255);
                        pbgra[p + 1] = (byte)(g2 * a / 255);
                        pbgra[p + 2] = (byte)(r * a / 255);
                        pbgra[p + 3] = a;
                    }
                }

                frames.Add(pbgra);
            }

            _gifFramePixels = frames;
            _gifLoaded = true;
            Logger.Debug($"RecordingOverlayWindow: GIF loaded, {frameCount} frames pre-rendered");
        }
        catch (Exception ex)
        {
            Logger.Warn($"RecordingOverlayWindow: Failed to load GIF: {ex.Message}");
        }
    }

    /// <summary>启动 GIF 动画定时器（帧数据必须已通过 LoadGifFrames 加载）。</summary>
    private void StartGifTimer()
    {
        if (_gifFramePixels == null || _gifFramePixels.Count == 0) return;

        // 已在播放，直接返回
        if (_gifTimer != null && _gifTimer.IsEnabled) return;

        // 显示当前帧
        WriteFrameToBuffer(_gifFramePixels[_gifFrameIndex]);

        if (_gifFramePixels.Count > 1)
        {
            int delayMs = _gifFrameDelays.Length > 0 ? _gifFrameDelays[0] : 40;
            _gifTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(delayMs)
            };
            _gifTimer.Tick += (s, ev) =>
            {
                if (_gifFramePixels == null) return;
                _gifFrameIndex = (_gifFrameIndex + 1) % _gifFramePixels.Count;
                WriteFrameToBuffer(_gifFramePixels[_gifFrameIndex]);
            };
            _gifTimer.Start();
        }
    }

    /// <summary>停止 GIF 动画定时器（保留帧缓存，可快速恢复）。</summary>
    private void StopGifTimer()
    {
        _gifTimer?.Stop();
        _gifTimer = null;
    }

    /// <summary>暂停时：停止 GIF 定时器，将暂停静态图写入 _frameBuffer。</summary>
    private void ShowPauseIcon()
    {
        StopGifTimer();
        var pausePath = ResourcePath("recording-pause.png");
        Logger.Debug($"RecordingOverlayWindow: ShowPauseIcon, exists={File.Exists(pausePath)}");

        if (File.Exists(pausePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource   = new Uri(pausePath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                var converted = new FormatConvertedBitmap(bmp, PixelFormats.Pbgra32, null, 0);
                BitmapSource source = converted;
                if (converted.PixelWidth != GifSize || converted.PixelHeight != GifSize)
                {
                    double sx = (double)GifSize / converted.PixelWidth;
                    double sy = (double)GifSize / converted.PixelHeight;
                    source = new TransformedBitmap(converted, new ScaleTransform(sx, sy));
                }

                var pixels = new byte[GifSize * GifSize * 4];
                source.CopyPixels(pixels, GifSize * 4, 0);
                WriteFrameToBuffer(pixels);
            }
            catch (Exception ex)
            {
                Logger.Warn($"RecordingOverlayWindow: Failed to load pause icon: {ex.Message}");
                ClearFrameBuffer();
            }
        }
        else
        {
            ClearFrameBuffer();
        }
    }

    /// <summary>录音时：恢复 GIF 旋转动画（帧已预加载，只需启动定时器）。</summary>
    private void ShowRecordingIcon()
    {
        Logger.Debug("RecordingOverlayWindow: ShowRecordingIcon");
        StartGifTimer();
    }

    // ── 按钮图标加载 ─────────────────────────────────────────────────

    private void LoadButtonImages()
    {
        Logger.Debug("RecordingOverlayWindow: LoadButtonImages");
        SetButtonImage(PauseButton,  "PauseImage",  "pause.png");
        SetButtonImage(ResumeButton, "ResumeImage", "start.png");
        SetButtonImage(StopButton,   "StopImage",   "stop.png");
        SetButtonImage(DropButton,   "DropImage",   "drop.png");
        SetButtonImage(HideButton,   "HideImage",   "hide.png");
    }

    private static void SetButtonImage(WpfButton button, string imageName, string fileName)
    {
        var path = ResourcePath(fileName);
        if (!File.Exists(path))
        {
            Logger.Warn($"RecordingOverlayWindow: Button image not found: {path}");
            return;
        }

        var bmp = new BitmapImage(new Uri(path, UriKind.Absolute));
        if (bmp.CanFreeze) bmp.Freeze();

        if (button.Template?.FindName(imageName, button) is WpfImage img)
            img.Source = bmp;
        else
            Logger.Warn($"RecordingOverlayWindow: Template image '{imageName}' not found");
    }

    // ── 控制按钮事件 ─────────────────────────────────────────────────

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: PauseButton clicked");
        _recordingService.Pause();
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: ResumeButton clicked");
        _recordingService.Resume();
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: StopButton clicked");
        PauseButton.IsEnabled  = false;
        ResumeButton.IsEnabled = false;
        StopButton.IsEnabled   = false;
        DropButton.IsEnabled   = false;
        InfoTextBlock.Text = LocalizationService.Get("RecordSaving");

        await _recordingService.StopAsync();
    }

    private async void DropButton_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: DropButton clicked");
        _isDiscarding = true;
        PauseButton.IsEnabled  = false;
        ResumeButton.IsEnabled = false;
        StopButton.IsEnabled   = false;
        DropButton.IsEnabled   = false;
        InfoTextBlock.Text = LocalizationService.Get("RecordDropping");

        await _recordingService.DiscardAsync();
    }

    private void InfoBox_Click(object sender, WpfMouseButtonEventArgs e)
    {
        Logger.Debug($"RecordingOverlayWindow: InfoBox clicked, dir={_outputDirectory}");
        if (!string.IsNullOrEmpty(_outputDirectory) && Directory.Exists(_outputDirectory))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName  = "explorer.exe",
                    Arguments = _outputDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Warn($"RecordingOverlayWindow: Open explorer failed: {ex.Message}");
            }
        }
    }

    private void HideButton_Click(object sender, RoutedEventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: HideButton clicked");
        HideToTray();
    }

    // ── 系统托盘功能 ─────────────────────────────────────────────────

    private void InitNotifyIcon()
    {
        var iconPath = ResourcePath("recording.ico");
        if (!File.Exists(iconPath))
        {
            Logger.Warn($"RecordingOverlayWindow: Tray icon not found: {iconPath}");
            return;
        }

        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new Icon(iconPath),
                Text = "录音中...",
                Visible = false
            };
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            Logger.Debug("RecordingOverlayWindow: NotifyIcon initialized");
        }
        catch (Exception ex)
        {
            Logger.Warn($"RecordingOverlayWindow: Failed to create NotifyIcon: {ex.Message}");
        }
    }

    private void HideToTray()
    {
        if (_notifyIcon == null)
        {
            Logger.Warn("RecordingOverlayWindow: NotifyIcon not initialized");
            return;
        }

        Hide();
        _notifyIcon.Visible = true;
        Logger.Debug("RecordingOverlayWindow: Hidden to tray");
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: NotifyIcon double-clicked");
        Show();
        WindowState = WindowState.Normal;
        Activate();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, WpfMouseButtonEventArgs e)
    {
        if (e.LeftButton == WpfMouseButtonState.Pressed)
        {
            try { DragMove(); } catch { }
        }
    }

    // ── 录音服务事件处理 ─────────────────────────────────────────────

    private void OnProgressUpdated(object? sender, RecordingProgressEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var dur = e.Duration;
            InfoTextBlock.Text = $"{(int)dur.TotalMinutes:D2}:{dur.Seconds:D2} | {e.EstimatedCompressedDisplay}";
        });
    }

    private void OnRecordingStateChanged(object? sender, RecordingState state)
    {
        Logger.Debug($"RecordingOverlayWindow: StateChanged → {state}");
        Dispatcher.InvokeAsync(() =>
        {
            switch (state)
            {
                case RecordingState.Recording:
                    ShowRecordingIcon();
                    PauseButton.IsEnabled  = true;
                    ResumeButton.IsEnabled = false;
                    StopButton.IsEnabled   = true;
                    DropButton.IsEnabled   = true;
                    break;

                case RecordingState.Paused:
                    ShowPauseIcon();
                    PauseButton.IsEnabled  = false;
                    ResumeButton.IsEnabled = true;
                    StopButton.IsEnabled   = true;
                    DropButton.IsEnabled   = true;
                    break;

                case RecordingState.Stopping:
                    PauseButton.IsEnabled  = false;
                    ResumeButton.IsEnabled = false;
                    StopButton.IsEnabled   = false;
                    DropButton.IsEnabled   = false;
                    // 放弃操作时不覆盖文本（DropButton_Click 已设为 "RecordDropping"）
                    if (!_isDiscarding)
                        InfoTextBlock.Text = LocalizationService.Get("RecordSaving");
                    break;

                case RecordingState.Idle:
                    Logger.Debug("RecordingOverlayWindow: Idle, closing");
                    Close();
                    break;
            }
        });
    }

    private void OnRecordingError(object? sender, string message)
    {
        Logger.Error($"RecordingOverlayWindow: Error: {message}");
        Dispatcher.InvokeAsync(() =>
        {
            ToastService.Instance.ShowError(message);
            Close();
        });
    }

    private void OnRecordingSaved(object? sender, string filePath)
    {
        Logger.Debug($"RecordingOverlayWindow: RecordingSaved: {filePath}");
        Dispatcher.InvokeAsync(() =>
        {
            _outputDirectory = Path.GetDirectoryName(filePath) ?? _outputDirectory;
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        Logger.Debug("RecordingOverlayWindow: OnClosed");

        _recordingService.ProgressUpdated -= OnProgressUpdated;
        _recordingService.StateChanged    -= OnRecordingStateChanged;
        _recordingService.ErrorOccurred   -= OnRecordingError;
        _recordingService.RecordingSaved  -= OnRecordingSaved;

        StopGifTimer();
        _gifFramePixels = null;

        // 清理托盘图标
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        base.OnClosed(e);
    }
}
