using System.Collections.Concurrent;
using EmployeeMonitor.Server.Models;

namespace EmployeeMonitor.Server.Application;

public class ClientRegistry : IClientRegistry
{
    private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

    public IReadOnlyCollection<ClientInfo> GetAll() => _clients.Values.ToList();

    public ClientInfo RegisterOrUpdate(ClientRegistrationDto dto)
    {
        var key = $"{dto.Domain}\\{dto.Machine}";

        return _clients.AddOrUpdate(key,
            _ => new ClientInfo
            {
                Domain = dto.Domain,
                Machine = dto.Machine,
                Ip = dto.Ip,
                User = dto.User,
                LastActivityUtc = DateTime.UtcNow
            },
            (_, existing) =>
            {
                existing.Domain = dto.Domain;
                existing.Ip = dto.Ip;
                existing.User = dto.User;
                existing.LastActivityUtc = DateTime.UtcNow;
                return existing;
            });
    }

    public bool TryGet(string domain, string machine, out ClientInfo? info)
    {
        return _clients.TryGetValue($"{domain}\\{machine}", out info);
    }

    public void RequestScreenshot(string domain, string machine)
    {
        if (_clients.TryGetValue($"{domain}\\{machine}", out var info))
        {
            info.CaptureRequested = true;
            info.CaptureRequestedAtUtc = DateTime.UtcNow;
        }
    }

    public void AttachScreenshot(string domain, string machine, byte[] data)
    {
        if (_clients.TryGetValue($"{domain}\\{machine}", out var info))
        {
            info.LastScreenshot = data;
            info.ScreenshotTakenUtc = DateTime.UtcNow;
            info.LastActivityUtc = DateTime.UtcNow;
        }
    }
}