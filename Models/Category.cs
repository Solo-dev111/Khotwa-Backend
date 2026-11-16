using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // One-to-Many
        public ICollection<Course> Courses { get; set; }
            = new List<Course>();
    }
}
