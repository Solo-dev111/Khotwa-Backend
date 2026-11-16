using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class SaveReviewDto
    {
        [Range(1, int.MaxValue)]
        public int CourseId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}




