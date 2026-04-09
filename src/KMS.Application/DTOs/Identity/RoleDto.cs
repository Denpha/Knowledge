namespace KMS.Application.DTOs.Identity;

public class RoleDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
}

public class CreateRoleDto : CreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRoleDto : UpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}