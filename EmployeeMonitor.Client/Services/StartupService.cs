using EmployeeMonitor.Client.Infrastructure;
using Microsoft.Win32;

namespace EmployeeMonitor.Client.Services;

public class StartupService : IStartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "EmployeeMonitorClient";
    private readonly string _exePath;

    public StartupService(string exePath)
    {
        _exePath = exePath;
    }

    public void EnsureRegistered()
    {
        if (_exePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Info("Skip autostart registration: running via dotnet.exe.");
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
            {
                Logger.Error($"Cannot open registry key: HKCU\\{RunKey}");
                return;
            }

            var current = key.GetValue(ValueName) as string;
            var desired = $"\"{_exePath}\"";

            if (current == desired)
            {
                Logger.Info("Autostart already registered.");
                return;
            }

            key.SetValue(ValueName, desired);
            Logger.Info($"Autostart registered: {desired}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Autostart registration failed: {ex.Message}");
        }
    }
}