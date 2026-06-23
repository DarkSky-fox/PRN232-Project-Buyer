namespace PRN232_Ebay_Buyer.API.DTOs;

// ─── Request DTOs ──────────────────────────────────────────────────────────

public record RegisterRequest(
    string Username,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record UpdateProfileRequest(
    int? UserId,
    string? Username,
    string? AvatarUrl
);

// ─── Response DTOs ─────────────────────────────────────────────────────────

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data
);

public record RegisterResponse(
    int UserId,
    string Email,
    string VerificationToken
);

public record LoginResponse(
    string Token,
    string Username,
    string Email,
    string Role,
    DateTime ExpiresAt
);

public record UserProfileResponse(
    int Id,
    string Username,
    string Email,
    string Role,
    string? AvatarUrl,
    string? NewToken
);
