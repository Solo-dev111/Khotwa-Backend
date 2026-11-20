using Khotwa.Data;
using Khotwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace Khotwa.Controllers;

[ApiController, Route("api/learning"), Authorize(Roles = "Student")]
public class LearningController(KhotwaDbContext db) : ControllerBase
{
    private int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{courseId:int}")]
    public async Task<IActionResult> Get(int courseId)
    {
        var enrollment = await db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.StudentId == StudentId && e.CourseId == courseId);
        if (enrollment == null) return Ok(new { enrolled = false, completedLessonIds = Array.Empty<int>(), totalLessons = 0, certificate = (object?)null });
        var ids = await db.LessonCompletions.Where(x => x.EnrollmentId == enrollment.Id && x.Lesson.CourseId == courseId).Select(x => x.LessonId).ToArrayAsync();
        var total = await db.Lessons.CountAsync(l => l.CourseId == courseId);
        var certificate = await db.CourseCertificates.AsNoTracking().Where(x => x.EnrollmentId == enrollment.Id)
            .Select(x => new { x.StudentName, x.CourseTitle, x.InstructorName, x.DurationMinutes, x.IssuedAt, x.CertificateNumber }).FirstOrDefaultAsync();
        return Ok(new { enrolled = true, completedLessonIds = ids, totalLessons = total, certificate });
    }

    [HttpPut("{courseId:int}/lessons/{lessonId:int}")]
    public async Task<IActionResult> Complete(int courseId, int lessonId)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var enrollment = await db.Enrollments.Include(e => e.Student).Include(e => e.Course).ThenInclude(c => c.Instructor)
                .FirstOrDefaultAsync(e => e.StudentId == StudentId && e.CourseId == courseId);
            if (enrollment == null || !enrollment.Course.IsPublished) return Forbid();
            if (!await db.Lessons.AnyAsync(l => l.Id == lessonId && l.CourseId == courseId)) return NotFound();
            if (!await db.LessonCompletions.AnyAsync(x => x.EnrollmentId == enrollment.Id && x.LessonId == lessonId))
            {
                db.LessonCompletions.Add(new LessonCompletion { EnrollmentId = enrollment.Id, LessonId = lessonId });
                await db.SaveChangesAsync();
            }
            var lessons = await db.Lessons.Where(l => l.CourseId == courseId).Select(l => new { l.Id, l.DurationMinutes }).ToListAsync();
            var completed = await db.LessonCompletions.Where(x => x.EnrollmentId == enrollment.Id).Select(x => x.LessonId).ToListAsync();
            enrollment.IsCompleted = lessons.Count > 0 && lessons.All(l => completed.Contains(l.Id));
            if (enrollment.IsCompleted && !await db.CourseCertificates.AnyAsync(x => x.EnrollmentId == enrollment.Id))
                db.CourseCertificates.Add(new CourseCertificate {
                    EnrollmentId = enrollment.Id, StudentName = enrollment.Student.FullName,
                    CourseTitle = enrollment.Course.Title, InstructorName = enrollment.Course.Instructor.FullName,
                    DurationMinutes = lessons.All(l => l.DurationMinutes.HasValue) ? lessons.Sum(l => l.DurationMinutes) : null,
                    IssuedAt = DateTime.UtcNow, CertificateNumber = Guid.NewGuid()
                });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (SqlException e) when (e.Number is 1205 or 1222) { return Conflict("Please retry"); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 1205 or 1222 or 2601 or 2627 }) { return Conflict("Please retry"); }
        return await Get(courseId);
    }
}
