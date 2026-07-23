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

    // key = productId, value = existing review (null nếu chưa review)
    public Dictionary<int, ReviewResponse?> UserReviews { get; set; } = new();

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

    // 0 = tạo mới, > 0 = sửa review hiện có
    [BindProperty]
    public int ReviewId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, bool paid = false)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage("/Auth/Login");
        }

        if (paid)
        {
            TempData["SuccessMessage"] = "Payment completed successfully via PayPal!";
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

                // Nếu đơn hàng đã Delivered → load review hiện có của user cho từng sản phẩm
                if (OrderDetail.Status == "Delivered" && OrderDetail.Items?.Count > 0)
                {
                    var reviewTasks = OrderDetail.Items.Select(async item =>
                    {
                        try
                        {
                            var reviewResp = await client.GetAsync($"/api/reviews/my?productId={item.ProductId}");
                            if (reviewResp.IsSuccessStatusCode)
                            {
                                var reviewJson = await reviewResp.Content.ReadAsStringAsync();
                                var reviewResult = JsonSerializer.Deserialize<ApiResponse<ReviewResponse?>>(
                                    reviewJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                return (item.ProductId, Review: reviewResult?.Data);
                            }
                        }
                        catch { /* bỏ qua lỗi */ }
                        return (item.ProductId, Review: (ReviewResponse?)null);
                    });

                    var reviewResults = await Task.WhenAll(reviewTasks);
                    foreach (var (productId, review) in reviewResults)
                    {
                        UserReviews[productId] = review;
                    }
                }
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

        HttpResponseMessage response;

        if (ReviewId > 0)
        {
            // ── Sửa review đã có ──────────────────────────────────────────
            var body = new { Rating = ReviewRating, Comment = ReviewComment };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            response = await client.PutAsync($"/api/reviews/{ReviewId}", content);
        }
        else
        {
            // ── Tạo review mới ────────────────────────────────────────────
            var body = new { ProductId = ReviewProductId, Rating = ReviewRating, Comment = ReviewComment };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            response = await client.PostAsync("/api/reviews", content);
        }
        
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = ReviewId > 0
                ? "Your review has been updated successfully."
                : "Thank you! Your review has been submitted.";
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
