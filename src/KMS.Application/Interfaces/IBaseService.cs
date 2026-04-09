using KMS.Application.DTOs;

namespace KMS.Application.Interfaces;

public interface IBaseService<TDto, TCreateDto, TUpdateDto, TSearchParams>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
    where TSearchParams : class
{
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaginatedResult<TDto>> SearchAsync(TSearchParams searchParams, CancellationToken cancellationToken = default);
    Task<TDto> CreateAsync(TCreateDto createDto, Guid createdById, CancellationToken cancellationToken = default);
    Task<TDto> UpdateAsync(Guid id, TUpdateDto updateDto, Guid updatedById, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid deletedById, CancellationToken cancellationToken = default);
}