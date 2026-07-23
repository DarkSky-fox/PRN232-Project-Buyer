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
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileModel(
        IHttpClientFactory httpClientFactory, 
        ILogger<ProfileModel> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    
    [BindProperty]
    public string AvatarUrl { get; set; } = string.Empty;

    [BindProperty]
    public string? FullName { get; set; }

    [BindProperty]
    public string? Phone { get; set; }

    [BindProperty]
    public string? Street { get; set; }

    [BindProperty]
    public string? City { get; set; }

    [BindProperty]
    public string? State { get; set; }

    [BindProperty]
    public string? Country { get; set; }

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    public bool IsAuthenticated { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsLoading { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUserProfileAsync();
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
            // ── Handle image upload from file explorer ──
            if (AvatarFile != null)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(fileStream);
                }

                AvatarUrl = "/uploads/" + uniqueFileName;
            }

            var payload = new
            {
                userId = int.Parse(userId),
                username = Username.Trim(),
                avatarUrl = !string.IsNullOrEmpty(AvatarUrl) ? AvatarUrl.Trim() : null,
                fullName = FullName?.Trim(),
                phone = Phone?.Trim(),
                street = Street?.Trim(),
                city = City?.Trim(),
                state = State?.Trim(),
                country = Country?.Trim()
            };

            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.PutAsJsonAsync("/api/auth/update-profile", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content
                    .ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();

                if (result?.Success == true && result.Data is not null)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    
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

                    Message = "Profile updated successfully!";
                    IsSuccess = true;
                    
                    // Reload data from backend to ensure consistent state
                    await LoadUserProfileAsync();
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
        Response.Cookies.Delete("BearerToken");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Auth/Login");
    }

    private async Task LoadUserProfileAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("AuthApi");
                    var response = await client.GetAsync($"/api/auth/profile/{userId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content
                            .ReadFromJsonAsync<ApiResponse<UserProfileResponse>>();
                        if (result?.Success == true && result.Data is not null)
                        {
                            Email = result.Data.Email;
                            Username = result.Data.Username;
                            Role = result.Data.Role;
                            AvatarUrl = result.Data.AvatarUrl ?? string.Empty;
                            FullName = result.Data.FullName;
                            Phone = result.Data.Phone;
                            Street = result.Data.Street;
                            City = result.Data.City;
                            State = result.Data.State;
                            Country = result.Data.Country;
                            IsAuthenticated = true;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load user profile from API");
                }
            }

            // Fallback to claims if API load fails
            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            Username = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            Role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
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
        string? NewToken,
        string? FullName,
        string? Phone,
        string? Street,
        string? City,
        string? State,
        string? Country
    );
}
