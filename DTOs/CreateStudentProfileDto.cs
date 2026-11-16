using System.ComponentModel.DataAnnotations;

namespace Khotwa.DTOs
{
    public class CreateStudentProfileDto : SaveStudentProfileDto
    {
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }
    }
}
