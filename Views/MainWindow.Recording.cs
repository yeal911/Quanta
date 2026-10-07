// ============================================================================
// 文件名：MainWindow.Recording.cs
// 文件用途：录音悬浮窗接线（创建/展示/关闭）与窗口关闭清理。
//          录音业务（状态校验、配置持久化、启停、错误处理）在
//          MainViewModel.Recording.cs，此处只做视图层编排。
// ============================================================================

using System;
using System.ComponentModel;
using Quanta.Core.Interfaces;
using Quanta.Domain.Search;

namespace Quanta.Views;

public partial class MainWindow
{
    /// <summary>
    /// 从搜索结果启动录音（由 SearchEngine 通过 Dispatcher 调用）。
    /// 前置校验与配置持久化在 MainViewModel.PrepareRecording，
    /// 这里只负责悬浮窗的生命周期接线。
    /// </summary>
    public async void StartRecordingFromResult(SearchResult result)
    {
        var request = _viewModel.PrepareRecording(result);
        if (request == null) return;

        try
        {
            _recordingOverlay = new RecordingOverlayWindow(_recordingService, request.OutputDirectory);
            _recordingOverlay.Closed += (s, e) =>
            {
                _recordingOverlay = null;
                if (_viewModel.IsRecordingActive)
                    _ = _viewModel.StopRecordingAsync();
            };

            bool started = await _viewModel.StartRecordingAsync(request);
            if (!started)
            {
                _recordingOverlay?.Close();
                _recordingOverlay = null;
                return;
            }
            _recordingOverlay.Dispatcher.Invoke(() => { });
            _recordingOverlay.Show();
            _recordingOverlay.ShowRecordingUI();
        }
        catch (Exception ex)
        {
            _viewModel.HandleRecordingStartError(ex);
            try { _recordingOverlay?.Close(); } catch { }
            _recordingOverlay = null;
        }
    }

    /// <summary>
    /// 处理录音配置芯片的右键点击，循环切换配置值（业务在 MainViewModel.CycleRecordOption）。
    /// </summary>
    private void RecordChip_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var element = sender as System.Windows.FrameworkElement;
        if (element == null) return;

        var result = element.DataContext as SearchResult;
        if (result == null) return;

        var tag = element.Tag?.ToString() ?? "";
        _viewModel.CycleRecordOption(result, tag);
        e.Handled = true;
    }

    /// <summary>
    /// 窗口关闭前校验：录音进行中时提示并阻止关闭（校验在 MainViewModel.ConfirmCanExit）。
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_viewModel.ConfirmCanExit())
        {
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    /// <summary>
    /// 窗口关闭后清理：停止剪贴板监听、注销热键、停止录音并关闭悬浮窗。
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _clipboardMonitor.Stop();
        _hotkeyManager.Dispose();
        if (_viewModel.IsRecordingActive)
        {
            _ = _viewModel.StopRecordingAsync();
        }
        _recordingOverlay?.Close();
        base.OnClosed(e);
    }
}
