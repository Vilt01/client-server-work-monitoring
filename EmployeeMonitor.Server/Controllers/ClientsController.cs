using EmployeeMonitor.Server.Application;
using EmployeeMonitor.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeMonitor.Server.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly IClientRegistry _registry;

    public ClientsController(IClientRegistry registry)
    {
        _registry = registry;
    }

    // POST /api/clients/ping
    // Клиент шлёт свою регистрацию, получает команду
    [HttpPost("ping")]
    public ActionResult<PingResponseDto> Ping([FromBody] ClientRegistrationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Domain) || string.IsNullOrWhiteSpace(dto.Machine))
            return BadRequest("Domain and Machine are required.");

        var info = _registry.RegisterOrUpdate(dto);

        var response = new PingResponseDto
        {
            Command = info.CaptureRequested ? "capture" : "none"
        };

        // Сбрасываем флаг — клиент получил команду
        if (info.CaptureRequested)
            info.CaptureRequested = false;

        return Ok(response);
    }

    // GET /api/clients
    // Список всех клиентов
    [HttpGet]
    public IActionResult GetAll()
    {
        var list = _registry.GetAll().Select(c => new
        {
            c.Domain,
            c.Machine,
            c.Ip,
            c.User,
            LastActivityUtc = c.LastActivityUtc.ToString("o"),
            IsOnline = c.IsOnline,
            HasScreenshot = c.LastScreenshot != null,
            ScreenshotTakenUtc = c.ScreenshotTakenUtc?.ToString("o")
        });

        return Ok(list);
    }

    // POST /api/clients/{domain}/{machine}/screenshot/request
    // Поставить флаг захвата
    [HttpPost("{domain}/{machine}/screenshot/request")]
    public IActionResult RequestScreenshot(string domain, string machine)
    {
        if (!_registry.TryGet(domain, machine, out _))
            return NotFound("Client not found.");

        _registry.RequestScreenshot(domain, machine);
        return Ok();
    }

    // POST /api/clients/screenshot?domain=&machine=
    // Приём BMP от клиента
    [HttpPost("screenshot")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadScreenshot(
        [FromQuery] string domain,
        [FromQuery] string machine)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(machine))
            return BadRequest("Domain and Machine are required.");

        if (!_registry.TryGet(domain, machine, out _))
            return NotFound("Client not found.");

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var data = ms.ToArray();

        if (data.Length == 0)
            return BadRequest("Empty body.");

        _registry.AttachScreenshot(domain, machine, data);
        return Ok();
    }

    // GET /api/clients/{domain}/{machine}/screenshot
    // Отдача BMP
    [HttpGet("{domain}/{machine}/screenshot")]
    public IActionResult GetScreenshot(string domain, string machine)
    {
        if (!_registry.TryGet(domain, machine, out var info) || info?.LastScreenshot == null)
            return NotFound();

        return File(info.LastScreenshot, "image/bmp");
    }
}