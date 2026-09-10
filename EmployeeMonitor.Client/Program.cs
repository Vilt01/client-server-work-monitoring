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
            IStartupService startup = new StartupService(exePath);
            startup.EnsureRegistered();

            // Временная проверка пинга
            ISystemInfoService systemInfo = new SystemInfoService();
            IServerApiClient api = new ServerApiClient(settings.ServerUrl);

            var me = systemInfo.GetCurrent();
            Logger.Info($"Me: {me.Domain}\\{me.Machine} ip={me.Ip} user={me.User}");

            var response = await api.PingAsync(me, CancellationToken.None);
            Logger.Info($"Ping response: {response.Command}");

            await Task.CompletedTask;
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
        if (!File.Exists(path))
            return new ClientSettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ClientSettings>(
                   json,
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new ClientSettings();
    }
}