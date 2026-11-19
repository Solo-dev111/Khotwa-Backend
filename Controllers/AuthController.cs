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
    private readonly IWebHostEnvironment _environment;

    public AuthController(AuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    // FIRST-RUN SETUP (local development only)
    [HttpGet("registration-status")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetRegistrationStatus()
    {
        bool canRegisterAdmin = _environment.IsDevelopment()
            && await _authService.CanRegisterAdminAsync();

        return Ok(new { canRegisterAdmin });
    }

    [HttpPost("register/admin")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterFirstAdmin([FromBody] RegisterDto dto)
    {
        if (!_environment.IsDevelopment())
            return StatusCode(403, "Initial admin registration is unavailable");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("Full name is required");

        var result = await _authService.RegisterAsync(dto, UserRole.Admin);

        if (result == null)
            return StatusCode(403, "Initial admin registration is no longer available");

        return Ok(result);
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
