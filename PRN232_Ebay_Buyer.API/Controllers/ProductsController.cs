using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Ebay_Buyer.API.DTOs;
using PRN232_Ebay_Buyer.API.Models;

namespace PRN232_Ebay_Buyer.API.Controllers;

[ApiController]
[Route("api")]
public class ProductsController : ControllerBase
{
    private readonly CloneEbayDbContext _db;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(CloneEbayDbContext db, ILogger<ProductsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── GET /api/products ──────────────────────────────────────────────────
    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 12;
        if (pageSize > 50) pageSize = 50;

        var query = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .AsQueryable();

        // ── Filter by category ──
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        // ── Filter by search (title) ──
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(p => p.Title != null && p.Title.ToLower().Contains(searchTerm));
        }

        // ── Filter by price range ──
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // ── Sorting ──
        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Title),
            "name_desc" => query.OrderByDescending(p => p.Title),
            "newest" => query.OrderByDescending(p => p.Id),
            _ => query.OrderByDescending(p => p.Id) // default: newest first
        };

        // ── Pagination ──
        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.Images,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.SellerId,
                p.Seller != null ? p.Seller.Username : null,
                p.IsAuction,
                p.AuctionEndTime
            ))
            .ToListAsync();

        var result = new PagedResult<ProductDto>(
            items, page, pageSize, totalItems, totalPages);

        _logger.LogInformation(
            "GetProducts: page={Page}, pageSize={PageSize}, category={Category}, search={Search}, total={Total}",
            page, pageSize, categoryId, search, totalItems);

        return Ok(new ApiResponse<PagedResult<ProductDto>>(
            true, "Products retrieved successfully.", result));
    }

    // ─── GET /api/products/{id} ─────────────────────────────────────────────
    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProduct(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Include(p => p.Reviews).ThenInclude(r => r.Reviewer)
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound(new ApiResponse<ProductDetailDto>(
                false, "Product not found.", null));
        }

        // ── Reviews ──
        var reviews = product.Reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(
                r.Id,
                r.Rating,
                r.Comment,
                r.Reviewer?.Username,
                r.Reviewer?.AvatarUrl,
                r.CreatedAt
            ))
            .ToList();

        var avgRating = product.Reviews.Any()
            ? product.Reviews.Where(r => r.Rating.HasValue).Average(r => r.Rating!.Value)
            : 0;

        // ── Stock ──
        var stock = product.Inventories.Sum(i => i.Quantity ?? 0);

        // ── Seller Info ──
        SellerInfoDto? sellerInfo = null;
        if (product.Seller != null)
        {
            var store = await _db.Stores
                .FirstOrDefaultAsync(s => s.SellerId == product.SellerId);

            var feedback = await _db.Feedbacks
                .FirstOrDefaultAsync(f => f.SellerId == product.SellerId);

            var sellerProductCount = await _db.Products
                .CountAsync(p => p.SellerId == product.SellerId);

            sellerInfo = new SellerInfoDto(
                product.Seller.Id,
                product.Seller.Username,
                product.Seller.AvatarUrl,
                store?.StoreName,
                store?.Description,
                feedback?.AverageRating,
                feedback?.TotalReviews,
                feedback?.PositiveRate,
                sellerProductCount
            );
        }

        // ── Related Products (same category, exclude current) ──
        var relatedProducts = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Seller)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
            .OrderByDescending(p => p.Id)
            .Take(4)
            .Select(p => new ProductDto(
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.Images,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.SellerId,
                p.Seller != null ? p.Seller.Username : null,
                p.IsAuction,
                p.AuctionEndTime
            ))
            .ToListAsync();

        var dto = new ProductDetailDto(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            product.Images,
            product.CategoryId,
            product.Category?.Name,
            product.IsAuction,
            product.AuctionEndTime,
            stock,
            Math.Round(avgRating, 1),
            reviews.Count,
            sellerInfo,
            reviews,
            relatedProducts
        );

        return Ok(new ApiResponse<ProductDetailDto>(true, "Product retrieved successfully.", dto));
    }

    // ─── GET /api/categories ────────────────────────────────────────────────
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Products.Count
            ))
            .ToListAsync();

        return Ok(new ApiResponse<List<CategoryDto>>(
            true, "Categories retrieved successfully.", categories));
    }
}
