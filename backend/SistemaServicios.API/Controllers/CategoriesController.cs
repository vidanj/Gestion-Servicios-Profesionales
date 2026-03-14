using Microsoft.AspNetCore.Mvc;
using SistemaServicios.API.DTOs.Categories;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Tags("Módulo de Categorías")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        try
        {
            var categories = await _categoryService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            return Ok(category);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(
                500,
                new { message = "Error interno del servidor.", error = ex.Message }
            );
        }
    }
}
