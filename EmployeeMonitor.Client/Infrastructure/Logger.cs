using System.Diagnostics;

namespace EmployeeMonitor.Client.Infrastructure;

public static class Logger
{
    private static readonly object _lock = new();
    private static readonly string _logPath =
        Path.Combine(AppContext.BaseDirectory, "client.log");

    private const long MaxSizeBytes = 1_000_000; // 1 МБ
    private static DateTime _lastNetworkErrorLogged = DateTime.MinValue;

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message) => Write("ERROR", message);

    public static void NetworkError(string message)
    {
        lock (_lock)
        {
            if ((DateTime.UtcNow - _lastNetworkErrorLogged).TotalSeconds < 60)
                return;
            _lastNetworkErrorLogged = DateTime.UtcNow;
        }
        Write("NETERR", message);
    }

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        Debug.WriteLine(line);

        try
        {
            lock (_lock)
            {
                if (File.Exists(_logPath) && new FileInfo(_logPath).Length > MaxSizeBytes)
                    File.Delete(_logPath);

                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // логирование не должно ронять приложение
        }
    }
}