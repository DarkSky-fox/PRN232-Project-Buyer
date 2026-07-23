using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PRN232_Ebay_Buyer.API.Extensions;
using PRN232_Ebay_Buyer.API.Models;
using PRN232_Ebay_Buyer.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 0. Instance ID (dùng để phân biệt instance trong load balancing) ─────────
var instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID") ?? "local-1";
Console.WriteLine($"[Startup] Instance ID: {instanceId}, listening on: {builder.Configuration["ASPNETCORE_URLS"] ?? "default"}");

// ── 0b. ForwardedHeaders (xử lý X-Forwarded-For, X-Forwarded-Proto từ Nginx) ─
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Cho phép Nginx (trong Docker network) forward headers
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── 1. DbContext ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<CloneEbayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. Services ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IJwtService, JwtService>();

// ── 3. Memory Cache (dùng cho verification token) ──────────────────────────
builder.Services.AddMemoryCache();

// ── 3b. Rate Limiting (100 req / 60 s per client IP, HTTP 429 on breach) ───
builder.Services.AddApiRateLimiting(builder.Configuration);

// ── 3. JWT Authentication / Authorization ───────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Auth Failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            var token = context.Token;
            logger.LogWarning("JWT OnMessageReceived: token present={HasToken}, token prefix={Prefix}",
                !string.IsNullOrEmpty(token),
                !string.IsNullOrEmpty(token) && token.Length > 20 ? token.Substring(0, 20) + "..." : token ?? "");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── 4. Controllers & JSON options ───────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });

// ── 5. CORS (cho phép Frontend gọi API qua Nginx và trực tiếp) ──────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Hỗ trợ cả URL trực tiếp lẫn qua Nginx
        var frontendUrls = builder.Configuration
            .GetSection("AllowedOrigins").Get<string[]>()
            ?? [builder.Configuration.GetValue<string>("FrontendUrl") ?? "http://localhost:5000"];

        policy.WithOrigins(frontendUrls)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ── 6. HTTP pipeline ─────────────────────────────────────────────────────────
// QUAN TRỌNG: UseForwardedHeaders phải gọi trước các middleware khác
// để X-Forwarded-For được phân giải thành RemoteIpAddress trước khi
// rate limiter partition by IP.
app.UseForwardedHeaders();

app.UseCors("AllowFrontend");

// ── Rate Limiter ─────────────────────────────────────────────────────────────
// Đặt SAU ForwardedHeaders + CORS (IP đã được resolve)
// và TRƯỚC Authentication (tránh chi phí xác thực JWT cho các request bị chặn).
app.UseRateLimiter();

// Thêm Instance ID vào mọi response (dùng để debug load balancing)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Instance-Id"] = instanceId;
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
