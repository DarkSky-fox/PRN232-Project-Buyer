using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Ebay_Buyer.API.DTOs;
using PRN232_Ebay_Buyer.API.Models;
using System.Security.Claims;

namespace PRN232_Ebay_Buyer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly CloneEbayDbContext _context;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(CloneEbayDbContext context, ILogger<PaymentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    /// <summary>
    /// GET /api/Payment/paypal/details/{orderId}
    /// Lấy thông tin đơn hàng và thanh toán để hiển thị trên trang Paypal Gateway giả lập.
    /// </summary>
    [HttpGet("paypal/details/{orderId:int}")]
    public async Task<IActionResult> GetPaypalOrderDetails(int orderId)
    {
        var userId = GetUserId();
        var order = await _context.OrderTables
            .Include(o => o.Address)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound(new ApiResponse<string>(false, "Order not found", null));
        }

        if (order.BuyerId != userId)
        {
            return StatusCode(403, new ApiResponse<string>(false, "Forbidden: Order does not belong to user", null));
        }

        var payment = order.Payments.FirstOrDefault(p => p.Method == "PayPal") 
                     ?? order.Payments.FirstOrDefault();

        var details = new PaypalCheckoutDetailsDto
        {
            OrderId = order.Id,
            TotalAmount = order.TotalPrice ?? 0,
            OrderStatus = order.Status ?? "Pending Payment",
            PaymentStatus = payment?.Status ?? "Pending",
            OrderDate = order.OrderDate,
            Address = order.Address == null ? null : new AddressDto
            {
                Id = order.Address.Id,
                FullName = order.Address.FullName ?? "",
                Phone = order.Address.Phone ?? "",
                Street = order.Address.Street ?? "",
                City = order.Address.City ?? "",
                State = order.Address.State ?? "",
                Country = order.Address.Country ?? "",
                IsDefault = order.Address.IsDefault ?? false
            },
            Items = order.OrderItems.Select(oi => new CartItemDto
            {
                ProductId = oi.ProductId ?? 0,
                Title = oi.Product?.Title ?? "Product",
                Price = oi.UnitPrice ?? 0,
                Quantity = oi.Quantity ?? 1,
                ImageUrl = oi.Product?.Images?.Split(',').FirstOrDefault()?.Trim() ?? ""
            }).ToList()
        };

        return Ok(new ApiResponse<PaypalCheckoutDetailsDto>(true, "Success", details));
    }

    /// <summary>
    /// POST /api/Payment/paypal/capture
    /// Xử lý thanh toán thành công qua giả lập PayPal.
    /// </summary>
    [HttpPost("paypal/capture")]
    public async Task<IActionResult> CapturePaypalPayment([FromBody] CapturePaypalPaymentRequest req)
    {
        var userId = GetUserId();
        var order = await _context.OrderTables
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId);

        if (order == null)
        {
            return NotFound(new ApiResponse<string>(false, "Order not found", null));
        }

        if (order.BuyerId != userId)
        {
            return StatusCode(403, new ApiResponse<string>(false, "Forbidden", null));
        }

        var payment = order.Payments.FirstOrDefault(p => p.Method == "PayPal");
        if (payment == null)
        {
            payment = new Payment
            {
                OrderId = order.Id,
                UserId = userId,
                Amount = order.TotalPrice,
                Method = "PayPal",
                Status = "Pending"
            };
            _context.Payments.Add(payment);
        }

        if (payment.Status == "Completed" || payment.Status == "Paid")
        {
            return Ok(new ApiResponse<string>(true, "Payment already completed", "COMPLETED"));
        }

        payment.Status = "Completed";
        payment.PaidAt = DateTime.Now;

        // Cập nhật trạng thái đơn hàng sang Processing
        order.Status = "Processing";

        await _context.SaveChangesAsync();

        _logger.LogInformation("PayPal payment captured successfully for Order #{OrderId} by User #{UserId}", order.Id, userId);

        return Ok(new ApiResponse<object>(true, "PayPal payment completed successfully!", new
        {
            OrderId = order.Id,
            PaymentStatus = payment.Status,
            PaidAt = payment.PaidAt,
            OrderStatus = order.Status
        }));
    }

    /// <summary>
    /// POST /api/Payment/paypal/cancel
    /// Hủy giao dịch thanh toán PayPal.
    /// </summary>
    [HttpPost("paypal/cancel")]
    public async Task<IActionResult> CancelPaypalPayment([FromBody] CancelPaypalPaymentRequest req)
    {
        var userId = GetUserId();
        var order = await _context.OrderTables
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == req.OrderId);

        if (order == null)
        {
            return NotFound(new ApiResponse<string>(false, "Order not found", null));
        }

        if (order.BuyerId != userId)
        {
            return StatusCode(403, new ApiResponse<string>(false, "Forbidden", null));
        }

        var payment = order.Payments.FirstOrDefault(p => p.Method == "PayPal");
        if (payment != null && payment.Status != "Completed")
        {
            payment.Status = "Cancelled";
        }

        if (order.Status == "Pending Payment" || order.Status == "Pending")
        {
            order.Status = "Cancelled";
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<string>(true, "PayPal payment cancelled", "CANCELLED"));
    }
}
