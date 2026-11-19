namespace Khotwa.Models;

public class LessonCompletion
{
    public int EnrollmentId { get; set; }
    public int LessonId { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public Enrollment Enrollment { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
