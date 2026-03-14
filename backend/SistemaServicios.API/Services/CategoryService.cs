using SistemaServicios.API.DTOs.Categories;
using SistemaServicios.API.Interfaces;

namespace SistemaServicios.API.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var categories = await _categoryRepository.GetAllActiveAsync();
        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            throw new KeyNotFoundException($"Categoría con ID {id} no encontrada.");
        }

        return MapToDto(category);
    }

    private static CategoryDto MapToDto(SistemaServicios.API.Models.Category c) =>
        new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IconUrl = c.IconUrl,
            IsActive = c.IsActive,
        };
}
