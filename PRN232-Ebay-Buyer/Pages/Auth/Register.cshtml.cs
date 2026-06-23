using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN232_Ebay_Buyer.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IHttpClientFactory httpClientFactory, ILogger<RegisterModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsLoading { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            Message = "All fields are required.";
            IsSuccess = false;
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            Message = "Passwords do not match.";
            IsSuccess = false;
            return Page();
        }

        if (Password.Length < 6)
        {
            Message = "Password must be at least 6 characters.";
            IsSuccess = false;
            return Page();
        }

        IsLoading = true;

        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");

            var payload = new
            {
                username = Username.Trim(),
                email = Email.Trim().ToLowerInvariant(),
                password = Password
            };

            var response = await client.PostAsJsonAsync("/api/auth/register", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<ApiResponse<RegisterResponse>>();

                if (result?.Success == true)
                {
                    Message = $"Registration successful! Your verification token is: {result.Data?.VerificationToken}";
                    IsSuccess = true;
                    _logger.LogInformation(
                        "Registration succeeded for {Email}. Token: {Token}",
                        Email, result.Data?.VerificationToken);
                }
                else
                {
                    Message = result?.Message ?? "Registration failed.";
                    IsSuccess = false;
                }
            }
            else
            {
                var errorContent = await response.Content
                    .ReadFromJsonAsync<ApiResponse<object>>();

                Message = errorContent?.Message ?? $"Error: {response.StatusCode}";
                IsSuccess = false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to API");
            Message = "Unable to connect to server. Please try again later.";
            IsSuccess = false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse API response");
            Message = "Invalid response from server.";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }

        return Page();
    }

    private record ApiResponse<T>(bool Success, string Message, T? Data);
    private record RegisterResponse(int UserId, string Email, string VerificationToken);
}
