using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages;

public class ProductDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductDetailModel> _logger;

    public ProductDetailModel(IHttpClientFactory httpClientFactory, ILogger<ProductDetailModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ─── View Data ───────────────────────────────────────────────────────────
    public ProductDetailDto? Product { get; set; }
    public string? ErrorMessage { get; set; }

    // ─── OnGetAsync ──────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (!id.HasValue || id.Value <= 0)
        {
            return RedirectToPage("/Products");
        }

        var client = _httpClientFactory.CreateClient("AuthApi");

        // Forward JWT token if authenticated
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (!string.IsNullOrEmpty(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        try
        {
            var response = await client.GetAsync($"/api/products/{id.Value}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ProductDetailDto>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    Product = result.Data;
                }
                else
                {
                    ErrorMessage = "Product data is empty.";
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorMessage = "Product not found.";
            }
            else
            {
                ErrorMessage = $"API returned status {(int)response.StatusCode}";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = "Cannot connect to API server. Please make sure the API is running.";
            _logger.LogError("API connection error: {Error}", ex.Message);
        }
        catch (Exception ex)
        {
            ErrorMessage = "An unexpected error occurred.";
            _logger.LogError("Error loading product detail: {Error}", ex.Message);
        }

        return Page();
    }
}
