namespace EmployeeMonitor.Server.Models;

public class ClientRegistrationDto
{
    public string Domain { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}