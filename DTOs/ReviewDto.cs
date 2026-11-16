namespace Khotwa.DTOs
{
    public class ReviewDto
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}





