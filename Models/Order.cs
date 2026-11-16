namespace Khotwa.Models
{
    public enum OrderStatus
    {
        Pending,
        Paid,
        Cancelled
    }

    public class Order
    {
        public int Id { get; set; }

        // Foreign Key
        public int StudentId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Navigation Properties
        public User Student { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();
    }
}
