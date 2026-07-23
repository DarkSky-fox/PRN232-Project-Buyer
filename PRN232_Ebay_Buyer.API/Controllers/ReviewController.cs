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

    // ── Helper: lấy userId từ JWT claim ────────────────────────────────────
    private int GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    // ── Helper: map Review entity → ReviewResponse ──────────────────────────
    private static ReviewResponse MapToResponse(Review r) => new(
        r.Id,
        r.ProductId,
        r.ReviewerId,
        r.Reviewer?.Username,
        r.Reviewer?.AvatarUrl,
        r.Rating,
        r.Comment,
        r.CreatedAt);

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/reviews                                          [Authorize]
    // Tạo đánh giá mới — điều kiện: đã mua & status = "Delivered",
    // chưa từng review sản phẩm này.
    // ────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ReviewResponse>>> CreateReview(
        [FromBody] CreateReviewRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new ApiResponse<ReviewResponse>(false, "Unauthorized.", null));

        // 1. Validate Rating
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new ApiResponse<ReviewResponse>(
                false, "Rating phải từ 1 đến 5.", null));

        // 2. Kiểm tra sản phẩm tồn tại
        var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId);
        if (!productExists)
            return NotFound(new ApiResponse<ReviewResponse>(
                false, "Sản phẩm không tồn tại.", null));

        // 3. Kiểm tra buyer đã mua & đã nhận hàng (status = "Delivered")
        bool hasPurchased = await _db.OrderItems
            .AnyAsync(oi =>
                oi.ProductId == request.ProductId &&
                oi.Order != null &&
                oi.Order.BuyerId == userId &&
                oi.Order.Status == "Delivered");

        if (!hasPurchased)
            return BadRequest(new ApiResponse<ReviewResponse>(
                false, "Bạn chỉ có thể đánh giá sản phẩm đã mua và đã nhận.", null));

        // 4. Kiểm tra chưa từng review sản phẩm này (1 lần duy nhất)
        bool alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.ProductId == request.ProductId && r.ReviewerId == userId);

        if (alreadyReviewed)
            return Conflict(new ApiResponse<ReviewResponse>(
                false, "Bạn đã đánh giá sản phẩm này, hãy chỉnh sửa đánh giá cũ.", null));

        // 5. Tạo Review
        var review = new Review
        {
            ProductId  = request.ProductId,
            ReviewerId = userId,
            Rating     = request.Rating,
            Comment    = request.Comment?.Trim(),
            CreatedAt  = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        // Load reviewer info để trả về response
        await _db.Entry(review).Reference(r => r.Reviewer).LoadAsync();

        _logger.LogInformation(
            "User {UserId} created review {ReviewId} for product {ProductId}.",
            userId, review.Id, request.ProductId);

        return CreatedAtAction(
            nameof(GetProductReviews),
            new { productId = review.ProductId },
            new ApiResponse<ReviewResponse>(
                true, "Đánh giá đã được ghi nhận.", MapToResponse(review)));
    }

    // ────────────────────────────────────────────────────────────────────────
    // PUT /api/reviews/{id}                                      [Authorize]
    // Sửa đánh giá — chỉ reviewer gốc mới được sửa.
    // ────────────────────────────────────────────────────────────────────────
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ReviewResponse>>> UpdateReview(
        int id,
        [FromBody] UpdateReviewRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
            return Unauthorized(new ApiResponse<ReviewResponse>(false, "Unauthorized.", null));

        // 1. Tìm review
        var review = await _db.Reviews
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
            return NotFound(new ApiResponse<ReviewResponse>(
                false, "Không tìm thấy đánh giá.", null));

        // 2. Kiểm tra quyền sở hữu
        if (review.ReviewerId != userId)
            return StatusCode(403, new ApiResponse<ReviewResponse>(
                false, "Không được sửa đánh giá của người khác.", null));

        // 3. Validate Rating
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new ApiResponse<ReviewResponse>(
                false, "Rating phải từ 1 đến 5.", null));

        // 4. Cập nhật
        review.Rating  = request.Rating;
        review.Comment = request.Comment?.Trim();

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} updated review {ReviewId} for product {ProductId}.",
            userId, review.Id, review.ProductId);

        return Ok(new ApiResponse<ReviewResponse>(
            true, "Đánh giá đã được cập nhật.", MapToResponse(review)));
    }

    // ────────────────────────────────────────────────────────────────────────
    // GET /api/reviews/product/{productId}                       [AllowAnonymous]
    // Lấy tất cả đánh giá của 1 sản phẩm — public, không cần đăng nhập.
    // ────────────────────────────────────────────────────────────────────────
    [HttpGet("product/{productId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ReviewResponse>>>> GetProductReviews(int productId)
    {
        // 1. Kiểm tra sản phẩm tồn tại
        var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
            return NotFound(new ApiResponse<List<ReviewResponse>>(
                false, "Sản phẩm không tồn tại.", null));

        // 2. Lấy danh sách review, sắp xếp mới nhất lên đầu
        var reviews = await _db.Reviews
            .Where(r => r.ProductId == productId)
            .Include(r => r.Reviewer)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponse(
                r.Id,
                r.ProductId,
                r.ReviewerId,
                r.Reviewer != null ? r.Reviewer.Username : null,
                r.Reviewer != null ? r.Reviewer.AvatarUrl : null,
                r.Rating,
                r.Comment,
                r.CreatedAt))
            .ToListAsync();

        return Ok(new ApiResponse<List<ReviewResponse>>(true, "Success", reviews));
    }
}
