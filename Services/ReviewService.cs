using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum ReviewActionResult
{
    Success,
    NotFound,
    CourseUnavailable,
    NotEnrolled,
    AlreadyReviewed,
    Forbidden
}

public class ReviewService
{
    private readonly KhotwaDbContext _context;

    public ReviewService(KhotwaDbContext context)
    {
        _context = context;
    }

    // Convert Review to DTO
    private static IQueryable<ReviewDto> ToDtoQuery(
        IQueryable<Review> query)
    {
        return query.Select(r => new ReviewDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            StudentName = r.Student.FullName,
            CourseId = r.CourseId,
            CourseTitle = r.Course.Title,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        });
    }

    // Reusable Pagination
    private static async Task<PagedResultDto<ReviewDto>>
        GetPagedAsync(
            IQueryable<Review> query,
            int pageNumber,
            int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        int totalCount = await query.CountAsync();

        var pagedQuery = query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await ToDtoQuery(pagedQuery)
            .ToListAsync();

        return new PagedResultDto<ReviewDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // GET ALL + SEARCH + FILTER + PAGINATION
    public async Task<PagedResultDto<ReviewDto>> GetAllAsync(
        int? courseId,
        int? rating,
        string? search,
        int pageNumber,
        int pageSize,
        bool isAdmin)
    {
        var query = _context.Reviews.AsNoTracking();

        // Visitors see reviews of published courses only
        if (!isAdmin)
        {
            query = query.Where(r => r.Course.IsPublished);
        }

        if (courseId.HasValue)
        {
            query = query.Where(r =>
                r.CourseId == courseId.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r =>
                r.Rating == rating.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(r =>
                r.Student.FullName.Contains(keyword) ||
                r.Course.Title.Contains(keyword) ||
                (r.Comment != null &&
                 r.Comment.Contains(keyword)));
        }

        return await GetPagedAsync(
            query, pageNumber, pageSize);
    }

    // GET MY REVIEWS
    public async Task<PagedResultDto<ReviewDto>> GetMineAsync(
        int studentId,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.StudentId == studentId);

        return await GetPagedAsync(
            query, pageNumber, pageSize);
    }

    // GET BY ID
    public async Task<ReviewDto?> GetByIdAsync(
        int id,
        int? userId,
        bool isAdmin)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.Id == id);

        if (!isAdmin)
        {
            query = query.Where(r =>
                r.Course.IsPublished ||
                (userId.HasValue &&
                 r.StudentId == userId.Value));
        }

        return await ToDtoQuery(query)
            .FirstOrDefaultAsync();
    }

    // CREATE
    public async Task<(ReviewActionResult, ReviewDto?)>
        CreateAsync(
            SaveReviewDto dto,
            int studentId)
    {
        bool courseExists = await _context.Courses
            .AnyAsync(c =>
                c.Id == dto.CourseId &&
                c.IsPublished);

        if (!courseExists)
            return (ReviewActionResult.CourseUnavailable, null);

        bool enrolled = await _context.Enrollments
            .AnyAsync(e =>
                e.CourseId == dto.CourseId &&
                e.StudentId == studentId);

        if (!enrolled)
            return (ReviewActionResult.NotEnrolled, null);

        bool alreadyReviewed = await _context.Reviews
            .AnyAsync(r =>
                r.CourseId == dto.CourseId &&
                r.StudentId == studentId);

        if (alreadyReviewed)
            return (ReviewActionResult.AlreadyReviewed, null);

        var review = new Review
        {
            CourseId = dto.CourseId,
            StudentId = studentId,
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var result = await GetByIdAsync(
            review.Id, studentId, false);

        return (ReviewActionResult.Success, result);
    }

    // UPDATE
    public async Task<ReviewActionResult> UpdateAsync(
        int id,
        UpdateReviewDto dto,
        int studentId)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return ReviewActionResult.NotFound;

        // Student can edit their own review only
        if (review.StudentId != studentId)
            return ReviewActionResult.Forbidden;

        review.Rating = dto.Rating;
        review.Comment = dto.Comment?.Trim();

        await _context.SaveChangesAsync();

        return ReviewActionResult.Success;
    }

    // DELETE
    public async Task<ReviewActionResult> DeleteAsync(
        int id,
        int userId,
        bool isAdmin)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return ReviewActionResult.NotFound;

        if (!isAdmin && review.StudentId != userId)
            return ReviewActionResult.Forbidden;

        _context.Reviews.Remove(review);

        await _context.SaveChangesAsync();

        return ReviewActionResult.Success;
    }
}
