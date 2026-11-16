using Khotwa.Services;
using Khotwa.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api")]
[ApiController]
public class LessonsController : ControllerBase
{
    private readonly LessonService _service;

    public LessonsController(LessonService service)
    {
        _service = service;
    }

    // Get logged-in user's ID
    private int? GetCurrentUserId()
    {
        string? value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int id)
            ? id
            : null;
    }

    // GET: api/courses/1/lessons
    [HttpGet("courses/{courseId:int}/lessons")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByCourse(int courseId)
    {
        var result = await _service.GetByCourseAsync(
            courseId,
            GetCurrentUserId(),
            User.IsInRole("Admin"));

        if (result == null)
            return NotFound("Course not found");

        return Ok(result);
    }

    // GET: api/lessons/1
    [HttpGet("lessons/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var (status, lesson) = await _service.GetByIdAsync(
            id,
            GetCurrentUserId(),
            User.IsInRole("Admin"));

        if (status == LessonResult.NotFound)
            return NotFound("Lesson not found");

        if (status == LessonResult.Forbidden)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized("Login required");

            return Forbid();
        }

        return Ok(lesson);
    }

    // POST: api/courses/1/lessons
    [HttpPost("courses/{courseId:int}/lessons")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Create(
        int courseId,
        [FromBody] SaveLessonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Lesson title is required");

        int? userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var (status, lesson) = await _service.CreateAsync(
            courseId,
            dto,
            userId.Value,
            User.IsInRole("Admin"));

        if (status == LessonResult.NotFound)
            return NotFound("Course not found");

        if (status == LessonResult.Forbidden)
            return Forbid();

        return CreatedAtAction(
            nameof(GetById),
            new { id = lesson!.Id },
            lesson);
    }

    // PUT: api/lessons/1
    [HttpPut("lessons/{id:int}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveLessonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Lesson title is required");

        int? userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _service.UpdateAsync(
            id,
            dto,
            userId.Value,
            User.IsInRole("Admin"));

        if (result == LessonResult.NotFound)
            return NotFound("Lesson not found");

        if (result == LessonResult.Forbidden)
            return Forbid();

        return NoContent();
    }

    // DELETE: api/lessons/1
    [HttpDelete("lessons/{id:int}")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        int? userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var result = await _service.DeleteAsync(
            id,
            userId.Value,
            User.IsInRole("Admin"));

        if (result == LessonResult.NotFound)
            return NotFound("Lesson not found");

        if (result == LessonResult.Forbidden)
            return Forbid();

        return NoContent();
    }
}
