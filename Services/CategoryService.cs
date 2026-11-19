
using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Khotwa.Services;

public enum DeleteCategoryResult
{
    Deleted,
    NotFound,
    HasCourses,
    InvalidTarget,
    Changed
}

public class CategoryService
{
    private readonly KhotwaDbContext _context;

    public CategoryService(KhotwaDbContext context)
    {
        _context = context;
    }

    // GET ALL + SEARCH + PAGINATION
    public async Task<PagedResultDto<CategoryDto>> GetAllAsync(
        string? search,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Categories.AsNoTracking();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(c =>
                c.Name.Contains(keyword) ||
                (c.Description != null &&
                 c.Description.Contains(keyword)));
        }

        // Total records before pagination
        int totalCount = await query.CountAsync();

        // Pagination
        var categories = await query
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CourseCount = c.Courses.Count()
            })
            .ToListAsync();

        return new PagedResultDto<CategoryDto>
        {
            Items = categories,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // GET BY ID
    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CourseCount = c.Courses.Count()
            })
            .FirstOrDefaultAsync();
    }

    // CREATE
    public async Task<CategoryDto> CreateAsync(
        SaveCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name.Trim(),
            Description = dto.Description
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CourseCount = 0
        };
    }

    // UPDATE
    public async Task<bool> UpdateAsync(
        int id,
        SaveCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return false;

        category.Name = dto.Name.Trim();
        category.Description = dto.Description;

        await _context.SaveChangesAsync();

        return true;
    }

    // All possible destinations, independent of the current table search or page.
    public async Task<List<CategoryDto>> GetTransferTargetsAsync(int id)
    {
        return await _context.Categories.AsNoTracking()
            .Where(c => c.Id != id)
            .OrderBy(c => c.Name).ThenBy(c => c.Id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CourseCount = c.Courses.Count()
            })
            .ToListAsync();
    }

    // Move courses and delete the category atomically; never delete course data.
    public async Task<DeleteCategoryResult> DeleteAsync(
        int id, int? targetCategoryId = null, int? expectedCourseCount = null)
    {
        if (targetCategoryId.HasValue &&
            (targetCategoryId.Value <= 0 || targetCategoryId.Value == id))
            return DeleteCategoryResult.InvalidTarget;

        try
        {
            await using var transaction = await _context.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            if (!await _context.Categories.AnyAsync(c => c.Id == id))
                return DeleteCategoryResult.NotFound;

            var courses = _context.Courses.Where(c => c.CategoryId == id);
            int courseCount = await courses.CountAsync();

            if (expectedCourseCount.HasValue && expectedCourseCount.Value != courseCount)
                return DeleteCategoryResult.Changed;

            if (courseCount > 0 && !targetCategoryId.HasValue)
                return DeleteCategoryResult.HasCourses;

            if (targetCategoryId.HasValue)
            {
                int target = targetCategoryId.Value;
                if (!await _context.Categories.AnyAsync(c => c.Id == target))
                    return DeleteCategoryResult.Changed;

                if (courseCount > 0)
                    await courses.ExecuteUpdateAsync(setters => setters
                        .SetProperty(c => c.CategoryId, target));
            }

            int deleted = await _context.Categories
                .Where(c => c.Id == id).ExecuteDeleteAsync();

            if (deleted != 1)
                return DeleteCategoryResult.Changed;

            await transaction.CommitAsync();
            return DeleteCategoryResult.Deleted;
        }
        catch (SqlException ex) when (ex.Number is 547 or 1205 or 1222)
        {
            // FK conflict, deadlock or lock timeout: transaction is disposed/rolled back.
            return DeleteCategoryResult.Changed;
        }
    }
}
