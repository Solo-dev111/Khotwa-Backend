
using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum DeleteCategoryResult
{
    Deleted,
    NotFound,
    HasCourses
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
                Description = c.Description
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
                Description = c.Description
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
            Description = category.Description
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

    // DELETE
    public async Task<DeleteCategoryResult> DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return DeleteCategoryResult.NotFound;

        // Do not delete categories containing courses
        bool hasCourses = await _context.Courses
            .AnyAsync(c => c.CategoryId == id);

        if (hasCourses)
            return DeleteCategoryResult.HasCourses;

        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        return DeleteCategoryResult.Deleted;
    }
}
