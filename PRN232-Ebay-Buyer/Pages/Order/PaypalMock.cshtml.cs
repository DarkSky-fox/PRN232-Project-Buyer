using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PRN232_Ebay_Buyer.Pages.Order;

public class PaypalMockModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PaypalMockModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty(SupportsGet = true)]
    public int OrderId { get; set; }

    public PaypalCheckoutDetailsDto? Details { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (OrderId <= 0)
        {
            return RedirectToPage("/Order/Index");
        }

        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login");
        }

        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await client.GetAsync($"/api/Payment/paypal/details/{OrderId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<PaypalCheckoutDetailsDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Success == true && result.Data != null)
                {
                    Details = result.Data;
                }
                else
                {
                    ErrorMessage = result?.Message ?? "Could not retrieve PayPal payment details.";
                }
            }
            else
            {
                ErrorMessage = "Failed to load PayPal transaction details.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error connecting to payment server: " + ex.Message;
        }

        return Page();
    }
}
