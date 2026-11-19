using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class SaveLessonDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(10000)]
        public string? Content { get; set; }

        [MaxLength(2048)]
        public string? VideoUrl { get; set; }

        [Range(0, int.MaxValue)]
        [Required]
        [Range(1, 1440)]
        public int? DurationMinutes { get; set; }

        public int SortOrder { get; set; }

        public bool IsPreview { get; set; } = false;
    }
}
