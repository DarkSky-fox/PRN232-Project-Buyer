namespace PRN232_Ebay_Buyer.API.DTOs;

// ─── Product DTOs ──────────────────────────────────────────────────────────

public record ProductDto(
    int Id,
    string? Title,
    string? Description,
    decimal? Price,
    string? Images,
    int? CategoryId,
    string? CategoryName,
    int? SellerId,
    string? SellerName,
    bool? IsAuction,
    DateTime? AuctionEndTime
);

public record CategoryDto(
    int Id,
    string? Name,
    int ProductCount
);

// ─── Paged Result Wrapper ──────────────────────────────────────────────────

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

// ─── Product Detail DTOs ───────────────────────────────────────────────────

public record ReviewDto(
    int Id,
    int? Rating,
    string? Comment,
    string? ReviewerName,
    string? ReviewerAvatar,
    DateTime? CreatedAt
);

public record SellerInfoDto(
    int Id,
    string? Username,
    string? AvatarUrl,
    string? StoreName,
    string? StoreDescription,
    decimal? AverageRating,
    int? TotalReviews,
    decimal? PositiveRate,
    int TotalProducts
);

public record ProductDetailDto(
    int Id,
    string? Title,
    string? Description,
    decimal? Price,
    string? Images,
    int? CategoryId,
    string? CategoryName,
    bool? IsAuction,
    DateTime? AuctionEndTime,
    int StockQuantity,
    double AverageRating,
    int ReviewCount,
    SellerInfoDto? Seller,
    List<ReviewDto> Reviews,
    List<ProductDto> RelatedProducts
);
