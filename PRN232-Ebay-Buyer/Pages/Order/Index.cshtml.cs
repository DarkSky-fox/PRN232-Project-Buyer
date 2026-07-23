using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PRN232_Ebay_Buyer.Pages.Order;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public PagedResponse<OrderSummaryResponse>? Orders { get; set; }
    public string CurrentStatus { get; set; } = "";

    public async Task<IActionResult> OnGetAsync([FromQuery] string? status, [FromQuery] int pageNumber = 1)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login");
        }

        CurrentStatus = status ?? "";

        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var url = $"/api/orders?pageNumber={pageNumber}&pageSize=10";
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            url += $"&status={status}";
        }

        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<PagedResponse<OrderSummaryResponse>>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Data != null)
            {
                Orders = result.Data;
            }
        }

        return Page();
    }
}
