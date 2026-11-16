using Khotwa.DTOs;
using Khotwa.Models;
using Khotwa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderService _service;

    public OrdersController(OrderService service)
    {
        _service = service;
    }

    private int? CurrentUserId()
    {
        string? value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int id)
            ? id : null;
    }

    private IActionResult ErrorResponse(OrderActionResult result)
    {
        return result switch
        {
            OrderActionResult.NotFound =>
                NotFound("Order not found"),

            OrderActionResult.InvalidCourses =>
                BadRequest("Invalid or duplicate course IDs"),

            OrderActionResult.CourseUnavailable =>
                BadRequest("Some courses are not published or do not exist"),

            OrderActionResult.AlreadyEnrolled =>
                Conflict("Student is already enrolled in a selected course"),

            OrderActionResult.NotPending =>
                Conflict("Only pending orders can be changed"),

            _ => BadRequest()
        };
    }

    // GET: api/orders (Admin)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? studentId,
        [FromQuery] OrderStatus? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _service.GetAllAsync(
            studentId, status, pageNumber, pageSize));
    }

    // GET: api/orders/mine
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (CurrentUserId() is not int id)
            return Unauthorized();

        return Ok(await _service.GetAllAsync(
            id, null, pageNumber, pageSize));
    }

    // GET: api/orders/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var order = await _service.GetByIdAsync(
            id, userId, User.IsInRole("Admin"));

        return order == null
            ? NotFound("Order not found")
            : Ok(order);
    }

    // POST: api/orders
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create(
        [FromBody] SaveOrderDto dto)
    {
        if (CurrentUserId() is not int studentId)
            return Unauthorized();

        var (result, order) =
            await _service.CreateAsync(studentId, dto);

        if (result != OrderActionResult.Success)
            return ErrorResponse(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order!.Id },
            order);
    }

    // PUT: api/orders/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveOrderDto dto)
    {
        if (CurrentUserId() is not int studentId)
            return Unauthorized();

        var result = await _service.UpdateAsync(
            id, studentId, dto);

        return result == OrderActionResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }

    // POST: api/orders/1/confirm-demo
    [HttpPost("{id:int}/confirm-demo")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ConfirmDemo(int id)
    {
        if (CurrentUserId() is not int studentId)
            return Unauthorized();

        var result = await _service.ConfirmDemoAsync(
            id, studentId);

        return result == OrderActionResult.Success
            ? Ok(new
            {
                Message = "Demo purchase confirmed",
                OrderId = id
            })
            : ErrorResponse(result);
    }

    // DELETE: api/orders/1
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.DeleteAsync(
            id, userId, User.IsInRole("Admin"));

        return result == OrderActionResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }
}
