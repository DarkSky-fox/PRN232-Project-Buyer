using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages;

public class ProductsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductsModel> _logger;

    public ProductsModel(IHttpClientFactory httpClientFactory, ILogger<ProductsModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ─── Bind Properties ─────────────────────────────────────────────────────
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 12;

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    // ─── View Data ───────────────────────────────────────────────────────────
    public List<ProductDto> Products { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }

    // ─── OnGetAsync ──────────────────────────────────────────────────────────
    public async Task OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient("AuthApi");

        // Forward JWT token if user is authenticated
        var token = HttpContext.Request.Cookies["JwtToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

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
            _logger.LogWarning("Failed to load categories: {Error}", ex.Message);
        }

        // ── Load products ──
        try
        {
            var queryParams = new List<string>
            {
                $"page={CurrentPage}",
                $"pageSize={PageSize}"
            };

            if (CategoryId.HasValue && CategoryId.Value > 0)
                queryParams.Add($"categoryId={CategoryId.Value}");
            if (!string.IsNullOrWhiteSpace(Search))
                queryParams.Add($"search={Uri.EscapeDataString(Search)}");
            if (MinPrice.HasValue)
                queryParams.Add($"minPrice={MinPrice.Value}");
            if (MaxPrice.HasValue)
                queryParams.Add($"maxPrice={MaxPrice.Value}");
            if (!string.IsNullOrWhiteSpace(SortBy))
                queryParams.Add($"sortBy={SortBy}");

            var url = $"/api/products?{string.Join("&", queryParams)}";

            _logger.LogInformation("Calling API: {Url}", url);

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<ProductDto>>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    Products = result.Data.Items;
                    TotalItems = result.Data.TotalItems;
                    TotalPages = result.Data.TotalPages;
                    CurrentPage = result.Data.Page;
                }
            }
            else
            {
                ErrorMessage = $"API returned status {(int)response.StatusCode}";
                _logger.LogWarning("GetProducts API failed: {Status}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = "Cannot connect to API server. Please make sure the API is running.";
            _logger.LogError("API connection error: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "An unexpected error occurred while loading products.";
            _logger.LogError("Unexpected error: {Error}", ex.Message);
        }
    }
}
