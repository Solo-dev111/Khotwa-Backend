namespace Khotwa.Models;

public class Favorite
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
