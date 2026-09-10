namespace EmployeeMonitor.Client.Services;

public interface IMonitoringService
{
    Task RunAsync(CancellationToken ct);
}