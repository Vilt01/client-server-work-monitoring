using EmployeeMonitor.Client.Infrastructure;
using EmployeeMonitor.Client.Options;

namespace EmployeeMonitor.Client.Services;

public class MonitoringService : IMonitoringService
{
    private readonly IServerApiClient _api;
    private readonly ISystemInfoService _systemInfo;
    private readonly IScreenCaptureService _screenCapture;
    private readonly ClientSettings _settings;

    public MonitoringService(
        IServerApiClient api,
        ISystemInfoService systemInfo,
        IScreenCaptureService screenCapture,
        ClientSettings settings)
    {
        _api = api;
        _systemInfo = systemInfo;
        _screenCapture = screenCapture;
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

                if (string.Equals(response.Command, "capture", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Capture command received. Taking screenshot...");
                    var bmp = _screenCapture.CaptureAsBmp();
                    Logger.Info($"Screenshot captured: {bmp.Length} bytes");

                    await _api.UploadScreenshotAsync(me.Domain, me.Machine, bmp, ct);
                    Logger.Info("Screenshot uploaded.");
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Logger.NetworkError($"Loop error: {ex.Message}"); }

            try { await Task.Delay(interval, ct); }
            catch (OperationCanceledException) { break; }
        }

        Logger.Info("Monitoring loop stopped.");
    }
}