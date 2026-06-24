using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN232_Ebay_Buyer.API.DTOs;
using PRN232_Ebay_Buyer.API.Models;
using System.Security.Claims;

namespace PRN232_Ebay_Buyer.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly CloneEbayDbContext _db;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(CloneEbayDbContext db, ILogger<ReviewController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ─── POST /api/reviews ──────────────────────────────────────────────────────
    /// <summary>
    /// Gửi đánh giá sản phẩm. Buyer chỉ được đánh giá sản phẩm đã mua và đã nhận hàng.
    /// Mỗi buyer chỉ được đánh giá 1 lần/sản phẩm.
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReviewResponse>>> CreateReview(
        [FromBody] CreateReviewRequest dto)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(Fail<ReviewResponse>("Token không hợp lệ."));

        // Validate rating
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest(Fail<ReviewResponse>("Rating phải nằm trong khoảng từ 1 đến 5."));

        // Kiểm tra sản phẩm tồn tại
        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product is null)
            return NotFound(Fail<ReviewResponse>("Sản phẩm không tồn tại."));

        // Kiểm tra buyer đã mua và đã nhận sản phẩm này
        var hasPurchased = await _db.OrderItems
            .AnyAsync(oi =>
                oi.ProductId == dto.ProductId &&
                oi.Order != null &&
                oi.Order.BuyerId == userId &&
                oi.Order.Status == "Delivered");

        if (!hasPurchased)
        {
            return BadRequest(Fail<ReviewResponse>(
                "Bạn chỉ có thể đánh giá sản phẩm đã mua và đã nhận hàng thành công."));
        }

        // Kiểm tra chưa review sản phẩm này
        var existingReview = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == dto.ProductId && r.ReviewerId == userId);

        if (existingReview is not null)
        {
            return Conflict(Fail<ReviewResponse>(
                "Bạn đã đánh giá sản phẩm này rồi. Hãy sử dụng chức năng chỉnh sửa đánh giá."));
        }

        var review = new Review
        {
            ProductId  = dto.ProductId,
            ReviewerId = userId,
            Rating     = dto.Rating,
            Comment    = dto.Comment?.Trim(),
            CreatedAt  = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        // Load reviewer info để trả về
        await _db.Entry(review).Reference(r => r.Reviewer).LoadAsync();

        _logger.LogInformation(
            "Review #{ReviewId} tạo bởi user {UserId} cho sản phẩm #{ProductId}.",
            review.Id, userId, dto.ProductId);

        return CreatedAtAction(
            nameof(GetProductReviews),
            new { productId = dto.ProductId },
            new ApiResponse<ReviewResponse>(
                true, "Đánh giá của bạn đã được gửi thành công.", MapToResponse(review)));
    }

    // ─── PUT /api/reviews/{id} ──────────────────────────────────────────────────
    /// <summary>
    /// Chỉnh sửa đánh giá đã gửi. Chỉ chủ nhân đánh giá mới được sửa.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ReviewResponse>>> UpdateReview(
        int id,
        [FromBody] UpdateReviewRequest dto)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(Fail<ReviewResponse>("Token không hợp lệ."));

        // Validate rating
        if (dto.Rating < 1 || dto.Rating > 5)
            return BadRequest(Fail<ReviewResponse>("Rating phải nằm trong khoảng từ 1 đến 5."));

        var review = await _db.Reviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
            return NotFound(Fail<ReviewResponse>("Không tìm thấy đánh giá."));

        if (review.ReviewerId != userId)
            return StatusCode(403, Fail<ReviewResponse>("Bạn không có quyền chỉnh sửa đánh giá này."));

        review.Rating  = dto.Rating;
        review.Comment = dto.Comment?.Trim();

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Review #{ReviewId} được cập nhật bởi user {UserId}.", review.Id, userId);

        return Ok(new ApiResponse<ReviewResponse>(
            true, "Đánh giá đã được cập nhật thành công.", MapToResponse(review)));
    }

    // ─── GET /api/reviews/product/{productId} ───────────────────────────────────
    /// <summary>
    /// Lấy tất cả đánh giá của một sản phẩm. Không cần đăng nhập.
    /// </summary>
    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<ApiResponse<List<ReviewResponse>>>> GetProductReviews(int productId)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product is null)
            return NotFound(Fail<List<ReviewResponse>>("Sản phẩm không tồn tại."));

        var reviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = reviews.Select(MapToResponse).ToList();

        return Ok(new ApiResponse<List<ReviewResponse>>(
            true, $"Lấy {result.Count} đánh giá thành công.", result));
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private static ApiResponse<T> Fail<T>(string message)
        => new(false, message, default);

    private static ReviewResponse MapToResponse(Review r) => new(
        r.Id,
        r.ProductId,
        r.ReviewerId,
        r.Reviewer?.Username,
        r.Reviewer?.AvatarUrl,
        r.Rating,
        r.Comment,
        r.CreatedAt);
}
