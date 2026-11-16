using System.ComponentModel.DataAnnotations;

namespace Khotwa.Models
{
    public class StudentProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        public string? ProfileImageUrl { get; set; }

        // One-to-One
        public User User { get; set; } = null!;
    }
}
