namespace Khotwa.DTOs
{
    public class LessonSummaryDto
    {
        public int Id { get; set; }

        public int CourseId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int? DurationMinutes { get; set; }

        public int SortOrder { get; set; }

        public bool IsPreview { get; set; }
    }
}
