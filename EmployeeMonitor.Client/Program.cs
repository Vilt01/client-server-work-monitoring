using System.Text.Json;
using EmployeeMonitor.Client.Infrastructure;
using EmployeeMonitor.Client.Options;
using EmployeeMonitor.Client.Services;

namespace EmployeeMonitor.Client;

internal static class Program
{
    private static async Task Main()
    {
        // Single instance — не даём запустить второй экземпляр
        using var mutex = new Mutex(true, "Global\\EmployeeMonitorClient", out bool isNew);
        if (!isNew)
        {
            Logger.Info("Another instance is already running. Exiting.");
            return;
        }

        try
        {
            var settings = LoadSettings();
            Logger.Info($"Client starting. ServerUrl={settings.ServerUrl}, PingInterval={settings.PingIntervalSeconds}s");

            // Ручной DI
            var exePath = Environment.ProcessPath ?? "EmployeeMonitor.Client.exe";
            IStartupService startup = new StartupService(exePath);
            ISystemInfoService systemInfo = new SystemInfoService();
            IScreenCaptureService screenCapture = new ScreenCaptureService();
            IServerApiClient api = new ServerApiClient(settings.ServerUrl);
            IMonitoringService monitoring = new MonitoringService(api, systemInfo, screenCapture, settings);

            startup.EnsureRegistered();

            using var cts = new CancellationTokenSource();
            await monitoring.RunAsync(cts.Token);

            await monitoring.RunAsync(cts.Token);
        }
        catch (Exception ex)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "bootstrap.log");
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [FATAL] {ex}\r\n\r\n");
            }
            catch { }
        }
    }

    private static ClientSettings LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "client.settings.json");
        if (!File.Exists(path)) return new ClientSettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ClientSettings>(
                   json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ClientSettings();
    }
}