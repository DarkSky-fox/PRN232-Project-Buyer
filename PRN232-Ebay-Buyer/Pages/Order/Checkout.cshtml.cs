using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PRN232_Ebay_Buyer.Pages.Order;

public class CheckoutModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CheckoutModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public List<AddressDto> Addresses { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login");
        }

        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Order/addresses");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<List<AddressDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result?.Data != null)
            {
                Addresses = result.Data;
            }
        }

        return Page();
    }
}
