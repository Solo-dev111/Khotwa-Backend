using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class SaveEnrollmentDto
    {
        [Range(1, int.MaxValue)]
        public int CourseId { get; set; }

        // Admin may specify a student.
        // Regular students use their own ID from JWT.
        public int? StudentId { get; set; }
    }

    public class UpdateEnrollmentDto
    {
        public bool IsCompleted { get; set; }
    }
}
