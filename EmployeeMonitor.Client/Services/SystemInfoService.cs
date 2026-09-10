using System.Net;
using System.Net.Sockets;
using EmployeeMonitor.Client.Models;

namespace EmployeeMonitor.Client.Services;

public class SystemInfoService : ISystemInfoService
{
    public ClientRegistration GetCurrent()
    {
        return new ClientRegistration
        {
            Domain = Environment.UserDomainName,
            Machine = Environment.MachineName,
            User = Environment.UserName,
            Ip = GetLocalIp()
        };
    }

    private static string GetLocalIp()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return ip?.ToString() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}