using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum EnrollmentActionResult
{
    Success,
    NotFound,
    InvalidStudent,
    CourseUnavailable,
    PaymentRequired,
    AlreadyEnrolled,
    PurchasedEnrollment,
    InvalidCompletion
}

public class EnrollmentService
{
    private readonly KhotwaDbContext _context;

    public EnrollmentService(KhotwaDbContext context)
    {
        _context = context;
    }

    // Convert query to DTO
    private static IQueryable<EnrollmentDto> ToDtoQuery(
        IQueryable<Enrollment> query)
    {
        return query.Select(e => new EnrollmentDto
        {
            Id = e.Id,
            StudentId = e.StudentId,
            StudentName = e.Student.FullName,
            CourseId = e.CourseId,
            CourseTitle = e.Course.Title,
            EnrolledAt = e.EnrolledAt,
            IsCompleted = e.Course.Lessons.Any() && !e.Course.Lessons.Any(l =>
                !e.LessonCompletions.Any(x => x.LessonId == l.Id))
        });
    }

    // GET ALL + FILTER + PAGINATION
    public async Task<PagedResultDto<EnrollmentDto>> GetAllAsync(
        int? studentId,
        int? courseId,
        bool? isCompleted,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Enrollments.AsNoTracking();

        if (studentId.HasValue)
            query = query.Where(e =>
                e.StudentId == studentId.Value);

        if (courseId.HasValue)
            query = query.Where(e =>
                e.CourseId == courseId.Value);

        if (isCompleted.HasValue)
            query = query.Where(e =>
                (e.Course.Lessons.Any() && !e.Course.Lessons.Any(l =>
                    !e.LessonCompletions.Any(x => x.LessonId == l.Id))) == isCompleted.Value);

        int count = await query.CountAsync();


        var pagedQuery = query
            .OrderByDescending(e => e.EnrolledAt)
            .ThenByDescending(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await ToDtoQuery(pagedQuery)
            .ToListAsync();


        return new PagedResultDto<EnrollmentDto>
        {
            Items = items,
            TotalCount = count,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // GET BY ID
    public async Task<EnrollmentDto?> GetByIdAsync(
        int id,
        int userId,
        bool isAdmin)
    {
        var query = _context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id);

        if (!isAdmin)
            query = query.Where(e =>
                e.StudentId == userId);

        return await ToDtoQuery(query)
            .FirstOrDefaultAsync();
    }

    // CREATE ENROLLMENT
    public async Task<(EnrollmentActionResult, EnrollmentDto?)>
        CreateAsync(
            SaveEnrollmentDto dto,
            int userId,
            bool isAdmin)
    {
        int studentId = isAdmin
            ? dto.StudentId.GetValueOrDefault()
            : userId;

        bool validStudent = await _context.Users
            .AnyAsync(u =>
                u.Id == studentId &&
                u.Role == UserRole.Student);

        if (!validStudent)
            return (EnrollmentActionResult.InvalidStudent, null);

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == dto.CourseId &&
                c.IsPublished);

        if (course == null)
            return (EnrollmentActionResult.CourseUnavailable, null);

        // Only admin may grant paid courses directly
        if (!isAdmin && course.Price > 0)
            return (EnrollmentActionResult.PaymentRequired, null);

        bool exists = await _context.Enrollments
            .AnyAsync(e =>
                e.StudentId == studentId &&
                e.CourseId == dto.CourseId);

        if (exists)
            return (EnrollmentActionResult.AlreadyEnrolled, null);

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return (
            EnrollmentActionResult.Success,
            await GetByIdAsync(enrollment.Id, studentId, true)
        );
    }

    // UPDATE COMPLETION STATUS
    public async Task<EnrollmentActionResult> UpdateAsync(
        int id,
        int userId,
        bool isAdmin,
        UpdateEnrollmentDto dto)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                (isAdmin || e.StudentId == userId));

        if (enrollment == null)
            return EnrollmentActionResult.NotFound;

        // Completion is derived from lesson records, not a client-supplied flag.
        bool completed = await _context.Lessons.AnyAsync(l => l.CourseId == enrollment.CourseId)
            && !await _context.Lessons.AnyAsync(l => l.CourseId == enrollment.CourseId &&
                !_context.LessonCompletions.Any(x => x.EnrollmentId == id && x.LessonId == l.Id));
        if (dto.IsCompleted != completed)
            return EnrollmentActionResult.InvalidCompletion;
        enrollment.IsCompleted = completed;

        await _context.SaveChangesAsync();

        return EnrollmentActionResult.Success;
    }

    // DELETE ENROLLMENT (ADMIN)
    public async Task<EnrollmentActionResult> DeleteAsync(int id)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
            return EnrollmentActionResult.NotFound;

        // Do not revoke access to a purchased course
        bool purchased = await _context.OrderItems
            .AnyAsync(i =>
                i.CourseId == enrollment.CourseId &&
                i.Order.StudentId == enrollment.StudentId &&
                i.Order.Status == OrderStatus.Paid);

        if (purchased)
            return EnrollmentActionResult.PurchasedEnrollment;

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        return EnrollmentActionResult.Success;
    }
}
