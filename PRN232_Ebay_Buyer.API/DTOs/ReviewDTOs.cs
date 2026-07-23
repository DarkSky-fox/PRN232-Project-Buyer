namespace PRN232_Ebay_Buyer.API.DTOs;

// ─── Request DTOs ──────────────────────────────────────────────────────────────

/// <summary>
/// Tạo đánh giá sản phẩm mới. Rating phải từ 1–5.
/// </summary>
public record CreateReviewRequest(
    int ProductId,
    int Rating,      // 1–5, bắt buộc
    string? Comment
);

/// <summary>
/// Sửa đánh giá đã tạo trước đó. Rating phải từ 1–5.
/// </summary>
public record UpdateReviewRequest(
    int Rating,      // 1–5, bắt buộc
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

/// <summary>
/// Kết quả kiểm tra quyền review sản phẩm của user hiện tại.
/// </summary>
public record CanReviewResponse(
    bool CanReview,      // đã mua & đã Delivered
    bool HasReviewed,    // đã từng review sản phẩm này
    int? ReviewId        // id review hiện tại (nếu đã review)
);
