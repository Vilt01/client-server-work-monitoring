using EmployeeMonitor.Server.Models;

namespace EmployeeMonitor.Server.Application;

public interface IClientRegistry
{
    IReadOnlyCollection<ClientInfo> GetAll();
    ClientInfo RegisterOrUpdate(ClientRegistrationDto dto);
    bool TryGet(string domain, string machine, out ClientInfo? info);
    void RequestScreenshot(string domain, string machine);
    void AttachScreenshot(string domain, string machine, byte[] data);
}