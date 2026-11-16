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
        public int SortOrder { get; set; }

        public bool IsPreview { get; set; } = false;
    }
}
