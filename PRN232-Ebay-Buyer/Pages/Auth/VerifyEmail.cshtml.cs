using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN232_Ebay_Buyer.Pages.Auth;

public class VerifyEmailModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<VerifyEmailModel> _logger;

    public VerifyEmailModel(IHttpClientFactory httpClientFactory, ILogger<VerifyEmailModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Title { get; set; } = "Email Verification";
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsLoading { get; set; } = true;

    public async Task<IActionResult> OnGetAsync([FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Title = "Invalid Request";
            Message = "Verification token is missing.";
            IsSuccess = false;
            IsLoading = false;
            return Page();
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.GetAsync($"/api/auth/verify-email?token={Uri.EscapeDataString(token)}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<ApiResponse<object>>();

                IsSuccess = result?.Success ?? false;
                Title = IsSuccess ? "Verification Successful!" : "Verification Failed";
                Message = result?.Message ?? "An unexpected error occurred.";
            }
            else
            {
                var errorContent = await response.Content
                    .ReadFromJsonAsync<ApiResponse<object>>();

                IsSuccess = false;
                Title = "Verification Failed";
                Message = errorContent?.Message ?? $"Error: {response.StatusCode}";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to API during email verification");
            IsSuccess = false;
            Title = "Connection Error";
            Message = "Unable to connect to server. Please try again later.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse API response during email verification");
            IsSuccess = false;
            Title = "Server Error";
            Message = "Invalid response from server.";
        }
        finally
        {
            IsLoading = false;
        }

        return Page();
    }

    private record ApiResponse<T>(bool Success, string Message, T? Data);
}
