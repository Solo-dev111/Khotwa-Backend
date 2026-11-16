namespace Khotwa.DTOs
{
    public class LessonDto
    {
        public int Id { get; set; }

        public int CourseId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        public string? VideoUrl { get; set; }

        public int SortOrder { get; set; }

        public bool IsPreview { get; set; }
    }
}
