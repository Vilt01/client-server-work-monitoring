namespace EmployeeMonitor.Client.Options;

public class ClientSettings
{
    public string ServerUrl { get; set; } = "http://localhost:8080";
    public int PingIntervalSeconds { get; set; } = 5;
}