using System.Text.Json;
using EmployeeMonitor.Client.Infrastructure;
using EmployeeMonitor.Client.Options;
using EmployeeMonitor.Client.Services;

namespace EmployeeMonitor.Client;

internal static class Program
{
    private static async Task Main()
    {
        try
        {
            var settings = LoadSettings();
            Logger.Info($"Client starting. ServerUrl={settings.ServerUrl}, PingInterval={settings.PingIntervalSeconds}s");

            var exePath = Environment.ProcessPath ?? "EmployeeMonitor.Client.exe";
            Logger.Info($"ProcessPath={exePath}");
            Logger.Info($"BaseDirectory={AppContext.BaseDirectory}");

            IStartupService startup = new StartupService(exePath);
            startup.EnsureRegistered();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Если Logger не сработал — пишем напрямую в bootstrap.log
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "bootstrap.log");
                File.AppendAllText(path,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [FATAL] {ex}\r\n\r\n");
            }
            catch { /* совсем всё плохо — молча выходим */ }
        }
    }

    private static ClientSettings LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "client.settings.json");
        if (!File.Exists(path))
            return new ClientSettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ClientSettings>(
                   json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ClientSettings();
    }
}