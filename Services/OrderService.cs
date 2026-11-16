using Khotwa.Data;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Khotwa.Services;

public enum OrderActionResult
{
    Success,
    NotFound,
    InvalidCourses,
    CourseUnavailable,
    AlreadyEnrolled,
    NotPending
}

public class OrderService
{
    private readonly KhotwaDbContext _context;

    public OrderService(KhotwaDbContext context)
    {
        _context = context;
    }

    // Convert database query to DTO
    private static IQueryable<OrderDto> ToDtoQuery(
        IQueryable<Order> query)
    {
        return query.Select(o => new OrderDto
        {
            Id = o.Id,
            StudentId = o.StudentId,
            OrderDate = o.OrderDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status.ToString(),
            Items = o.OrderItems
                .OrderBy(i => i.Id)
                .Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    CourseId = i.CourseId,
                    CourseTitle = i.Course.Title,
                    PriceAtPurchase = i.PriceAtPurchase
                })
                .ToList()
        });
    }

    // GET ORDERS + PAGINATION
    public async Task<PagedResultDto<OrderDto>> GetAllAsync(
        int? studentId,
        OrderStatus? status,
        int pageNumber,
        int pageSize)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 100000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Orders.AsNoTracking();

        if (studentId.HasValue)
            query = query.Where(o =>
                o.StudentId == studentId.Value);

        if (status.HasValue)
            query = query.Where(o =>
                o.Status == status.Value);

        int count = await query.CountAsync();


        var pagedQuery = query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await ToDtoQuery(pagedQuery)
            .ToListAsync();


        return new PagedResultDto<OrderDto>
        {
            Items = items,
            TotalCount = count,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // GET ORDER BY ID
    public async Task<OrderDto?> GetByIdAsync(
        int id,
        int userId,
        bool isAdmin)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == id);

        if (!isAdmin)
            query = query.Where(o =>
                o.StudentId == userId);

        return await ToDtoQuery(query)
            .FirstOrDefaultAsync();
    }

    // VALIDATE SELECTED COURSES
    private async Task<(OrderActionResult, List<Course>)>
        ValidateCoursesAsync(
            int studentId,
            List<int> courseIds)
    {
        if (courseIds == null ||
            courseIds.Count == 0 ||
            courseIds.Count > 20 ||
            courseIds.Any(id => id <= 0) ||
            courseIds.Distinct().Count() != courseIds.Count)
        {
            return (OrderActionResult.InvalidCourses, new());
        }

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c =>
                courseIds.Contains(c.Id) &&
                c.IsPublished)
            .ToListAsync();

        if (courses.Count != courseIds.Count)
            return (OrderActionResult.CourseUnavailable, new());

        bool alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e =>
                e.StudentId == studentId &&
                courseIds.Contains(e.CourseId));

        if (alreadyEnrolled)
            return (OrderActionResult.AlreadyEnrolled, new());

        return (OrderActionResult.Success, courses);
    }

    // CREATE PENDING ORDER
    public async Task<(OrderActionResult, OrderDto?)> CreateAsync(
        int studentId,
        SaveOrderDto dto)
    {
        var (result, courses) =
            await ValidateCoursesAsync(
                studentId, dto.CourseIds);

        if (result != OrderActionResult.Success)
            return (result, null);

        var order = new Order
        {
            StudentId = studentId,
            Status = OrderStatus.Pending,
            TotalAmount = courses.Sum(c => c.Price),
            OrderItems = courses.Select(c => new OrderItem
            {
                CourseId = c.Id,
                PriceAtPurchase = c.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return (
            OrderActionResult.Success,
            await GetByIdAsync(order.Id, studentId, false)
        );
    }

    // UPDATE COURSES IN A PENDING ORDER
    public async Task<OrderActionResult> UpdateAsync(
        int orderId,
        int studentId,
        SaveOrderDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o =>
                o.Id == orderId &&
                o.StudentId == studentId);

        if (order == null)
            return OrderActionResult.NotFound;

        if (order.Status != OrderStatus.Pending)
            return OrderActionResult.NotPending;

        var (result, courses) =
            await ValidateCoursesAsync(
                studentId, dto.CourseIds);

        if (result != OrderActionResult.Success)
            return result;

        var ids = courses.Select(c => c.Id).ToHashSet();

        // Remove items no longer selected
        var removed = order.OrderItems
            .Where(i => !ids.Contains(i.CourseId))
            .ToList();

        _context.OrderItems.RemoveRange(removed);

        // Add new items or update their prices
        foreach (var course in courses)
        {
            var existing = order.OrderItems
                .FirstOrDefault(i =>
                    i.CourseId == course.Id);

            if (existing == null)
            {
                order.OrderItems.Add(new OrderItem
                {
                    CourseId = course.Id,
                    PriceAtPurchase = course.Price
                });
            }
            else
            {
                existing.PriceAtPurchase = course.Price;
            }
        }

        order.TotalAmount = courses.Sum(c => c.Price);

        await _context.SaveChangesAsync();
        return OrderActionResult.Success;
    }

    // CONFIRM DEMO PURCHASE AND CREATE ENROLLMENTS
    public async Task<OrderActionResult> ConfirmDemoAsync(
        int orderId,
        int studentId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o =>
                o.Id == orderId &&
                o.StudentId == studentId);

        if (order == null)
            return OrderActionResult.NotFound;

        if (order.Status != OrderStatus.Pending)
            return OrderActionResult.NotPending;

        var ids = order.OrderItems
            .Select(i => i.CourseId)
            .ToList();

        if (ids.Count == 0)
            return OrderActionResult.InvalidCourses;

        int publishedCount = await _context.Courses
            .CountAsync(c =>
                ids.Contains(c.Id) &&
                c.IsPublished);

        if (publishedCount != ids.Count)
            return OrderActionResult.CourseUnavailable;

        bool alreadyEnrolled = await _context.Enrollments
            .AnyAsync(e =>
                e.StudentId == studentId &&
                ids.Contains(e.CourseId));

        if (alreadyEnrolled)
            return OrderActionResult.AlreadyEnrolled;

        // This is a simulation, not a real payment.
        order.Status = OrderStatus.Paid;

        foreach (int courseId in ids)
        {
            _context.Enrollments.Add(new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return OrderActionResult.Success;
    }

    // DELETE PENDING ORDER
    public async Task<OrderActionResult> DeleteAsync(
        int orderId,
        int userId,
        bool isAdmin)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.Id == orderId &&
                (isAdmin || o.StudentId == userId));

        if (order == null)
            return OrderActionResult.NotFound;

        // Preserve completed purchase history
        if (order.Status != OrderStatus.Pending)
            return OrderActionResult.NotPending;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return OrderActionResult.Success;
    }
}
