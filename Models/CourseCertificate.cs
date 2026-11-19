using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models;

public class CourseCertificate
{
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    [MaxLength(100)] public string StudentName { get; set; } = "";
    [MaxLength(200)] public string CourseTitle { get; set; } = "";
    [MaxLength(100)] public string InstructorName { get; set; } = "";
    public int? DurationMinutes { get; set; }
    public DateTime IssuedAt { get; set; }
    public Guid CertificateNumber { get; set; }
}
