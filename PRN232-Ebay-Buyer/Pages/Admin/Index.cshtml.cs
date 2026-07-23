using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PRN232_Ebay_Buyer.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public DashboardStatsResponse? Stats { get; set; }
        public List<AdminOrderDto> RecentOrders { get; set; } = new();

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Request.Cookies["BearerToken"];

            if (string.IsNullOrEmpty(token) || !User.IsInRole("Admin"))
            {
                // Not logged in or not admin, redirect to home or login
                return RedirectToPage("/Index");
            }

            var client = _httpClientFactory.CreateClient("AuthApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                // Get Stats
                var statsResponse = await client.GetAsync("/api/admin/dashboard-stats");
                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsContent = await statsResponse.Content.ReadAsStringAsync();
                    var statsApiRes = JsonSerializer.Deserialize<ApiResponse<DashboardStatsResponse>>(statsContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Stats = statsApiRes?.Data;
                }

                // Get Recent Orders
                var ordersResponse = await client.GetAsync("/api/admin/recent-orders");
                if (ordersResponse.IsSuccessStatusCode)
                {
                    var ordersContent = await ordersResponse.Content.ReadAsStringAsync();
                    var ordersApiRes = JsonSerializer.Deserialize<ApiResponse<List<AdminOrderDto>>>(ordersContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (ordersApiRes?.Data != null)
                    {
                        RecentOrders = ordersApiRes.Data;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard stats.");
            }

            return Page();
        }
    }
}
