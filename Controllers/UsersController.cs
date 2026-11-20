using Khotwa.Services;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service)
    {
        _service = service;
    }

    // Validate role names
    private static bool TryParseRole(
        string? value,
        out UserRole role)
    {
        role = UserRole.Student;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string roleName = value.Trim();

        return Enum.GetNames<UserRole>().Any(name =>
            name.Equals(
                roleName,
                StringComparison.OrdinalIgnoreCase))
            && Enum.TryParse(roleName, true, out role);
    }

    // Get current admin ID
    private int? GetCurrentAdminId()
    {
        string? value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out int id)
            ? id
            : null;
    }

    // GET ALL + SEARCH + FILTER + PAGINATION
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        UserRole? selectedRole = null;

        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!TryParseRole(role, out UserRole parsedRole))
                return BadRequest("Invalid role");

            selectedRole = parsedRole;
        }

        var result = await _service.GetAllAsync(
            search,
            selectedRole,
            pageNumber,
            pageSize);

        return Ok(result);
    }

    // GET BY ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _service.GetByIdAsync(id);

        if (user == null)
            return NotFound("User not found");

        return Ok(user);
    }

    // CREATE
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("Full name is required");

        if (!TryParseRole(dto.Role, out UserRole role))
            return BadRequest("Invalid role");

        var (user, result) =
            await _service.CreateAsync(dto, role);

        if (result == UserOperationResult.DuplicateEmail)
            return Conflict("Email already exists");

        return CreatedAtAction(
            nameof(GetById),
            new { id = user!.Id },
            user);
    }

    // UPDATE
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("Full name is required");

        if (!TryParseRole(dto.Role, out UserRole role))
            return BadRequest("Invalid role");

        int? adminId = GetCurrentAdminId();

        if (adminId == null)
            return Unauthorized();

        var result = await _service.UpdateAsync(
            id, dto, role, adminId.Value);

        return result switch
        {
            UserOperationResult.Success => NoContent(),

            UserOperationResult.NotFound =>
                NotFound("User not found"),

            UserOperationResult.DuplicateEmail =>
                Conflict("Email already exists"),

            UserOperationResult.HasRelatedRecords =>
                Conflict("Cannot change role of user with related records"),

            UserOperationResult.CannotChangeOwnRole =>
                Conflict("Cannot change your own role"),

            _ => BadRequest()
        };
    }

    // DELETE
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        int? adminId = GetCurrentAdminId();

        if (adminId == null)
            return Unauthorized();

        var result = await _service.DeleteAsync(
            id, adminId.Value);

        return result switch
        {
            UserOperationResult.Success => NoContent(),

            UserOperationResult.NotFound =>
                NotFound("User not found"),

            UserOperationResult.HasRelatedRecords =>
                Conflict(new
                {
                    code = "USER_HAS_RELATED_RECORDS",
                    message = "User has related records",
                    dependencies = await _service.GetDeletionDependenciesAsync(id)
                }),

            UserOperationResult.CannotDeleteSelf =>
                Conflict("Cannot delete your own account"),

            _ => BadRequest()
        };
    }
}
