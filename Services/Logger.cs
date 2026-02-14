using System.IO;

namespace Quanta.Services;

public static class Logger
{
    private static readonly string LogDirectory;
    private static readonly string LogFilePath;
    private static readonly object LockObj = new();

    static Logger()
    {
        LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Quanta", "logs");
        Directory.CreateDirectory(LogDirectory);
        LogFilePath = Path.Combine(LogDirectory, $"quanta_{DateTime.Now:yyyyMMdd}.log");
    }

    public static void Log(string message, string level = "INFO")
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            lock (LockObj)
            {
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // Silently fail if logging fails
        }
    }

    public static void Error(string message, Exception? ex = null)
    {
        var msg = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
        Log(msg, "ERROR");
    }

    public static void Warn(string message) => Log(message, "WARN");

    public static void Debug(string message) => Log(message, "DEBUG");
}
