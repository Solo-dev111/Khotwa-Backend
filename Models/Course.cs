using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models
{
    public class Course
    {
        public int Id { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int CategoryId { get; set; }

        public int InstructorId { get; set; }

        // Navigation Properties
        public Category Category { get; set; } = null!;

        public User Instructor { get; set; } = null!;

        public ICollection<Lesson> Lessons { get; set; }
            = new List<Lesson>();

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();
    }

}
