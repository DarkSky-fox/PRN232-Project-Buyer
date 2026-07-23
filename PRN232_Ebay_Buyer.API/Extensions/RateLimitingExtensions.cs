using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.API.Extensions;

/// <summary>
/// Extension methods that register and configure ASP.NET Core's built-in rate limiter.
/// Extracted from Program.cs to honour the Single-Responsibility principle.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Policy name – kept internal; the GlobalLimiter applies it to every endpoint
    /// automatically, so no controller-level attribute is needed.
    /// </summary>
    internal const string GlobalPolicy = "global-ip";

    /// <summary>
    /// Registers a fixed-window rate limiter that limits each unique client IP to
    /// <c>RateLimiting:PermitLimit</c> requests per <c>RateLimiting:WindowSeconds</c>
    /// seconds.  All values are read from <paramref name="configuration"/> so they
    /// can be tuned without recompilation.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Read config ──────────────────────────────────────────────────────
        var section = configuration.GetSection("RateLimiting");
        int permitLimit   = section.GetValue<int>("PermitLimit",   100);
        int windowSeconds = section.GetValue<int>("WindowSeconds", 60);
        int queueLimit    = section.GetValue<int>("QueueLimit",    0);

        // ── Register ─────────────────────────────────────────────────────────
        services.AddRateLimiter(options =>
        {
            // GlobalLimiter partitions by real client IP and applies to every
            // endpoint unless decorated with [DisableRateLimiting].
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                // UseForwardedHeaders() already ran → RemoteIpAddress is the real client IP.
                var clientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit             = permitLimit,
                        Window                  = TimeSpan.FromSeconds(windowSeconds),
                        QueueProcessingOrder    = QueueProcessingOrder.OldestFirst,
                        QueueLimit              = queueLimit,
                        AutoReplenishment       = true
                    });
            });

            // ── HTTP 429 rejection handler ───────────────────────────────────
            // • Adds Retry-After header (RFC 6585 §4).
            // • Writes JSON body that matches the project-wide ApiResponse<T> record shape.
            // • Logs the blocked IP and path.
            options.OnRejected = async (context, cancellationToken) =>
            {
                // Use ILogger<Program> — static classes cannot be type arguments in C#.
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var path     = context.HttpContext.Request.Path;

                logger.LogWarning(
                    "[RateLimit] 429 Too Many Requests — IP: {ClientIp}, Path: {Path}",
                    clientIp, path);

                // Compute Retry-After from lease metadata; fall back to full window.
                var retryAfter = windowSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterTs))
                    retryAfter = (int)Math.Ceiling(retryAfterTs.TotalSeconds);

                var response = context.HttpContext.Response;
                response.StatusCode  = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/json";

                // Standard Retry-After header so clients / API gateways can back off.
                response.Headers["Retry-After"] = retryAfter.ToString();

                // ApiResponse<T> is a positional record: (bool Success, string Message, T? Data)
                var body = new ApiResponse<object>(
                    false,
                    $"Rate limit exceeded. Too many requests from your IP. " +
                    $"Please wait {retryAfter} second(s) before retrying.",
                    new
                    {
                        retryAfterSeconds = retryAfter,
                        limitPerWindow    = permitLimit,
                        windowSeconds
                    });

                await response.WriteAsJsonAsync(body, cancellationToken);
            };
        });

        return services;
    }
}
