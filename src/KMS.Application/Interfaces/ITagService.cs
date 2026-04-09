using KMS.Application.DTOs.Knowledge;

namespace KMS.Application.Interfaces;

public interface ITagService : IBaseService<TagDto, CreateTagDto, UpdateTagDto, TagSearchParams>
{
    Task<List<TagDto>> GetPopularTagsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<List<TagDto>> GetByArticleIdAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task<List<TagDto>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
}