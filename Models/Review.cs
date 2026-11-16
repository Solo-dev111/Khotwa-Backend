using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models
{
    public class Review
    {
        public int Id { get; set; }

        // Foreign Keys
        public int StudentId { get; set; }

        public int CourseId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public User Student { get; set; } = null!;

        public Course Course { get; set; } = null!;
    }
}
