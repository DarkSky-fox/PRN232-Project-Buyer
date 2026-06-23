using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN232_Ebay_Buyer.Pages;

public class ProfileModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(IHttpClientFactory httpClientFactory, ILogger<ProfileModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;

    public bool IsAuthenticated { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsLoading { get; set; }

    public IActionResult OnGet()
    {
        LoadUserFromClaims();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        LoadUserFromClaims();

        if (!IsAuthenticated)
        {
            Message = "Session expired. Please log in again.";
            IsSuccess = false;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            Message = "Username is required.";
            IsSuccess = false;
            return Page();
        }

        IsLoading = true;

        try
        {
            // Log tat ca claims de debug
            var allClaims = string.Join(" | ",
                User.Claims.Select(c => $"{c.Type}={c.Value}"));
            _logger.LogWarning("Profile POST - All claims: {Claims}", allClaims);
            _logger.LogWarning("Profile POST - User.Identity.IsAuthenticated={IsAuth}",
                User.Identity?.IsAuthenticated);

            // Doc JWT tu Claims (da duoc luu khi dang nhap)
            var token = User.FindFirst("jwt_token")?.Value ?? string.Empty;

            _logger.LogWarning("Profile update attempt. IsAuthenticated={Auth}, JWT from claims present={HasToken}",
                IsAuthenticated, !string.IsNullOrEmpty(token));

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("JWT token missing from claims");
                Message = "Authentication token missing. Please log in again.";
                IsSuccess = false;
                IsLoading = false;
                return Page();
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                username = Username.Trim(),
                avatarUrl = AvatarUrl?.Trim()
            };

            _logger.LogWarning("Sending PUT to /api/auth/update-profile with Bearer token prefix: {Prefix}",
                token.Length > 20 ? token.Substring(0, 20) + "..." : token);

            var response = await client.PutAsJsonAsync("/api/auth/update-profile", payload);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("API response: Status={Status}, Body={Body}", response.StatusCode, responseBody);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();

                if (result?.Success == true && result.Data is not null)
                {
                    Email = result.Data.Email;
                    Role = result.Data.Role;
                    AvatarUrl = result.Data.AvatarUrl ?? string.Empty;

                    Message = "Profile updated successfully!";
                    IsSuccess = true;
                }
                else
                {
                    Message = result?.Message ?? "Update failed.";
                    IsSuccess = false;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Message = "Session expired. Please log in again.";
                IsSuccess = false;
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
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
            _logger.LogError(ex, "Failed to connect to API during profile update");
            Message = "Unable to connect to server. Please try again later.";
            IsSuccess = false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse API response during profile update");
            Message = "Invalid response from server.";
            IsSuccess = false;
        }
        finally
        {
            IsLoading = false;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetLogoutAsync()
    {
        Response.Cookies.Delete("EbayAuth");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Auth/Login");
    }

    private void LoadUserFromClaims()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
            Username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
            IsAuthenticated = !string.IsNullOrEmpty(Email);
        }
        else
        {
            IsAuthenticated = false;
        }
    }

    private record ApiResponse<T>(bool Success, string Message, T? Data);
    private record UserProfileResponse(
        int Id,
        string Username,
        string Email,
        string Role,
        string? AvatarUrl
    );
}
