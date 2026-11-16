using Khotwa.Services;
using Khotwa.DTOs;
using Khotwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Khotwa.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // REGISTER STUDENT
    [HttpPost("register/student")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterStudent(
        [FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("Full name is required");

        var result = await _authService.RegisterAsync(
            dto, UserRole.Student);

        if (result == null)
            return Conflict("Email already exists");

        return Ok(result);
    }

    // REGISTER INSTRUCTOR
    [HttpPost("register/instructor")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterInstructor(
        [FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("Full name is required");

        var result = await _authService.RegisterAsync(
            dto, UserRole.Instructor);

        if (result == null)
            return Conflict("Email already exists");

        return Ok(result);
    }

    // LOGIN
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result == null)
            return Unauthorized("Invalid email or password");

        return Ok(result);
    }

    // CURRENT USER
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            FullName = User.FindFirstValue(ClaimTypes.Name),
            Email = User.FindFirstValue(ClaimTypes.Email),
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
