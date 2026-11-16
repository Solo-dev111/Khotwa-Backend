using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class SaveOrderDto
    {
        [Required]
        [MinLength(1)]
        [MaxLength(20)]
        public List<int> CourseIds { get; set; } = new();
    }
}
