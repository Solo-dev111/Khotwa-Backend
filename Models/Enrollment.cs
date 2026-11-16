namespace Khotwa.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        // Foreign Keys
        public int StudentId { get; set; }

        public int CourseId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public bool IsCompleted { get; set; } = false;

        // Navigation Properties
        public User Student { get; set; } = null!;

        public Course Course { get; set; } = null!;
    }
}
