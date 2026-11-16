namespace Khotwa.DTOs
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public decimal PriceAtPurchase { get; set; }
    }
}
