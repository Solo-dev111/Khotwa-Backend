using Khotwa.DTOs;
using Khotwa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Khotwa.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : Controller
    {
        private readonly CategoryService _service;

        public CategoriesController(CategoryService service)
        {
            _service = service;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _service.GetAllAsync(
                search, pageNumber, pageSize);

            return Ok(result);
        }

        // GET: api/categories/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound("Category not found");

            return Ok(category);
        }

        // POST: api/categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] SaveCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Category name is required");

            var category = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = category.Id },
                category);
        }

        // PUT: api/categories/1
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] SaveCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Category name is required");

            bool updated = await _service.UpdateAsync(id, dto);

            if (!updated)
                return NotFound("Category not found");

            return NoContent();
        }

        // DELETE: api/categories/1
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (result == DeleteCategoryResult.NotFound)
                return NotFound("Category not found");

            if (result == DeleteCategoryResult.HasCourses)
                return Conflict(
                    "Cannot delete a category that contains courses");

            return NoContent();
        }
    }
}
