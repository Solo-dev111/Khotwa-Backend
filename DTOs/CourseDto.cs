namespace Khotwa.DTOs
{
    public class CourseDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Level { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int InstructorId { get; set; }

        public string InstructorName { get; set; } = string.Empty;

        public int LessonCount { get; set; }
    }
}
