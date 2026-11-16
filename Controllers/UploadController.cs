
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Khotwa.Controllers;

[Route("api/upload")]
[ApiController]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public UploadController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // Upload course thumbnail
    [HttpPost("course")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> UploadCourse(IFormFile file)
    {
        return await SaveImage(file, "courses");
    }

    // Upload student profile image
    [HttpPost("profile")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UploadProfile(IFormFile file)
    {
        return await SaveImage(file, "profiles");
    }

    private async Task<IActionResult> SaveImage(
        IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please select an image");

        // Maximum 3 MB
        if (file.Length > 3 * 1024 * 1024)
            return BadRequest("Maximum image size is 3 MB");

        string extension = Path.GetExtension(
            file.FileName).ToLowerInvariant();

        string[] allowed = { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowed.Contains(extension))
            return BadRequest("Only JPG, PNG and WEBP are allowed");

        // Create upload directory
        string folderPath = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot", "uploads", folder);

        Directory.CreateDirectory(folderPath);

        // Generate unique file name
        string fileName = Guid.NewGuid() + extension;

        string filePath = Path.Combine(folderPath, fileName);

        await using (var stream = new FileStream(
            filePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        // Return image URL
        string imageUrl =
            $"{Request.Scheme}://{Request.Host}/uploads/{folder}/{fileName}";

        return Ok(new { url = imageUrl });
    }
}
