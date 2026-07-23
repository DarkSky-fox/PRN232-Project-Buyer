using System;

namespace PRN232_Ebay_Buyer.API.DTOs
{
    public class DashboardStatsResponse
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AdminOrderDto
    {
        public int OrderId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
    }
}
