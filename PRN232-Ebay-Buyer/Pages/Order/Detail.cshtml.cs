using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace PRN232_Ebay_Buyer.Pages.Order;

public class DetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public OrderDetailResponse? OrderDetail { get; set; }

    [BindProperty]
    public int OrderId { get; set; }

    [BindProperty]
    public string CancelReason { get; set; } = "";

    [BindProperty]
    public string ReturnReason { get; set; } = "";

    [BindProperty]
    public string ReturnImageData { get; set; } = ""; // JSON array of Base64 image strings
    
    [BindProperty]
    public int ReviewProductId { get; set; }
    
    [BindProperty]
    public int ReviewRating { get; set; }
    
    [BindProperty]
    public string ReviewComment { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login");
        }

        OrderId = id;
        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/orders/{id}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<OrderDetailResponse>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result?.Data != null)
            {
                OrderDetail = result.Data;
            }
        }
        else
        {
            return RedirectToPage("/Order/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        return await HandleReturnOrCancel("Cancel", CancelReason);
    }

    public async Task<IActionResult> OnPostReturnAsync()
    {
        // Nếu có ảnh đính kèm, gắn thông tin ảnh vào reason để lưu vào DB
        var reasonWithImages = ReturnReason;
        if (!string.IsNullOrWhiteSpace(ReturnImageData) && ReturnImageData != "[]")
        {
            reasonWithImages = ReturnReason + "\n[IMAGE_DATA]" + ReturnImageData;
        }
        return await HandleReturnOrCancel("Return", reasonWithImages);
    }

    private async Task<IActionResult> HandleReturnOrCancel(string type, string reason)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new { type, reason };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"/api/orders/{OrderId}/return", content);
        
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = $"Your {type.ToLower()} request has been submitted successfully.";
        }
        else
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            try {
                var errResult = JsonSerializer.Deserialize<ApiResponse<object>>(errorJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                TempData["ErrorMessage"] = errResult?.Message ?? "Failed to submit request.";
            } catch {
                TempData["ErrorMessage"] = "Failed to submit request.";
            }
        }

        return RedirectToPage(new { id = OrderId });
    }

    public async Task<IActionResult> OnPostReviewAsync()
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token)) return RedirectToPage("/Auth/Login");

        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new { ProductId = ReviewProductId, Rating = ReviewRating, Comment = ReviewComment };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/reviews", content);
        
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = "Thank you! Your review has been submitted.";
        }
        else
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            try {
                var errResult = JsonSerializer.Deserialize<ApiResponse<object>>(errorJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                TempData["ErrorMessage"] = errResult?.Message ?? "Failed to submit review.";
            } catch {
                TempData["ErrorMessage"] = "Failed to submit review.";
            }
        }

        return RedirectToPage(new { id = OrderId });
    }
}
