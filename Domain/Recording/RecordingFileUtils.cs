// ============================================================================
// 文件名: RecordingFileUtils.cs
// 文件用途: 录音相关的文件系统工具函数（无状态，线程安全）。
//          提供写权限检查、磁盘空间检查、安全删除等功能。
// ============================================================================

using System.IO;
using Quanta.Helpers;

namespace Quanta.Services;

internal static class RecordingFileUtils
{
    /// <summary>检查目录是否有写入权限（通过实际写入临时文件测试）。</summary>
    internal static bool CheckWritePermission(string directory)
    {
        try
        {
            var test = Path.Combine(directory, $".quanta_{Guid.NewGuid():N}");
            File.WriteAllText(test, "x");
            File.Delete(test);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"RecordingFileUtils.CheckWritePermission: {ex.Message}");
            return false;
        }
    }

    /// <summary>检查目录所在磁盘剩余空间是否大于 100MB。</summary>
    internal static bool CheckDiskSpace(string directory)
    {
        try
        {
            var root = Path.GetPathRoot(directory);
            if (string.IsNullOrEmpty(root)) return true;
            var drive = new DriveInfo(root);
            bool ok = drive.AvailableFreeSpace > 100L * 1024 * 1024;
            Logger.Debug($"RecordingFileUtils.CheckDiskSpace: free={drive.AvailableFreeSpace / (1024 * 1024)}MB, ok={ok}");
            return ok;
        }
        catch (Exception ex) { Logger.Warn($"RecordingFileUtils.CheckDiskSpace: {ex.Message}"); return true; }
    }

    /// <summary>安全删除文件，文件不存在或删除失败时仅记录日志，不抛异常。</summary>
    internal static void TryDeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        try { File.Delete(path); Logger.Debug($"RecordingFileUtils: deleted {path}"); }
        catch (Exception ex) { Logger.Warn($"RecordingFileUtils: delete failed {path}: {ex.Message}"); }
    }
}
