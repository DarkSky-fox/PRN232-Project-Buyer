using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public List<ProductDto> LatestProducts { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public string Message { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("AuthApi");

        // ── Load categories ──
        try
        {
            var catResponse = await client.GetAsync("/api/categories");
            if (catResponse.IsSuccessStatusCode)
            {
                var catJson = await catResponse.Content.ReadAsStringAsync();
                var catResult = JsonSerializer.Deserialize<ApiResponse<List<CategoryDto>>>(
                    catJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (catResult?.Data != null)
                {
                    Categories = catResult.Data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load categories on homepage: {Error}", ex.Message);
        }

        // ── Load latest arrivals ──
        try
        {
            var url = "/api/products?pageSize=8&sortBy=newest";
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<ProductDto>>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Data != null)
                {
                    LatestProducts = result.Data.Items;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load latest arrivals on homepage");
            Message = "Unable to connect to server. Please try again later.";
        }
    }

    private record ApiResponse<T>(bool Success, string Message, T? Data);
}
