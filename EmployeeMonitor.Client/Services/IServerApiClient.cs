using EmployeeMonitor.Client.Models;

namespace EmployeeMonitor.Client.Services;

public interface IServerApiClient
{
    Task<PingResponse> PingAsync(ClientRegistration registration, CancellationToken ct);
    Task UploadScreenshotAsync(string domain, string machine, byte[] bmp, CancellationToken ct);
}