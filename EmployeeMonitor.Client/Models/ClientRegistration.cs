namespace EmployeeMonitor.Client.Models;

public class ClientRegistration
{
    public string Domain { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}