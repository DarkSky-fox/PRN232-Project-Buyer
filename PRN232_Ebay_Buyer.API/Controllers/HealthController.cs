using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;

namespace PRN232_Ebay_Buyer.API.Controllers;

/// <summary>
/// Health check endpoint dùng cho Nginx passive health check và monitoring.
/// Exempt from rate limiting — health probes must never be blocked.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[DisableRateLimiting]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IConfiguration config, ILogger<HealthController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Liveness check – xác nhận instance đang chạy.
    /// Nginx dùng endpoint này để passive health check.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "local";
        var machineName = Environment.MachineName;
        var port = HttpContext.Connection.LocalPort;

        return Ok(new
        {
            status    = "healthy",
            instance  = instanceId,
            host      = machineName,
            port      = port,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Readiness check – kiểm tra kết nối Database.
    /// Trả về 503 nếu DB không sẵn sàng.
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "local";
        var connString = _config.GetConnectionString("DefaultConnection");

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();

            return Ok(new
            {
                status   = "ready",
                instance = instanceId,
                database = "connected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HealthCheck] Database connection failed on instance {Instance}", instanceId);
            return StatusCode(503, new
            {
                status   = "unavailable",
                instance = instanceId,
                database = "disconnected",
                error    = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
