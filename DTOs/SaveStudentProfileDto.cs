using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class SaveStudentProfileDto
    {
        [MaxLength(500)]
        public string? Bio { get; set; }

        [MaxLength(2048)]
        public string? ProfileImageUrl { get; set; }
    }
}
