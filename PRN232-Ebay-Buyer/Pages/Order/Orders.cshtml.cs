using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages;

public class OrdersModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrdersModel> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OrdersModel(IHttpClientFactory httpClientFactory, ILogger<OrdersModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ─── Bind ──────────────────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)] public int CurrentPage   { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }

    // ─── View Data ─────────────────────────────────────────────────────────────
    public PagedResponse<OrderSummaryResponse>? OrdersData { get; set; }
    public string? ErrorMessage  { get; set; }
    public string? SuccessMessage { get; set; }

    private static readonly Dictionary<string, (string Bg, string Text, string Icon)> StatusStyle = new()
    {
        ["Pending"]    = ("#fff3cd", "#856404", "🕐"),
        ["Processing"] = ("#cce5ff", "#004085", "⚙️"),
        ["Shipped"]    = ("#e2d9f3", "#6f42c1", "🚚"),
        ["Delivered"]  = ("#d4edda", "#155724", "✅"),
        ["Cancelled"]  = ("#f8d7da", "#721c24", "❌"),
    };

    public (string Bg, string Text, string Icon) GetStatusStyle(string? status)
        => status != null && StatusStyle.TryGetValue(status, out var s) ? s : ("#e9ecef", "#495057", "📦");

    // ─── GET ───────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync()
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        

        try
        {
            var queryParams = new List<string> { $"pageNumber={CurrentPage}", "pageSize=5" };
            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
                queryParams.Add($"status={Uri.EscapeDataString(StatusFilter)}");

            var response = await client.GetAsync($"/api/orders?{string.Join("&", queryParams)}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<OrderSummaryResponse>>>(json, JsonOpts);
                OrdersData = result?.Data;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToPage("/Auth/Login");
            }
            else
            {
                ErrorMessage = "Không thể tải danh sách đơn hàng.";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = "Không thể kết nối tới server. Vui lòng thử lại sau.";
            _logger.LogError("API connection error: {Error}", ex.Message);
        }

        return Page();
    }
}
