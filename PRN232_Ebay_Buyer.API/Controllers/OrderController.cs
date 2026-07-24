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
public class OrderController : ControllerBase
{
    private readonly CloneEbayDbContext _context;

    public OrderController(CloneEbayDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses()
    {
        var userId = GetUserId();
        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                FullName = a.FullName ?? "",
                Phone = a.Phone ?? "",
                Street = a.Street ?? "",
                City = a.City ?? "",
                State = a.State ?? "",
                Country = a.Country ?? "",
                IsDefault = a.IsDefault ?? false
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<AddressDto>>(true, "Success", addresses));
    }

    [HttpPost("address")]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest req)
    {
        var userId = GetUserId();

        if (req.IsDefault)
        {
            var existingDefault = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault == true)
                .ToListAsync();
            foreach (var addr in existingDefault)
            {
                addr.IsDefault = false;
            }
        }

        var address = new Address
        {
            UserId = userId,
            FullName = req.FullName,
            Phone = req.Phone,
            Street = req.Street,
            City = req.City,
            State = req.State,
            Country = req.Country,
            IsDefault = req.IsDefault
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<int>(true, "Address created", address.Id));
    }

    [HttpGet("validate-coupon")]
    public async Task<IActionResult> ValidateCoupon([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new ApiResponse<string>(false, "Coupon code is required", null));

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code);
        
        if (coupon == null)
            return Ok(new { valid = false, message = "Invalid coupon code." });

        if (coupon.StartDate.HasValue && coupon.StartDate.Value > DateTime.Now)
            return Ok(new { valid = false, message = "Coupon is not yet active." });

        if (coupon.EndDate.HasValue && coupon.EndDate.Value < DateTime.Now)
            return Ok(new { valid = false, message = "Coupon has expired." });

        return Ok(new { valid = true, discountPercent = coupon.DiscountPercent, message = "Coupon applied successfully!" });
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest req)
    {
        var userId = GetUserId();

        if (req.Items == null || !req.Items.Any())
            return BadRequest(new ApiResponse<string>(false, "Cart is empty", null));

        if (req.AddressId <= 0)
            return BadRequest(new ApiResponse<string>(false, "Invalid address", null));

        string paymentMethod = string.Equals(req.PaymentMethod, "PayPal", StringComparison.OrdinalIgnoreCase) ? "PayPal" : "COD";
        decimal total = req.Items.Sum(i => i.Price * i.Quantity);

        if (!string.IsNullOrWhiteSpace(req.CouponCode))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == req.CouponCode);
            if (coupon != null &&
                (!coupon.StartDate.HasValue || coupon.StartDate.Value <= DateTime.Now) &&
                (!coupon.EndDate.HasValue || coupon.EndDate.Value >= DateTime.Now))
            {
                if (coupon.DiscountPercent.HasValue)
                {
                    decimal discount = total * (coupon.DiscountPercent.Value / 100m);
                    total -= discount;
                }
            }
        }

        string initialOrderStatus = paymentMethod == "PayPal" ? "Pending Payment" : "Pending";

        // Process cart (either new order or update the existing "Cart" status order)
        var cartOrder = await _context.OrderTables
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.BuyerId == userId && o.Status == "Cart");

        OrderTable order;
        if (cartOrder != null)
        {
            order = cartOrder;
            order.Status = initialOrderStatus;
            order.AddressId = req.AddressId;
            order.OrderDate = DateTime.Now;
            order.TotalPrice = total;
            
            // clear old items and re-add from request to be safe
            _context.OrderItems.RemoveRange(order.OrderItems);
        }
        else
        {
            order = new OrderTable
            {
                BuyerId = userId,
                AddressId = req.AddressId,
                OrderDate = DateTime.Now,
                TotalPrice = total,
                Status = initialOrderStatus
            };
            _context.OrderTables.Add(order);
        }

        await _context.SaveChangesAsync(); // save to get order.Id

        foreach (var item in req.Items)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            });
            // Update stock quantity
            var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == item.ProductId);
            if (inventory != null && inventory.Quantity >= item.Quantity)
            {
                inventory.Quantity -= item.Quantity;
            }
        }

        var payment = new Payment
        {
            OrderId = order.Id,
            UserId = userId,
            Amount = total,
            Method = paymentMethod,
            Status = "Pending",
            PaidAt = null
        };
        _context.Payments.Add(payment);

        await _context.SaveChangesAsync();

        var responseData = new CheckoutResponse
        {
            OrderId = order.Id,
            PaymentMethod = paymentMethod,
            PaypalRedirectUrl = paymentMethod == "PayPal" ? $"/Order/PaypalMock?orderId={order.Id}" : null
        };

        return Ok(new ApiResponse<CheckoutResponse>(true, "Order placed successfully", responseData));
    }
}

