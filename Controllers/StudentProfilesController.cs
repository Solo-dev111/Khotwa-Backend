using Khotwa.DTOs;
using Khotwa.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoursePlatform.Controllers;

[Route("api/student-profiles")]
[ApiController]
[Authorize]
public class StudentProfilesController : ControllerBase
{
    private readonly StudentProfileService _service;

    public StudentProfilesController(
        StudentProfileService service)
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
        StudentProfileResult result)
    {
        return result switch
        {
            StudentProfileResult.NotFound =>
                NotFound("Profile not found"),

            StudentProfileResult.InvalidStudent =>
                BadRequest("Student not found or invalid role"),

            StudentProfileResult.AlreadyExists =>
                Conflict("Student already has a profile"),

            _ => BadRequest()
        };
    }

    // GET ALL + SEARCH + PAGINATION (Admin)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _service.GetAllAsync(
            search, pageNumber, pageSize));
    }

    // GET MY PROFILE
    [HttpGet("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine()
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var profile = await _service.GetMineAsync(userId);

        return profile == null
            ? NotFound("Profile not found")
            : Ok(profile);
    }

    // GET BY ID (Admin)
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var profile = await _service.GetByIdAsync(id);

        return profile == null
            ? NotFound("Profile not found")
            : Ok(profile);
    }

    // CREATE MY PROFILE
    [HttpPost("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateMine(
        [FromBody] SaveStudentProfileDto dto)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var (result, profile) =
            await _service.CreateAsync(userId, dto);

        if (result != StudentProfileResult.Success)
            return ErrorResponse(result);

        return Created(
            "/api/student-profiles/mine",
            profile);
    }

    // CREATE PROFILE (Admin)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudentProfileDto dto)
    {
        var (result, profile) =
            await _service.CreateAsync(dto.UserId, dto);

        if (result != StudentProfileResult.Success)
            return ErrorResponse(result);

        return CreatedAtAction(
            nameof(GetById),
            new { id = profile!.Id },
            profile);
    }

    // UPDATE MY PROFILE
    [HttpPut("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateMine(
        [FromBody] SaveStudentProfileDto dto)
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.UpdateMineAsync(
            userId, dto);

        return result == StudentProfileResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }

    // UPDATE PROFILE (Admin)
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveStudentProfileDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);

        return result == StudentProfileResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }

    // DELETE MY PROFILE
    [HttpDelete("mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> DeleteMine()
    {
        if (CurrentUserId() is not int userId)
            return Unauthorized();

        var result = await _service.DeleteMineAsync(userId);

        return result == StudentProfileResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }

    // DELETE PROFILE (Admin)
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);

        return result == StudentProfileResult.Success
            ? NoContent()
            : ErrorResponse(result);
    }
}
