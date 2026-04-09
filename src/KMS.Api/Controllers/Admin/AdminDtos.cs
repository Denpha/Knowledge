namespace KMS.Api.Controllers.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullNameTh { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int ArticleCount { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
}

public class AdminRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
}

public class UpdateAdminUserRequest
{
    public bool? IsActive { get; set; }
    public List<Guid>? RoleIds { get; set; }
}

public class UpdateUserLockoutRequest
{
    public int LockMinutes { get; set; } = 1440;
}

public class ResetUserPasswordRequest
{
    public string? NewPassword { get; set; }
}

public class ResetUserPasswordResponse
{
    public Guid UserId { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
    public AdminUserDto UpdatedUser { get; set; } = new();
}

public class AdminSystemSettingDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
}

public class UpsertAdminSystemSettingRequest
{
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public bool IsEncrypted { get; set; }
}

public class AdminSettingPolicyDto
{
    public string Key { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string";
    public bool IsLocked { get; set; }
    public bool RequiresEncrypted { get; set; }
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public string? Description { get; set; }
}

public class CreateAdminRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateAdminRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
