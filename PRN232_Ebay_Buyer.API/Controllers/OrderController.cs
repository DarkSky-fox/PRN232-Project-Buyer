using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Ebay_Buyer.API.DTOs;
using PRN232_Ebay_Buyer.API.Models;
using System.Security.Claims;

namespace PRN232_Ebay_Buyer.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly CloneEbayDbContext _db;
    private readonly ILogger<OrderController> _logger;

    // Các trạng thái hợp lệ cho từng loại yêu cầu hoàn trả
    private static readonly string[] CancelableStatuses = ["Pending", "Processing", "Shipped"];
    private static readonly string[] ReturnableStatuses  = ["Delivered"];

    public OrderController(CloneEbayDbContext db, ILogger<OrderController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── GET /api/orders ────────────────────────────────────────────────────────
    /// <summary>
    /// Lấy danh sách lịch sử đơn hàng của buyer (có phân trang và lọc theo status).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResponse<OrderSummaryResponse>>>> GetOrders(
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize   = 5)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(Fail<PagedResponse<OrderSummaryResponse>>("Token không hợp lệ."));

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize   < 1) pageSize   = 5;

        var query = _db.OrderTables
            .Include(o => o.OrderItems)
            .Where(o => o.BuyerId == userId);

        // Lọc theo status nếu có
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status.Trim());

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = orders.Select(o => new OrderSummaryResponse(
            o.Id,
            o.OrderDate,
            o.Status,
            o.TotalPrice,
            o.OrderItems.Count
        )).ToList();

        var paged = new PagedResponse<OrderSummaryResponse>(items, totalCount, pageNumber, pageSize);

        return Ok(new ApiResponse<PagedResponse<OrderSummaryResponse>>(
            true, "Lấy danh sách đơn hàng thành công.", paged));
    }

    // ─── GET /api/orders/{id} ───────────────────────────────────────────────────
    /// <summary>
    /// Lấy chi tiết một đơn hàng: sản phẩm, địa chỉ, thanh toán, vận chuyển, hoàn trả.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> GetOrderById(int id)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(Fail<OrderDetailResponse>("Token không hợp lệ."));

        var order = await _db.OrderTables
            .Include(o => o.Address)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .Include(o => o.ShippingInfos)
            .Include(o => o.ReturnRequests)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(Fail<OrderDetailResponse>("Không tìm thấy đơn hàng."));

        // Kiểm tra đơn hàng có thuộc về buyer hiện tại không
        if (order.BuyerId != userId)
            return StatusCode(403, Fail<OrderDetailResponse>("Bạn không có quyền xem đơn hàng này."));

        var detail = MapToDetail(order);

        return Ok(new ApiResponse<OrderDetailResponse>(
            true, "Lấy chi tiết đơn hàng thành công.", detail));
    }

    // ─── POST /api/orders/{id}/return ───────────────────────────────────────────
    /// <summary>
    /// Gửi yêu cầu hoàn trả hoặc huỷ đơn hàng.
    /// Type = "Cancel" → huỷ trước giao (Pending/Processing/Shipped).
    /// Type = "Return" → hoàn trả sau khi nhận (Delivered).
    /// </summary>
    [HttpPost("{id:int}/return")]
    public async Task<ActionResult<ApiResponse<ReturnRequestResponse>>> CreateReturnRequest(
        int id,
        [FromBody] CreateReturnRequestDto dto)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(Fail<ReturnRequestResponse>("Token không hợp lệ."));

        // Validate type
        if (string.IsNullOrWhiteSpace(dto.Type) ||
            (dto.Type != "Cancel" && dto.Type != "Return"))
        {
            return BadRequest(Fail<ReturnRequestResponse>(
                "Type phải là 'Cancel' (huỷ trước giao) hoặc 'Return' (hoàn trả sau nhận)."));
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(Fail<ReturnRequestResponse>("Lý do không được để trống."));

        var order = await _db.OrderTables
            .Include(o => o.ReturnRequests)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return NotFound(Fail<ReturnRequestResponse>("Không tìm thấy đơn hàng."));

        if (order.BuyerId != userId)
            return StatusCode(403, Fail<ReturnRequestResponse>("Bạn không có quyền thao tác đơn hàng này."));

        // Kiểm tra trạng thái phù hợp với type
        if (dto.Type == "Cancel" && !CancelableStatuses.Contains(order.Status))
        {
            return BadRequest(Fail<ReturnRequestResponse>(
                $"Đơn hàng ở trạng thái '{order.Status}' không thể huỷ. " +
                $"Chỉ huỷ được khi đơn đang ở: {string.Join(", ", CancelableStatuses)}."));
        }

        if (dto.Type == "Return" && !ReturnableStatuses.Contains(order.Status))
        {
            return BadRequest(Fail<ReturnRequestResponse>(
                $"Chỉ có thể hoàn trả đơn hàng đã giao (Delivered). " +
                $"Đơn hàng hiện tại đang ở trạng thái '{order.Status}'."));
        }

        // Kiểm tra chưa có yêu cầu hoàn trả
        if (order.ReturnRequests.Any())
        {
            return Conflict(Fail<ReturnRequestResponse>(
                "Đơn hàng này đã có yêu cầu hoàn trả/huỷ đang được xử lý."));
        }

        // Lưu type vào prefix của reason để không cần thêm cột DB
        var returnRequest = new ReturnRequest
        {
            OrderId   = order.Id,
            UserId    = userId,
            Reason    = $"[{dto.Type}] {dto.Reason.Trim()}",
            Status    = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.ReturnRequests.Add(returnRequest);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "ReturnRequest #{RrId} tạo bởi user {UserId} cho đơn #{OrderId} (Type={Type}).",
            returnRequest.Id, userId, order.Id, dto.Type);

        var response = new ReturnRequestResponse(
            returnRequest.Id,
            returnRequest.OrderId,
            dto.Type,
            dto.Reason.Trim(),
            returnRequest.Status,
            returnRequest.CreatedAt);

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = order.Id },
            new ApiResponse<ReturnRequestResponse>(
                true, "Yêu cầu hoàn trả/huỷ đã được gửi thành công.", response));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static ApiResponse<T> Fail<T>(string message)
        => new(false, message, default);

    private static OrderDetailResponse MapToDetail(OrderTable order)
    {
        // Address
        AddressInfo? addressInfo = order.Address is null ? null : new AddressInfo(
            order.Address.FullName,
            order.Address.Phone,
            order.Address.Street,
            order.Address.City,
            order.Address.State,
            order.Address.Country);

        // Items
        var items = order.OrderItems.Select(oi =>
        {
            // Lấy ảnh đầu tiên từ chuỗi images (phân cách bởi dấu phẩy)
            var firstImage = oi.Product?.Images?.Split(',').FirstOrDefault()?.Trim();
            return new OrderItemInfo(
                oi.ProductId ?? 0,
                oi.Product?.Title,
                firstImage,
                oi.Quantity ?? 0,
                oi.UnitPrice ?? 0,
                (oi.Quantity ?? 0) * (oi.UnitPrice ?? 0));
        }).ToList();

        // Payment (lấy bản ghi đầu tiên)
        var pay = order.Payments.FirstOrDefault();
        PaymentInfo? paymentInfo = pay is null ? null : new PaymentInfo(
            pay.Method, pay.Amount, pay.Status, pay.PaidAt);

        // Shipping (lấy bản ghi đầu tiên)
        var ship = order.ShippingInfos.FirstOrDefault();
        ShippingStatusInfo? shippingInfo = ship is null ? null : new ShippingStatusInfo(
            ship.Carrier, ship.TrackingNumber, ship.Status, ship.EstimatedArrival);

        // Return Request (lấy bản ghi đầu tiên)
        var rr = order.ReturnRequests.FirstOrDefault();
        ReturnRequestInfo? returnInfo = null;
        if (rr is not null)
        {
            // Tách type ra khỏi prefix [Cancel] / [Return]
            var type   = "Unknown";
            var reason = rr.Reason ?? string.Empty;
            if (reason.StartsWith("[Cancel]"))
            {
                type   = "Cancel";
                reason = reason[8..].Trim();
            }
            else if (reason.StartsWith("[Return]"))
            {
                type   = "Return";
                reason = reason[8..].Trim();
            }

            returnInfo = new ReturnRequestInfo(rr.Id, type, reason, rr.Status, rr.CreatedAt);
        }

        return new OrderDetailResponse(
            order.Id, order.OrderDate, order.Status, order.TotalPrice,
            addressInfo, items, paymentInfo, shippingInfo, returnInfo);
    }
}
