namespace KMS.Application.DTOs.Knowledge;

public class CategoryDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public int ArticleCount { get; set; }
    public int SubCategoryCount { get; set; }
    
    public List<CategoryDto>? SubCategories { get; set; }
}

public class CreateCategoryDto : CreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int Order { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryDto : UpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int? Order { get; set; }
    public bool? IsActive { get; set; }
}