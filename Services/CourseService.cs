using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum CourseActionResult
{
    Success,
    NotFound,
    Forbidden,
    InvalidCategory,
    InvalidInstructor,
    HasDependencies
}

public class CourseService
{
    private readonly KhotwaDbContext _context;

    public CourseService(KhotwaDbContext context)
    {
        _context = context;
    }

    // Convert Courses to DTOs
    private static IQueryable<CourseDto> ToDtoQuery(
        IQueryable<Course> query)
    {
        return query.Select(c => new CourseDto
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            Price = c.Price,
            Level = c.Level,
            ThumbnailUrl = c.ThumbnailUrl,
            IsPublished = c.IsPublished,
            CreatedAt = c.CreatedAt,

            CategoryId = c.CategoryId,
            CategoryName = c.Category.Name,

            InstructorId = c.InstructorId,
            InstructorName = c.Instructor.FullName,

            LessonCount = c.Lessons.Count()
        });
    }

    // Shared Search + Filters + Pagination
    private static async Task<PagedResultDto<CourseDto>> GetPagedAsync(
        IQueryable<Course> query,
        string? search,
        int? categoryId,
        string? level,
        decimal? minPrice,
        decimal? maxPrice,
        bool? isPublished,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(c =>
                c.Title.Contains(keyword) ||
                c.Description.Contains(keyword));
        }

        // Category filter
        if (categoryId.HasValue)
        {
            query = query.Where(c =>
                c.CategoryId == categoryId.Value);
        }

        // Level filter
        if (!string.IsNullOrWhiteSpace(level))
        {
            string selectedLevel = level.Trim();

            query = query.Where(c =>
                c.Level == selectedLevel);
        }

        // Price filters
        if (minPrice.HasValue)
        {
            query = query.Where(c =>
                c.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(c =>
                c.Price <= maxPrice.Value);
        }

        // Publication status filter
        if (isPublished.HasValue)
        {
            query = query.Where(c =>
                c.IsPublished == isPublished.Value);
        }

        int totalCount = await query.CountAsync();

        var pagedQuery = query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var courses = await ToDtoQuery(pagedQuery)
            .ToListAsync();

        return new PagedResultDto<CourseDto>
        {
            Items = courses,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // PUBLIC: Published courses only
    public async Task<PagedResultDto<CourseDto>> GetPublishedAsync(
        string? search,
        int? categoryId,
        string? level,
        decimal? minPrice,
        decimal? maxPrice,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished);

        return await GetPagedAsync(
            query,
            search,
            categoryId,
            level,
            minPrice,
            maxPrice,
            true,
            pageNumber,
            pageSize);
    }

    // INSTRUCTOR: Own courses, including drafts
    public async Task<PagedResultDto<CourseDto>> GetMineAsync(
        int instructorId,
        string? search,
        int? categoryId,
        string? level,
        decimal? minPrice,
        decimal? maxPrice,
        bool? isPublished,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.InstructorId == instructorId);

        return await GetPagedAsync(
            query,
            search,
            categoryId,
            level,
            minPrice,
            maxPrice,
            isPublished,
            pageNumber,
            pageSize);
    }

    // ADMIN: All courses
    public async Task<PagedResultDto<CourseDto>> GetAdminAsync(
        string? search,
        int? categoryId,
        string? level,
        decimal? minPrice,
        decimal? maxPrice,
        bool? isPublished,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Courses.AsNoTracking();

        return await GetPagedAsync(
            query,
            search,
            categoryId,
            level,
            minPrice,
            maxPrice,
            isPublished,
            pageNumber,
            pageSize);
    }

    // GET BY ID WITH PERMISSIONS
    public async Task<CourseDto?> GetByIdAsync(
        int id,
        int? userId,
        bool isAdmin,
        bool isInstructor)
    {
        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id);

        if (!isAdmin)
        {
            if (isInstructor && userId.HasValue)
            {
                int currentId = userId.Value;

                query = query.Where(c =>
                    c.IsPublished ||
                    c.InstructorId == currentId);
            }
            else
            {
                query = query.Where(c => c.IsPublished);
            }
        }

        return await ToDtoQuery(query)
            .FirstOrDefaultAsync();
    }

    // CHECK CATEGORY AND INSTRUCTOR
    private async Task<CourseActionResult> ValidateReferencesAsync(
        int categoryId,
        int instructorId)
    {
        bool categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == categoryId);

        if (!categoryExists)
            return CourseActionResult.InvalidCategory;

        bool instructorExists = await _context.Users
            .AnyAsync(u =>
                u.Id == instructorId &&
                u.Role == UserRole.Instructor);

        if (!instructorExists)
            return CourseActionResult.InvalidInstructor;

        return CourseActionResult.Success;
    }

    // CREATE
    public async Task<(CourseActionResult, CourseDto?)> CreateAsync(
        SaveCourseDto dto,
        int currentUserId,
        bool isAdmin)
    {
        // Instructor uses their own ID from JWT.
        // Admin must provide an instructor ID.
        int instructorId;

        if (isAdmin)
        {
            if (!dto.InstructorId.HasValue)
            {
                return (
                    CourseActionResult.InvalidInstructor,
                    null);
            }

            instructorId = dto.InstructorId.Value;
        }
        else
        {
            instructorId = currentUserId;
        }

        var validation = await ValidateReferencesAsync(
            dto.CategoryId,
            instructorId);

        if (validation != CourseActionResult.Success)
            return (validation, null);

        var course = new Course
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            Level = dto.Level.Trim(),
            ThumbnailUrl = dto.ThumbnailUrl,
            IsPublished = dto.IsPublished,
            CategoryId = dto.CategoryId,
            InstructorId = instructorId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var result = await GetByIdAsync(
            course.Id,
            currentUserId,
            isAdmin,
            !isAdmin);

        return (CourseActionResult.Success, result);
    }

    // UPDATE
    public async Task<CourseActionResult> UpdateAsync(
        int id,
        SaveCourseDto dto,
        int currentUserId,
        bool isAdmin)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return CourseActionResult.NotFound;

        // Instructor cannot edit another instructor's course
        if (!isAdmin && course.InstructorId != currentUserId)
            return CourseActionResult.Forbidden;

        // Only admin may change course instructor
        int instructorId = course.InstructorId;

        if (isAdmin && dto.InstructorId.HasValue)
        {
            instructorId = dto.InstructorId.Value;
        }

        var validation = await ValidateReferencesAsync(
            dto.CategoryId,
            instructorId);

        if (validation != CourseActionResult.Success)
            return validation;

        course.Title = dto.Title.Trim();
        course.Description = dto.Description.Trim();
        course.Price = dto.Price;
        course.Level = dto.Level.Trim();
        course.ThumbnailUrl = dto.ThumbnailUrl;
        course.IsPublished = dto.IsPublished;
        course.CategoryId = dto.CategoryId;
        course.InstructorId = instructorId;

        await _context.SaveChangesAsync();

        return CourseActionResult.Success;
    }

    // DELETE
    public async Task<CourseActionResult> DeleteAsync(
        int id,
        int currentUserId,
        bool isAdmin)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return CourseActionResult.NotFound;

        // Check ownership
        if (!isAdmin && course.InstructorId != currentUserId)
            return CourseActionResult.Forbidden;

        // Prevent deleting courses with related records
        bool hasEnrollments = await _context.Enrollments
            .AnyAsync(e => e.CourseId == id);

        bool hasOrders = await _context.OrderItems
            .AnyAsync(i => i.CourseId == id);

        bool hasReviews = await _context.Reviews
            .AnyAsync(r => r.CourseId == id);

        if (hasEnrollments || hasOrders || hasReviews)
            return CourseActionResult.HasDependencies;

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return CourseActionResult.Success;
    }
}
