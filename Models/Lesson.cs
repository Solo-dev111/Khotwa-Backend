using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? VideoUrl { get; set; }

        public int? DurationMinutes { get; set; }

        public int SortOrder { get; set; }

        public bool IsPreview { get; set; } = false;

        // Foreign Key
        public int CourseId { get; set; }

        // One-to-Many
        public Course Course { get; set; } = null!;
    }
}
