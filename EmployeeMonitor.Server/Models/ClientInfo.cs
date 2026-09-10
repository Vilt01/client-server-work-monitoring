namespace EmployeeMonitor.Server.Models;

public class ClientInfo
{
    // Идентификация
    public string Domain { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;

    // Составной ключ: domain\machine
    public string Key => $"{Domain}\\{Machine}";

    // Активность
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

    // Скриншот
    public bool CaptureRequested { get; set; }
    public DateTime? CaptureRequestedAtUtc { get; set; }
    public byte[]? LastScreenshot { get; set; }
    public DateTime? ScreenshotTakenUtc { get; set; }

    // Статус online/offline (по последней активности)
    public bool IsOnline => (DateTime.UtcNow - LastActivityUtc).TotalSeconds < 30;
}