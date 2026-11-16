using Khotwa.DTOs;
using Khotwa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly ReviewService _service;

    public ReviewsController(ReviewService service)
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

    // GET ALL + SEARCH + FILTER + PAGINATION
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? courseId = null,
        [FromQuery] int? rating = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (rating.HasValue &&
            (rating < 1 || rating > 5))
        {
            return BadRequest("Rating must be between 1 and 5");
        }

        var result = await _service.GetAllAsync(
            courseId,
            rating,
            search,
            pageNumber,
            pageSize,
            User.IsInRole("Admin"));

        return Ok(result);
    }

    // GET MY REVIEWS
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (CurrentUserId() is not int studentId)
            return Unauthorized();

        return Ok(await _service.GetMineAsync(
            studentId, pageNumber, pageSize));
    }

    // GET BY ID
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var review = await _service.GetByIdAsync(
            id,
            CurrentUserId(),
            User.IsInRole("Admin"));

        return review == null
            ? NotFound("Review not found")
            : Ok(review);
    }

    // CREATE
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create(
        [FromBody] SaveReviewDto dto)
    {
        if (CurrentUserId() is not int studentId)
            return Unauthorized();

        var (result, review) =
            await _service.CreateAsync(dto, studentId);

        if (result == ReviewActionResult.CourseUnavailable)
            return BadRequest("Course not found or unpublished");

        if (result == ReviewActionResult.NotEnrolled)
            return BadRequest("You must enroll before reviewing");

        if (result == ReviewActionResult.AlreadyReviewed)
            return Conflict("You already reviewed this course");

        return CreatedAtAction(
            nameof(GetById),
            new { id = review!.Id },
            review);
    }

    // UPDATE
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateReviewDto dto)
    {
        if (CurrentUserId() is not int studentId)
            return Unauthorized();

        var result = await _service.UpdateAsync(
            id, dto, studentId);

        if (result == ReviewActionResult.NotFound)
            return NotFound("Review not found");

        if (result == ReviewActionResult.Forbidden)
            return Forbid();

        return NoContent();
    }

    // DELETE
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.DeleteAsync(
            id,
            userId,
            User.IsInRole("Admin"));

        if (result == ReviewActionResult.NotFound)
            return NotFound("Review not found");

        if (result == ReviewActionResult.Forbidden)
            return Forbid();

        return NoContent();
    }
}
