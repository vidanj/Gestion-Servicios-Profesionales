using SistemaServicios.API.DTOs.Categories;

namespace SistemaServicios.API.Interfaces;

public interface ICategoryService
{
    public Task<IEnumerable<CategoryDto>> GetCategoriesAsync();

    public Task<CategoryDto> GetCategoryByIdAsync(int id);
}
