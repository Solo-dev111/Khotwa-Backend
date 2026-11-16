
using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Services;

public enum LessonResult
{
    Success,
    NotFound,
    Forbidden
}

public class LessonService
{
    private readonly KhotwaDbContext _context;

    public LessonService(KhotwaDbContext context)
    {
        _context = context;
    }

    private static LessonDto ToDto(Lesson lesson)
    {
        return new LessonDto
        {
            Id = lesson.Id,
            CourseId = lesson.CourseId,
            Title = lesson.Title,
            Content = lesson.Content,
            VideoUrl = lesson.VideoUrl,
            SortOrder = lesson.SortOrder,
            IsPreview = lesson.IsPreview
        };
    }

    // GET COURSE LESSONS (SUMMARIES)
    public async Task<List<LessonSummaryDto>?> GetByCourseAsync(
        int courseId,
        int? userId,
        bool isAdmin)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new
            {
                c.IsPublished,
                c.InstructorId
            })
            .FirstOrDefaultAsync();

        if (course == null)
            return null;

        bool isOwner = userId.HasValue &&
                       course.InstructorId == userId.Value;

        // Draft courses are visible only to owner or admin
        if (!course.IsPublished && !isOwner && !isAdmin)
            return null;

        return await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CourseId == courseId)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Id)
            .Select(l => new LessonSummaryDto
            {
                Id = l.Id,
                CourseId = l.CourseId,
                Title = l.Title,
                SortOrder = l.SortOrder,
                IsPreview = l.IsPreview
            })
            .ToListAsync();
    }

    // GET LESSON DETAILS WITH ACCESS CHECK
    public async Task<(LessonResult, LessonDto?)> GetByIdAsync(
        int id,
        int? userId,
        bool isAdmin)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return (LessonResult.NotFound, null);

        bool isOwner = userId.HasValue &&
            lesson.Course.InstructorId == userId.Value;

        // Admin and course owner have full access
        if (isAdmin || isOwner)
            return (LessonResult.Success, ToDto(lesson));

        // Do not expose unpublished lessons
        if (!lesson.Course.IsPublished)
            return (LessonResult.NotFound, null);

        // Free preview lesson
        if (lesson.IsPreview)
            return (LessonResult.Success, ToDto(lesson));

        // Paid lessons require enrollment
        if (!userId.HasValue)
            return (LessonResult.Forbidden, null);

        bool enrolled = await _context.Enrollments
            .AnyAsync(e =>
                e.StudentId == userId.Value &&
                e.CourseId == lesson.CourseId);

        if (!enrolled)
            return (LessonResult.Forbidden, null);

        return (LessonResult.Success, ToDto(lesson));
    }

    // CREATE
    public async Task<(LessonResult, LessonDto?)> CreateAsync(
        int courseId,
        SaveLessonDto dto,
        int userId,
        bool isAdmin)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return (LessonResult.NotFound, null);

        if (!isAdmin && course.InstructorId != userId)
            return (LessonResult.Forbidden, null);

        var lesson = new Lesson
        {
            CourseId = courseId,
            Title = dto.Title.Trim(),
            Content = dto.Content,
            VideoUrl = dto.VideoUrl,
            SortOrder = dto.SortOrder,
            IsPreview = dto.IsPreview
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        return (LessonResult.Success, ToDto(lesson));
    }

    // UPDATE
    public async Task<LessonResult> UpdateAsync(
        int id,
        SaveLessonDto dto,
        int userId,
        bool isAdmin)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return LessonResult.NotFound;

        if (!isAdmin &&
            lesson.Course.InstructorId != userId)
        {
            return LessonResult.Forbidden;
        }

        lesson.Title = dto.Title.Trim();
        lesson.Content = dto.Content;
        lesson.VideoUrl = dto.VideoUrl;
        lesson.SortOrder = dto.SortOrder;
        lesson.IsPreview = dto.IsPreview;

        await _context.SaveChangesAsync();

        return LessonResult.Success;
    }

    // DELETE
    public async Task<LessonResult> DeleteAsync(
        int id,
        int userId,
        bool isAdmin)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return LessonResult.NotFound;

        if (!isAdmin &&
            lesson.Course.InstructorId != userId)
        {
            return LessonResult.Forbidden;
        }

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();

        return LessonResult.Success;
    }
}
