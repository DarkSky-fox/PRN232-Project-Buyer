using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Ebay_Buyer.API.DTOs;
using PRN232_Ebay_Buyer.API.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PRN232_Ebay_Buyer.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly CloneEbayDbContext _db;

        public AdminController(CloneEbayDbContext db)
        {
            _db = db;
        }

        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<ApiResponse<DashboardStatsResponse>>> GetDashboardStats()
        {
            var totalUsers = await _db.Users.CountAsync();
            var totalOrders = await _db.OrderTables.CountAsync();
            var totalProducts = await _db.Products.CountAsync();
            
            // Calculate total revenue from delivered/completed orders
            var totalRevenue = await _db.OrderTables
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

            var stats = new DashboardStatsResponse
            {
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                TotalRevenue = totalRevenue
            };

            return Ok(new ApiResponse<DashboardStatsResponse>(true, "Stats retrieved successfully", stats));
        }

        [HttpGet("recent-orders")]
        public async Task<ActionResult<ApiResponse<List<AdminOrderDto>>>> GetRecentOrders()
        {
            var orders = await _db.OrderTables
                .Include(o => o.Buyer)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new AdminOrderDto
                {
                    OrderId = o.Id,
                    BuyerName = o.Buyer != null ? o.Buyer.Username : "Unknown",
                    TotalPrice = o.TotalPrice ?? 0,
                    Status = o.Status ?? "Unknown",
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            return Ok(new ApiResponse<List<AdminOrderDto>>(true, "Recent orders retrieved successfully", orders));
        }
    }
}
