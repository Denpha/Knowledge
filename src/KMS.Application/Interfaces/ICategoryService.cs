using KMS.Application.DTOs;
using KMS.Application.DTOs.Knowledge;

namespace KMS.Application.Interfaces;

public interface ICategoryService : IBaseService<CategoryDto, CreateCategoryDto, UpdateCategoryDto, SearchParams>
{
    Task<List<CategoryDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<List<CategoryDto>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task<bool> HasArticlesAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> HasSubcategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default);
}