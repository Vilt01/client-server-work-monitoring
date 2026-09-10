using System.Net.Http.Json;
using EmployeeMonitor.Client.Models;

namespace EmployeeMonitor.Client.Services;

public class ServerApiClient : IServerApiClient
{
    private readonly HttpClient _http;

    public ServerApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<PingResponse> PingAsync(ClientRegistration registration, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("api/clients/ping", registration, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PingResponse>(cancellationToken: ct)
               ?? new PingResponse();
    }

    public async Task UploadScreenshotAsync(string domain, string machine, byte[] bmp, CancellationToken ct)
    {
        using var content = new ByteArrayContent(bmp);
        content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/bmp");

        var url = $"api/clients/screenshot?domain={Uri.EscapeDataString(domain)}&machine={Uri.EscapeDataString(machine)}";
        var response = await _http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
    }
}