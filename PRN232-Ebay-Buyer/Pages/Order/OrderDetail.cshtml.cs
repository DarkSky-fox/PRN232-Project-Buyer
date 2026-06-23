using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages;

public class OrderDetailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderDetailModel> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public OrderDetailModel(IHttpClientFactory httpClientFactory, ILogger<OrderDetailModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ─── View Data ─────────────────────────────────────────────────────────────
    public OrderDetailResponse? Order { get; set; }
    public string? ErrorMessage   { get; set; }
    public string? SuccessMessage { get; set; }

    // ─── GET ───────────────────────────────────────────────────────────────────
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = CreateClient(token);

        try
        {
            var response = await client.GetAsync($"/api/orders/{id}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<OrderDetailResponse>>(json, JsonOpts);
                Order = result?.Data;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToPage("/Auth/Login");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound
                  || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return RedirectToPage("/Orders");
            }
            else
            {
                ErrorMessage = "Không thể tải chi tiết đơn hàng.";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = "Không thể kết nối tới server.";
            _logger.LogError("API error: {Error}", ex.Message);
        }

        return Page();
    }

    // ─── POST: Gửi yêu cầu hoàn trả / huỷ ─────────────────────────────────────
    public async Task<IActionResult> OnPostReturnAsync(int orderId, string type, string reason)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = CreateClient(token);

        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { Type = type, Reason = reason }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"/api/orders/{orderId}/return", body);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<ReturnRequestResponse>>(json, JsonOpts);

            if (response.IsSuccessStatusCode || (int)response.StatusCode == 201)
            {
                SuccessMessage = type == "Cancel"
                    ? "✅ Yêu cầu huỷ đơn đã được gửi thành công. Chúng tôi sẽ xử lý sớm nhất!"
                    : "✅ Yêu cầu hoàn trả đã được gửi thành công. Chúng tôi sẽ liên hệ bạn sớm!";
            }
            else
            {
                ErrorMessage = result?.Message ?? "Không thể gửi yêu cầu. Vui lòng thử lại.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi kết nối server.";
            _logger.LogError("Return request error: {Error}", ex.Message);
        }

        return await OnGetAsync(orderId);
    }

    // ─── POST: Gửi đánh giá ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostReviewAsync(int orderId, int productId, int rating, string? comment)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = CreateClient(token);

        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { ProductId = productId, Rating = rating, Comment = comment }),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/reviews", body);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<ReviewResponse>>(json, JsonOpts);

            if (response.IsSuccessStatusCode || (int)response.StatusCode == 201)
            {
                SuccessMessage = "⭐ Cảm ơn bạn đã đánh giá sản phẩm!";
            }
            else
            {
                ErrorMessage = result?.Message ?? "Không thể gửi đánh giá.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi kết nối server.";
            _logger.LogError("Review error: {Error}", ex.Message);
        }

        return await OnGetAsync(orderId);
    }

    // ─── POST: Sửa đánh giá ────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostUpdateReviewAsync(int orderId, int reviewId, int rating, string? comment)
    {
        var token = HttpContext.Request.Cookies["BearerToken"];
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login");

        var client = CreateClient(token);

        try
        {
            var body = new StringContent(
                JsonSerializer.Serialize(new { Rating = rating, Comment = comment }),
                Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/reviews/{reviewId}") { Content = body };
            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<ReviewResponse>>(json, JsonOpts);

            SuccessMessage = response.IsSuccessStatusCode
                ? "✅ Đánh giá đã được cập nhật thành công!"
                : result?.Message ?? "Không thể cập nhật đánh giá.";

            if (!response.IsSuccessStatusCode)
                ErrorMessage = SuccessMessage;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi kết nối server.";
            _logger.LogError("Update review error: {Error}", ex.Message);
        }

        return await OnGetAsync(orderId);
    }

    // ─── Helper ────────────────────────────────────────────────────────────────
    private HttpClient CreateClient(string token)
    {
        var client = _httpClientFactory.CreateClient("AuthApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
