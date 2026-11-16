using Khotwa.DTOs;
using Khotwa.Models;
using Khotwa.Data;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum StudentProfileResult
{
    Success,
    NotFound,
    InvalidStudent,
    AlreadyExists
}

public class StudentProfileService
{
    private readonly KhotwaDbContext _context;

    public StudentProfileService(
        KhotwaDbContext context)
    {
        _context = context;
    }

    // Convert profile to DTO
    private static IQueryable<StudentProfileDto> ToDtoQuery(
        IQueryable<StudentProfile> query)
    {
        return query.Select(p => new StudentProfileDto
        {
            Id = p.Id,
            UserId = p.UserId,
            FullName = p.User.FullName,
            Email = p.User.Email,
            Bio = p.Bio,
            ProfileImageUrl = p.ProfileImageUrl
        });
    }

    // GET ALL + SEARCH + PAGINATION (Admin)
    public async Task<PagedResultDto<StudentProfileDto>>
        GetAllAsync(
            string? search,
            int pageNumber,
            int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.StudentProfiles
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(p =>
                p.User.FullName.Contains(keyword) ||
                p.User.Email.Contains(keyword) ||
                (p.Bio != null &&
                 p.Bio.Contains(keyword)));
        }

        int totalCount = await query.CountAsync();

        var pagedQuery = query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await ToDtoQuery(pagedQuery)
            .ToListAsync();

        return new PagedResultDto<StudentProfileDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // GET BY PROFILE ID
    public async Task<StudentProfileDto?> GetByIdAsync(int id)
    {
        return await ToDtoQuery(
            _context.StudentProfiles
                .AsNoTracking()
                .Where(p => p.Id == id))
            .FirstOrDefaultAsync();
    }

    // GET MY PROFILE
    public async Task<StudentProfileDto?> GetMineAsync(
        int userId)
    {
        return await ToDtoQuery(
            _context.StudentProfiles
                .AsNoTracking()
                .Where(p => p.UserId == userId))
            .FirstOrDefaultAsync();
    }

    // CREATE PROFILE
    public async Task<(StudentProfileResult, StudentProfileDto?)>
        CreateAsync(
            int userId,
            SaveStudentProfileDto dto)
    {
        bool validStudent = await _context.Users
            .AnyAsync(u =>
                u.Id == userId &&
                u.Role == UserRole.Student);

        if (!validStudent)
        {
            return (StudentProfileResult.InvalidStudent, null);
        }

        bool exists = await _context.StudentProfiles
            .AnyAsync(p => p.UserId == userId);

        if (exists)
        {
            return (StudentProfileResult.AlreadyExists, null);
        }

        var profile = new StudentProfile
        {
            UserId = userId,
            Bio = dto.Bio?.Trim(),
            ProfileImageUrl = dto.ProfileImageUrl?.Trim()
        };

        _context.StudentProfiles.Add(profile);
        await _context.SaveChangesAsync();

        var result = await GetByIdAsync(profile.Id);

        return (StudentProfileResult.Success, result);
    }

    // UPDATE BY PROFILE ID (Admin)
    public async Task<StudentProfileResult> UpdateAsync(
        int id,
        SaveStudentProfileDto dto)
    {
        var profile = await _context.StudentProfiles
            .FindAsync(id);

        return await SaveChangesAsync(profile, dto);
    }

    // UPDATE MY PROFILE
    public async Task<StudentProfileResult> UpdateMineAsync(
        int userId,
        SaveStudentProfileDto dto)
    {
        var profile = await _context.StudentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return await SaveChangesAsync(profile, dto);
    }

    // Shared update logic
    private async Task<StudentProfileResult> SaveChangesAsync(
        StudentProfile? profile,
        SaveStudentProfileDto dto)
    {
        if (profile == null)
            return StudentProfileResult.NotFound;

        profile.Bio = dto.Bio?.Trim();
        profile.ProfileImageUrl =
            dto.ProfileImageUrl?.Trim();

        await _context.SaveChangesAsync();

        return StudentProfileResult.Success;
    }

    // DELETE BY PROFILE ID (Admin)
    public async Task<StudentProfileResult> DeleteAsync(int id)
    {
        var profile = await _context.StudentProfiles
            .FindAsync(id);

        return await RemoveAsync(profile);
    }

    // DELETE MY PROFILE
    public async Task<StudentProfileResult> DeleteMineAsync(
        int userId)
    {
        var profile = await _context.StudentProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return await RemoveAsync(profile);
    }

    // Shared delete logic
    private async Task<StudentProfileResult> RemoveAsync(
        StudentProfile? profile)
    {
        if (profile == null)
            return StudentProfileResult.NotFound;

        _context.StudentProfiles.Remove(profile);

        await _context.SaveChangesAsync();

        return StudentProfileResult.Success;
    }
}
