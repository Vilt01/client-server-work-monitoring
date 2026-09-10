using EmployeeMonitor.Client.Models;

namespace EmployeeMonitor.Client.Services;

public interface ISystemInfoService
{
    ClientRegistration GetCurrent();
}