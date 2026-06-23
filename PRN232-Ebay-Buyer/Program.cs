using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Data Protection (dùng cho mã hóa/giải mã cookie) ─────────────────────
builder.Services.AddDataProtection();

// ── 2. Razor Pages ─────────────────────────────────────────────────────────
builder.Services.AddRazorPages();

// ── 2. HttpClient (singleton, base address trỏ tới Backend API) ────────────
builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration.GetValue<string>("ApiBaseUrl")
        ?? "http://localhost:5001");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── 3. Memory Cache (lưu trạng thái tạm thời phía client nếu cần) ─────────
builder.Services.AddMemoryCache();

// ── 4. Cookie Authentication (giữ session JWT trên trình duyệt) ───────────
builder.Services.AddAuthentication(
    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EbayAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ── 5. HTTP pipeline ────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
