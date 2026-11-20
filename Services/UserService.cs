using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum UserOperationResult
{
    Success,
    NotFound,
    DuplicateEmail,
    HasRelatedRecords,
    CannotChangeOwnRole,
    CannotDeleteSelf
}

public class UserService
{
    private readonly KhotwaDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        KhotwaDbContext context,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // Convert User to DTO
    private static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }

    // GET ALL + SEARCH + FILTER + PAGINATION
    public async Task<PagedResultDto<UserDto>> GetAllAsync(
        string? search,
        UserRole? role,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();

            query = query.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        int totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<UserDto>
        {
            Items = users.Select(ToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // GET BY ID
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return user == null ? null : ToDto(user);
    }

    // CREATE
    public async Task<(UserDto?, UserOperationResult)> CreateAsync(
        CreateUserDto dto,
        UserRole role)
    {
        string email = dto.Email.Trim().ToLowerInvariant();

        bool exists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (exists)
            return (null, UserOperationResult.DuplicateEmail);

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Role = role
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user, dto.Password);

        if (role == UserRole.Student)
        {
            user.StudentProfile = new StudentProfile();
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (ToDto(user), UserOperationResult.Success);
    }

    // UPDATE
    public async Task<UserOperationResult> UpdateAsync(
        int id,
        UpdateUserDto dto,
        UserRole newRole,
        int currentAdminId)
    {
        var user = await _context.Users
            .Include(u => u.StudentProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return UserOperationResult.NotFound;

        string email = dto.Email.Trim().ToLowerInvariant();

        bool emailExists = await _context.Users
            .AnyAsync(u => u.Id != id && u.Email == email);

        if (emailExists)
            return UserOperationResult.DuplicateEmail;

        // Check role changes
        if (user.Role != newRole)
        {
            if (id == currentAdminId)
                return UserOperationResult.CannotChangeOwnRole;

            if (await HasRelatedRecordsAsync(id))
                return UserOperationResult.HasRelatedRecords;

            // Remove student profile if becoming another role
            if (newRole != UserRole.Student &&
                user.StudentProfile != null)
            {
                _context.StudentProfiles.Remove(
                    user.StudentProfile);
            }

            // Add student profile if becoming a student
            if (newRole == UserRole.Student &&
                user.StudentProfile == null)
            {
                user.StudentProfile = new StudentProfile();
            }

            user.Role = newRole;
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;

        await _context.SaveChangesAsync();

        return UserOperationResult.Success;
    }

    // Explain which protected relationships prevent account deletion.
    public async Task<UserDeletionDependencies> GetDeletionDependenciesAsync(int userId)
    {
        return new UserDeletionDependencies(
            await _context.Courses.CountAsync(c => c.InstructorId == userId),
            await _context.Enrollments.CountAsync(e => e.StudentId == userId),
            await _context.Orders.CountAsync(o => o.StudentId == userId),
            await _context.Reviews.CountAsync(r => r.StudentId == userId));
    }

    // DELETE
    public async Task<UserOperationResult> DeleteAsync(
        int id,
        int currentAdminId)
    {
        if (id == currentAdminId)
            return UserOperationResult.CannotDeleteSelf;

        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return UserOperationResult.NotFound;

        if (await HasRelatedRecordsAsync(id))
            return UserOperationResult.HasRelatedRecords;

        _context.Users.Remove(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        {
            // A related record may have been added after the initial check.
            _context.ChangeTracker.Clear();
            return UserOperationResult.HasRelatedRecords;
        }

        return UserOperationResult.Success;
    }

    // Check related records
    private async Task<bool> HasRelatedRecordsAsync(int userId)
    {
        if (await _context.Courses
            .AnyAsync(c => c.InstructorId == userId))
            return true;

        if (await _context.Enrollments
            .AnyAsync(e => e.StudentId == userId))
            return true;

        if (await _context.Orders
            .AnyAsync(o => o.StudentId == userId))
            return true;

        if (await _context.Reviews
            .AnyAsync(r => r.StudentId == userId))
            return true;

        return false;
    }
}

public sealed record UserDeletionDependencies(int Courses, int Enrollments, int Orders, int Reviews);
