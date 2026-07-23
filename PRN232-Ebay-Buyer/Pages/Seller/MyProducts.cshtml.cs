using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages.Seller;

public class MyProductsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MyProductsModel> _logger;

    public MyProductsModel(IHttpClientFactory httpClientFactory, ILogger<MyProductsModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public List<ProductDto> Products { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Auth/Login");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Auth/Login");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");

            // Forward JWT Token
            var token = HttpContext.Request.Cookies["BearerToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Gọi API lọc theo sellerId, đặt pageSize lớn để lấy hết hoặc phân trang (đặt tạm 50)
            var url = $"/api/products?sellerId={userId}&pageSize=50";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<ProductDto>>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    Products = result.Data.Items;
                    IsSuccess = true;
                }
            }
            else
            {
                Message = $"Failed to fetch products. API returned status: {response.StatusCode}";
                IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load seller's products");
            Message = "Unable to connect to server. Please try again later.";
            IsSuccess = false;
        }

        return Page();
    }

    private record ApiResponse<T>(bool Success, string Message, T? Data);
}
