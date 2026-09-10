using EmployeeMonitor.Client.Infrastructure;
using EmployeeMonitor.Client.Options;

namespace EmployeeMonitor.Client.Services;

public class MonitoringService : IMonitoringService
{
    private readonly IServerApiClient _api;
    private readonly ISystemInfoService _systemInfo;
    private readonly ClientSettings _settings;

    public MonitoringService(
        IServerApiClient api,
        ISystemInfoService systemInfo,
        ClientSettings settings)
    {
        _api = api;
        _systemInfo = systemInfo;
        _settings = settings;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(_settings.PingIntervalSeconds);

        Logger.Info($"Monitoring loop started. Interval={interval.TotalSeconds}s");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var me = _systemInfo.GetCurrent();
                var response = await _api.PingAsync(me, ct);

                Logger.Info($"Ping OK. Command={response.Command}");

                if (string.Equals(response.Command, "capture", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Capture command received (screenshot not implemented yet).");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.NetworkError($"Ping failed: {ex.Message}");
            }

            try
            {
                await Task.Delay(interval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Logger.Info("Monitoring loop stopped.");
    }
}
