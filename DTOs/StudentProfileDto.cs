namespace Khotwa.DTOs
{
    public class StudentProfileDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Bio { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}
