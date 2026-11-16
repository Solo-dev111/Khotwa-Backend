using Khotwa.DTOs;
using Khotwa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly CourseService _service;

    public CoursesController(CourseService service)
    {
        _service = service;
    }

    // Get current user ID from JWT
    private int? CurrentUserId()
    {
        string? value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int id)
            ? id : null;
    }

    // Validate price filters
    private static string? ValidatePrices(
        decimal? minPrice,
        decimal? maxPrice)
    {
        if (minPrice < 0 || maxPrice < 0)
            return "Price cannot be negative";

        if (minPrice.HasValue &&
            maxPrice.HasValue &&
            minPrice.Value > maxPrice.Value)
        {
            return "Minimum price cannot exceed maximum price";
        }

        return null;
    }

    // Validate course text fields
    private static bool HasValidText(SaveCourseDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Title)
            && !string.IsNullOrWhiteSpace(dto.Description)
            && !string.IsNullOrWhiteSpace(dto.Level);
    }

    // Convert service errors to HTTP responses
    private IActionResult ErrorResponse(CourseActionResult result)
    {
        return result switch
        {
            CourseActionResult.NotFound =>
                NotFound("Course not found"),

            CourseActionResult.Forbidden =>
                Forbid(),

            CourseActionResult.InvalidCategory =>
                BadRequest("Category not found"),

            CourseActionResult.InvalidInstructor =>
                BadRequest("Instructor not found or invalid role"),

            CourseActionResult.HasDependencies =>
                Conflict(
                    "Cannot delete a course with enrollments, orders or reviews"),

            _ => BadRequest()
        };
    }

    // GET: api/courses
    // Public: Published courses only
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? level = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        string? error = ValidatePrices(minPrice, maxPrice);

        if (error != null)
            return BadRequest(error);

        var result = await _service.GetPublishedAsync(
            search,
            categoryId,
            level,
            minPrice,
            maxPrice,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    // GET: api/courses/mine
    // Instructor: Own courses
    [HttpGet("mine")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? level = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (CurrentUserId() is not int instructorId)
            return Unauthorized();

        string? error = ValidatePrices(minPrice, maxPrice);

        if (error != null)
            return BadRequest(error);

        var result = await _service.GetMineAsync(
            instructorId,
            search,
            categoryId,
            level,
            minPrice,
            maxPrice,
            isPublished,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    // GET: api/courses/admin
    // Admin: All courses
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdmin(
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? level = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        string? error = ValidatePrices(minPrice, maxPrice);

        if (error != null)
            return BadRequest(error);

        var result = await _service.GetAdminAsync(
            search,
            categoryId,
            level,
            minPrice,
            maxPrice,
            isPublished,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    // GET: api/courses/1
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _service.GetByIdAsync(
            id,
            CurrentUserId(),
            User.IsInRole("Admin"),
            User.IsInRole("Instructor"));

        if (course == null)
            return NotFound("Course not found");

        return Ok(course);
    }

    // POST: api/courses
    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Create(
        [FromBody] SaveCourseDto dto)
    {
        if (!HasValidText(dto))
            return BadRequest(
                "Title, description and level are required");

        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var (result, course) = await _service.CreateAsync(
            dto,
            userId,
            User.IsInRole("Admin"));

        if (result != CourseActionResult.Success)
            return ErrorResponse(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = course!.Id },
            course);
    }

    // PUT: api/courses/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveCourseDto dto)
    {
        if (!HasValidText(dto))
            return BadRequest(
                "Title, description and level are required");

        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.UpdateAsync(
            id,
            dto,
            userId,
            User.IsInRole("Admin"));

        if (result != CourseActionResult.Success)
            return ErrorResponse(result);

        return NoContent();
    }

    // DELETE: api/courses/1
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.DeleteAsync(
            id,
            userId,
            User.IsInRole("Admin"));

        if (result != CourseActionResult.Success)
            return ErrorResponse(result);

        return NoContent();
    }
}
