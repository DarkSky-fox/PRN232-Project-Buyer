namespace PRN232_Ebay_Buyer.API.DTOs;

// ─── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>
/// Tạo đánh giá sản phẩm mới. Rating phải từ 1–5.
/// </summary>
public record CreateReviewRequest(
    int ProductId,
    int Rating,
    string? Comment
);

/// <summary>
/// Sửa đánh giá đã tạo trước đó. Rating phải từ 1–5.
/// </summary>
public record UpdateReviewRequest(
    int Rating,
    string? Comment
);

// ─── Response DTOs ─────────────────────────────────────────────────────────────

public record ReviewResponse(
    int Id,
    int? ProductId,
    int? ReviewerId,
    string? ReviewerName,
    string? ReviewerAvatar,
    int? Rating,
    string? Comment,
    DateTime? CreatedAt
);
