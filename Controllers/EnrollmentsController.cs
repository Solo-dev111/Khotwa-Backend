using Khotwa.DTOs;
using Khotwa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly EnrollmentService _service;

    public EnrollmentsController(EnrollmentService service)
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

    private IActionResult ErrorResponse(
        EnrollmentActionResult result)
    {
        return result switch
        {
            EnrollmentActionResult.NotFound =>
                NotFound("Enrollment not found"),

            EnrollmentActionResult.InvalidStudent =>
                BadRequest("Student not found or invalid role"),

            EnrollmentActionResult.CourseUnavailable =>
                BadRequest("Course not found or unpublished"),

            EnrollmentActionResult.PaymentRequired =>
                BadRequest("Purchase is required for this course"),

            EnrollmentActionResult.AlreadyEnrolled =>
                Conflict("Student is already enrolled"),

            EnrollmentActionResult.InvalidCompletion =>
                BadRequest("Completion is determined by completed lessons"),

            EnrollmentActionResult.PurchasedEnrollment =>
                Conflict("Cannot delete a purchased enrollment"),

            _ => BadRequest()
        };
    }

    // GET: api/enrollments (Admin)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? studentId,
        [FromQuery] int? courseId,
        [FromQuery] bool? isCompleted,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _service.GetAllAsync(
            studentId,
            courseId,
            isCompleted,
            pageNumber,
            pageSize));
    }

    // GET: api/enrollments/mine
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(
        [FromQuery] bool? isCompleted,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        return Ok(await _service.GetAllAsync(
            userId, null, isCompleted,
            pageNumber, pageSize));
    }

    // GET: api/enrollments/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var enrollment = await _service.GetByIdAsync(
            id, userId, User.IsInRole("Admin"));

        return enrollment == null
            ? NotFound("Enrollment not found")
            : Ok(enrollment);
    }

    // POST: api/enrollments
    [HttpPost]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> Create(
        [FromBody] SaveEnrollmentDto dto)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var (result, enrollment) =
            await _service.CreateAsync(
                dto, userId, User.IsInRole("Admin"));

        if (result != EnrollmentActionResult.Success)
            return ErrorResponse(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = enrollment!.Id },
            enrollment);
    }

    // PUT: api/enrollments/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateEnrollmentDto dto)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.UpdateAsync(
            id, userId, User.IsInRole("Admin"), dto);

        return result == EnrollmentActionResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }

    // DELETE: api/enrollments/1
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);

        return result == EnrollmentActionResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }
}
