using Khotwa.Data;
using Khotwa.Services;
using Khotwa.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace Khotwa.Controllers;

[ApiController, Route("api/[controller]"), Authorize(Roles = "Student")]
public class FavoritesController(KhotwaDbContext db) : ControllerBase
{
    private int StudentId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await CourseService.ToDtoQuery(db.Favorites.AsNoTracking()
            .Where(x => x.StudentId == StudentId && x.Course.IsPublished)
            .OrderByDescending(x => x.CreatedAt).Select(x => x.Course)).ToListAsync());

    [HttpPut("{courseId:int}")]
    public async Task<IActionResult> Add(int courseId)
    {
        var id = StudentId;
        if (!await db.Courses.AnyAsync(x => x.Id == courseId && x.IsPublished)) return NotFound();
        if (await db.Favorites.AnyAsync(x => x.StudentId == id && x.CourseId == courseId)) return NoContent();
        db.Favorites.Add(new Favorite { StudentId = id, CourseId = courseId });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 }) { }
        return NoContent();
    }

    [HttpDelete("{courseId:int}")]
    public async Task<IActionResult> Remove(int courseId)
    {
        await db.Favorites.Where(x => x.StudentId == StudentId && x.CourseId == courseId).ExecuteDeleteAsync();
        return NoContent();
    }
}
