using System.Net.Http.Json;
using System.Security.Claims;
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
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            Message = "Session expired. Please log in again.";
            IsSuccess = false;
            return Page();
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
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
            var payload = new
            {
                userId = int.Parse(userId),
                username = Username.Trim(),
                avatarUrl = AvatarUrl?.Trim()
            };

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PutAsJsonAsync("/api/auth/update-profile", payload);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();

                if (result?.Success == true && result.Data is not null)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                    if (!string.IsNullOrEmpty(result.Data.NewToken) &&
                        !string.IsNullOrEmpty(userIdClaim))
                    {
                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.NameIdentifier, result.Data.Id.ToString()),
                            new(ClaimTypes.Name, result.Data.Username),
                            new(ClaimTypes.Email, result.Data.Email),
                            new(ClaimTypes.Role, result.Data.Role)
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims,
                            CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                        };

                        await HttpContext.SignOutAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme);

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        Response.Cookies.Append("BearerToken", result.Data.NewToken,
                            new CookieOptions
                            {
                                HttpOnly = false,
                                SameSite = SameSiteMode.Lax,
                                Expires = DateTimeOffset.UtcNow.AddHours(1)
                            });
                    }

                    Email = result.Data.Email;
                    Role = result.Data.Role;
                    Username = result.Data.Username;
                    AvatarUrl = result.Data.AvatarUrl ?? string.Empty;
                    IsAuthenticated = true;

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
        string? AvatarUrl,
        string? NewToken
    );
}
