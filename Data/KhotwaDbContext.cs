using Khotwa.Models;
using Microsoft.EntityFrameworkCore;

namespace Khotwa.Data;

public class KhotwaDbContext : DbContext
{
    public KhotwaDbContext(
        DbContextOptions<KhotwaDbContext> options)
        : base(options)
    {
    }

   
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<StudentProfile> StudentProfiles { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Unique Email
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // One-to-One: User - StudentProfile
        modelBuilder.Entity<StudentProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-Many: Category - Courses
        modelBuilder.Entity<Course>()
            .HasOne(c => c.Category)
            .WithMany(c => c.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Instructor - Courses
        modelBuilder.Entity<Course>()
            .HasOne(c => c.Instructor)
            .WithMany(u => u.InstructorCourses)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Course - Lessons
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-Many: Students - Courses
        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent duplicate enrollment
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique();

        // One-to-Many: Student - Orders
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Student)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Order - OrderItems
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-Many: Orders - Courses
        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Course)
            .WithMany(c => c.OrderItems)
            .HasForeignKey(i => i.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent duplicate course in one order
        modelBuilder.Entity<OrderItem>()
            .HasIndex(i => new { i.OrderId, i.CourseId })
            .IsUnique();

        // One-to-Many: Student - Reviews
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Student)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-Many: Course - Reviews
        modelBuilder.Entity<Review>()
            .HasOne(r => r.Course)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // One review per student per course
        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.StudentId, r.CourseId })
            .IsUnique();

        // Decimal precision for money
        modelBuilder.Entity<Course>()
            .Property(c => c.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.PriceAtPurchase)
            .HasPrecision(18, 2);
    }
}
