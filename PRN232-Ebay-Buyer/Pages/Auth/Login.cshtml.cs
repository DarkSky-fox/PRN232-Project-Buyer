using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PRN232_Ebay_Buyer.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IHttpClientFactory httpClientFactory, ILogger<LoginModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsLoading { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Email and password are required.";
            IsSuccess = false;
            return Page();
        }

        IsLoading = true;

        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");

            var payload = new { email = Email.Trim().ToLowerInvariant(), password = Password };
            var response = await client.PostAsJsonAsync("/api/auth/login", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

                if (result?.Success == true && result.Data is not null)
                {
                    var loginData = result.Data;

                    // Lay user info tu token
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(loginData.Token);
                    var userId = jwtToken.Claims
                        .First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                    var username = jwtToken.Claims
                        .First(c => c.Type == ClaimTypes.Name).Value;
                    var email = jwtToken.Claims
                        .First(c => c.Type == ClaimTypes.Email).Value;
                    var role = jwtToken.Claims
                        .First(c => c.Type == ClaimTypes.Role).Value;

                    // Luu JWT vao Cookie de Profile page gui len API
                    Response.Cookies.Append("BearerToken", loginData.Token, new CookieOptions
                    {
                        HttpOnly = false,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = loginData.ExpiresAt
                    });

                    // Tao Claims Identity cho Cookie Authentication (luu userId va JWT)
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name,           username),
                        new Claim(ClaimTypes.Email,          email),
                        new Claim(ClaimTypes.Role,          role),
                        new Claim("jwt_token",              loginData.Token)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = loginData.ExpiresAt
                        });

                    _logger.LogInformation("User {Email} logged in successfully. JWT token length={Len}",
                        Email, loginData.Token.Length);

                    return RedirectToPage("/Index");
                }

                Message = result?.Message ?? "Login failed.";
                IsSuccess = false;
            }
            else
            {
                var errorContent = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Message = errorContent?.Message ?? $"Error: {response.StatusCode}";
                IsSuccess = false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to API during login");
            Message = "Unable to connect to server. Please try again later.";
            IsSuccess = false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse API response during login");
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
    private record LoginResponse(
        string Token,
        string Username,
        string Email,
        string Role,
        DateTime ExpiresAt
    );
}
