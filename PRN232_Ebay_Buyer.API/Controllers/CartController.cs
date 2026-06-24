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
public class CartController : ControllerBase
{
    private readonly CloneEbayDbContext _context;

    public CartController(CloneEbayDbContext context)
    {
        _context = context;
    }

    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var cartOrder = await _context.OrderTables
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.BuyerId == userId && o.Status == "Cart");

        if (cartOrder == null)
            return Ok(new ApiResponse<List<CartItemDto>>(true, "Success", new List<CartItemDto>()));

        var items = cartOrder.OrderItems.Select(oi => new CartItemDto
        {
            ProductId = oi.ProductId ?? 0,
            Quantity = oi.Quantity ?? 0,
            Price = oi.UnitPrice ?? 0,
            Title = oi.Product?.Title ?? "",
            ImageUrl = oi.Product?.Images?.Split(',').FirstOrDefault() ?? ""
        }).ToList();

        return Ok(new ApiResponse<List<CartItemDto>>(true, "Success", items));
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncCart([FromBody] SyncCartRequest request)
    {
        var userId = GetUserId();
        var cartOrder = await _context.OrderTables
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.BuyerId == userId && o.Status == "Cart");

        if (cartOrder == null)
        {
            cartOrder = new OrderTable
            {
                BuyerId = userId,
                OrderDate = DateTime.Now,
                Status = "Cart",
                TotalPrice = 0
            };
            _context.OrderTables.Add(cartOrder);
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.OrderItems.RemoveRange(cartOrder.OrderItems);
            await _context.SaveChangesAsync();
        }

        decimal total = 0;
        foreach (var item in request.Items)
        {
            _context.OrderItems.Add(new OrderItem
            {
                OrderId = cartOrder.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            });
            total += item.Price * item.Quantity;
        }

        cartOrder.TotalPrice = total;
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<string>(true, "Synced", null));
    }
}
