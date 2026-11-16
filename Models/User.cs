using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models
{
    public enum UserRole
    {
        Student,
        Instructor,
        Admin
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Student;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // One-to-One
        public StudentProfile? StudentProfile { get; set; }

        // One-to-Many
        public ICollection<Course> InstructorCourses { get; set; }
            = new List<Course>();

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<Order> Orders { get; set; }
            = new List<Order>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();
    }
}
