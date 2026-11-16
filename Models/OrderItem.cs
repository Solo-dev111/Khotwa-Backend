namespace Khotwa.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        // Foreign Keys
        public int OrderId { get; set; }

        public int CourseId { get; set; }

        public decimal PriceAtPurchase { get; set; }

        // Navigation Properties
        public Order Order { get; set; } = null!;

        public Course Course { get; set; } = null!;
    }
}
