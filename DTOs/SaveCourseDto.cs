using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class SaveCourseDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(3000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Required]
        [MaxLength(50)]
        public string Level { get; set; } = "Beginner";

        public string? ThumbnailUrl { get; set; }

        public bool IsPublished { get; set; } = false;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        // Optional for instructors, required for admin creation
        [Range(1, int.MaxValue)]
        public int? InstructorId { get; set; }
    }
}
