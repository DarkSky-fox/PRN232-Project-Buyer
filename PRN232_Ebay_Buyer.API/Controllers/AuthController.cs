using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PRN232_Ebay_Buyer.API.DTOs;
using PRN232_Ebay_Buyer.API.Services;
using PRN232_Ebay_Buyer.API.Models;

namespace PRN232_Ebay_Buyer.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CloneEbayDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    private static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(10);

    public AuthController(
        CloneEbayDbContext db,
        IJwtService jwtService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    // ─── POST /api/auth/register ────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(
        [FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ApiResponse<RegisterResponse>(
                false, "Username, email and password are required.", null));
        }

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == request.Email.Trim());

        if (emailExists)
        {
            return Conflict(new ApiResponse<RegisterResponse>(
                false, "Email is already registered.", null));
        }

        var verificationToken = GenerateSecureToken();

        var newUser = new User
        {
            Username = request.Username.Trim(),
            Email    = request.Email.Trim().ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role     = "User",
            AvatarUrl = null
        };

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        _cache.Set(
            $"verify:{verificationToken}",
            newUser.Email,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TokenExpiry
            });

        _logger.LogInformation(
            "Verification token stored for {Email}. Token: {Token}",
            newUser.Email, verificationToken);

        return Ok(new ApiResponse<RegisterResponse>(
            true,
            "Registration successful. Please check your email to verify your account.",
            new RegisterResponse(newUser.Id, newUser.Email, verificationToken)));
    }

    // ─── GET /api/auth/verify-email ─────────────────────────────────────────
    [HttpGet("verify-email")]
    public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new ApiResponse<object>(
                false, "Token is required.", null));
        }

        if (!_cache.TryGetValue<string>($"verify:{token}", out var email) || email is null)
        {
            return BadRequest(new ApiResponse<object>(
                false, "Invalid or expired token.", null));
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            return NotFound(new ApiResponse<object>(
                false, "User not found.", null));
        }

        _cache.Remove($"verify:{token}");

        _logger.LogInformation("Email verified successfully for {Email}", email);

        return Ok(new ApiResponse<object>(
            true,
            "Email verified successfully. You can now log in.",
            new { userId = user.Id, email = user.Email }));
    }

    // ─── POST /api/auth/login ──────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ApiResponse<LoginResponse>(
                false, "Email and password are required.", null));
        }

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());

        if (user is null)
        {
            return Unauthorized(new ApiResponse<LoginResponse>(
                false, "Invalid email or password.", null));
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return Unauthorized(new ApiResponse<LoginResponse>(
                false, "Invalid email or password.", null));
        }

        var token = _jwtService.GenerateToken(
            user.Id,
            user.Username ?? string.Empty,
            user.Email ?? string.Empty,
            user.Role ?? "User");

        var expiryMinutes = _configuration.GetValue<int>("Jwt:ExpiryInMinutes", 60);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        _logger.LogInformation("User {Email} logged in successfully.", user.Email);

        return Ok(new ApiResponse<LoginResponse>(
            true,
            "Login successful.",
            new LoginResponse(token, user.Username ?? string.Empty,
                user.Email ?? string.Empty, user.Role ?? "User", expiresAt)));
    }

    // ─── PUT /api/auth/update-profile ──────────────────────────────────────
    [Authorize]
    [HttpPut("update-profile")]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogWarning("[UpdateProfile] All claims: {Claims}, NameIdentifier={Claim}",
            string.Join(" | ", User.Claims.Select(c => $"{c.Type}={c.Value}")),
            userIdClaim);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiResponse<UserProfileResponse>(
                false, "Invalid token payload.", null));
        }

        var user = await _db.Users.FindAsync(userId);

        if (user is null)
        {
            return NotFound(new ApiResponse<UserProfileResponse>(
                false, "User not found.", null));
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            user.Username = request.Username.Trim();
        }

        if (request.AvatarUrl is not null)
        {
            user.AvatarUrl = request.AvatarUrl.Trim();
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Profile updated for user {UserId}.", userId);

        return Ok(new ApiResponse<UserProfileResponse>(
            true,
            "Profile updated successfully.",
            new UserProfileResponse(
                user.Id,
                user.Username ?? string.Empty,
                user.Email ?? string.Empty,
                user.Role ?? "User",
                user.AvatarUrl)));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
