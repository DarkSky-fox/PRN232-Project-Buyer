using System.Net.Http.Headers;
using System.Text;
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

    // ─── Review Status ───────────────────────────────────────────────────────
    public bool CanReview { get; set; }        // đã mua & đã Delivered
    public bool HasReviewed { get; set; }      // đã từng review sản phẩm này
    public ReviewResponse? UserReview { get; set; } // review hiện tại (nếu có)

    // ─── Review Form Bindings ────────────────────────────────────────────────
    [BindProperty]
    public int ReviewRating { get; set; } = 5;

    [BindProperty]
    public string ReviewComment { get; set; } = "";

    // 0 = tạo mới, >0 = sửa review hiện có
    [BindProperty]
    public int ReviewId { get; set; }

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
            // 1. Load product detail
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

            // 2. Nếu đã đăng nhập → kiểm tra quyền review & load review hiện có
            if (!string.IsNullOrEmpty(token) && Product != null)
            {
                await LoadReviewStatus(client, id.Value);
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

    private async Task LoadReviewStatus(HttpClient client, int productId)
    {
        try
        {
            // Gọi song song 2 endpoint
            var canReviewTask = client.GetAsync($"/api/reviews/can-review?productId={productId}");
            var myReviewTask  = client.GetAsync($"/api/reviews/my?productId={productId}");
            await Task.WhenAll(canReviewTask, myReviewTask);

            var canReviewResp = await canReviewTask;
            var myReviewResp  = await myReviewTask;

            if (canReviewResp.IsSuccessStatusCode)
            {
                var json = await canReviewResp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<CanReviewResponse>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Data != null)
                {
                    CanReview   = result.Data.CanReview;
                    HasReviewed = result.Data.HasReviewed;
                    ReviewId    = result.Data.ReviewId ?? 0;
                }
            }

            if (myReviewResp.IsSuccessStatusCode)
            {
                var json = await myReviewResp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ReviewResponse?>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                UserReview = result?.Data;
                if (UserReview != null)
                {
                    ReviewRating  = UserReview.Rating ?? 5;
                    ReviewComment = UserReview.Comment ?? "";
                }
            }
        }
        catch
        {
            // Bỏ qua lỗi, không ảnh hưởng đến hiển thị sản phẩm
        }
    }

    // ─── OnPostReviewAsync ───────────────────────────────────────────────────
    public async Task<IActionResult> OnPostReviewAsync(int id)
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
            var body = new { ProductId = id, Rating = ReviewRating, Comment = ReviewComment };
            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            response = await client.PostAsync("/api/reviews", content);
        }

        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = ReviewId > 0
                ? "Your review has been updated successfully!"
                : "Thank you! Your review has been submitted.";
        }
        else
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            try
            {
                var errResult = JsonSerializer.Deserialize<ApiResponse<object>>(
                    errorJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                TempData["ErrorMessage"] = errResult?.Message ?? "Failed to submit review.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to submit review.";
            }
        }

        return RedirectToPage(new { id });
    }
}
