namespace PRN232_Ebay_Buyer.API.DTOs;

// ─── Pagination ────────────────────────────────────────────────────────────────

public record PagedResponse<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

// ─── Sub-records ───────────────────────────────────────────────────────────────

public record AddressInfo(
    string? FullName,
    string? Phone,
    string? Street,
    string? City,
    string? State,
    string? Country
);

public record OrderItemInfo(
    int ProductId,
    string? ProductTitle,
    string? ProductImage,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal
);

public record PaymentInfo(
    string? Method,
    decimal? Amount,
    string? Status,
    DateTime? PaidAt
);

public record ShippingStatusInfo(
    string? Carrier,
    string? TrackingNumber,
    string? Status,
    DateTime? EstimatedArrival
);

public record ReturnRequestInfo(
    int Id,
    string? Type,
    string? Reason,
    string? Status,
    DateTime? CreatedAt
);

// ─── Order Responses ───────────────────────────────────────────────────────────

public record OrderSummaryResponse(
    int Id,
    DateTime? OrderDate,
    string? Status,
    decimal? TotalPrice,
    int ItemCount
);

public record OrderDetailResponse(
    int Id,
    DateTime? OrderDate,
    string? Status,
    decimal? TotalPrice,
    AddressInfo? Address,
    List<OrderItemInfo> Items,
    PaymentInfo? Payment,
    ShippingStatusInfo? Shipping,
    ReturnRequestInfo? ReturnRequest
);

// ─── Return Request ────────────────────────────────────────────────────────────

/// <summary>
/// Type: "Cancel" (trước giao hàng) hoặc "Return" (sau khi nhận hàng)
/// </summary>
public record CreateReturnRequestDto(
    string Type,
    string Reason
);

public record ReturnRequestResponse(
    int Id,
    int? OrderId,
    string? Type,
    string? Reason,
    string? Status,
    DateTime? CreatedAt
);
