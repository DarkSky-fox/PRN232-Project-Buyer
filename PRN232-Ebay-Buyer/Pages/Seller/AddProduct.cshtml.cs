using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PRN232_Ebay_Buyer.API.DTOs;

namespace PRN232_Ebay_Buyer.Pages.Seller;

public class AddProductModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AddProductModel> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AddProductModel(
        IHttpClientFactory httpClientFactory, 
        ILogger<AddProductModel> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    [BindProperty]
    [Required(ErrorMessage = "Product title is required.")]
    [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Product description is required.")]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 10000000.0, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 10000, ErrorMessage = "Quantity must be at least 1.")]
    public int StockQuantity { get; set; } = 1;

    [BindProperty]
    public bool IsAuction { get; set; }

    [BindProperty]
    public DateTime? AuctionEndTime { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please upload at least one product image.")]
    public IFormFile? ProductImageFile { get; set; }

    public List<CategoryDto> Categories { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Auth/Login");
        }

        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Auth/Login");
        }

        await LoadCategoriesAsync();

        // ── Custom conditional validation for Auction ──
        if (IsAuction)
        {
            if (AuctionEndTime == null)
            {
                ModelState.AddModelError(nameof(AuctionEndTime), "Auction end time is required when auction mode is active.");
            }
            else if (AuctionEndTime <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(AuctionEndTime), "Auction end time must be a future date and time.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Message = "Session expired. Please log in again.";
                IsSuccess = false;
                return Page();
            }

            string? imageUrl = null;

            // ── Upload Product Image ──
            if (ProductImageFile != null)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProductImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProductImageFile.CopyToAsync(fileStream);
                }

                imageUrl = "/uploads/" + uniqueFileName;
            }

            var payload = new CreateProductRequest(
                Title.Trim(),
                Description.Trim(),
                Price,
                imageUrl,
                CategoryId,
                int.Parse(userId),
                IsAuction,
                IsAuction ? AuctionEndTime?.ToUniversalTime() : null,
                StockQuantity
            );

            var client = _httpClientFactory.CreateClient("AuthApi");
            
            // Forward JWT Token
            var token = HttpContext.Request.Cookies["BearerToken"];
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await client.PostAsJsonAsync("/api/products", payload);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("/Seller/MyProducts");
            }
            else
            {
                var errorContent = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                Message = errorContent?.Message ?? $"Failed to list product. API returned status: {response.StatusCode}";
                IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while posting product");
            Message = "Unable to connect to server. Please try again later.";
            IsSuccess = false;
        }

        return Page();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthApi");
            var response = await client.GetAsync("/api/categories");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<List<CategoryDto>>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result?.Data != null)
                {
                    Categories = result.Data;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories for select dropdown");
        }
    }
}
