using KMS.Domain.Enums;

namespace KMS.Application.DTOs.Identity;

public class UserDto : BaseDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int ArticleCount { get; set; }
    public List<RoleDto> Roles { get; set; } = new List<RoleDto>();
}

public class CreateUserDto : CreateDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public List<Guid> RoleIds { get; set; } = new List<Guid>();
}

public class UpdateUserDto : UpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Faculty { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool? IsActive { get; set; }
    public List<Guid>? RoleIds { get; set; }
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequestDto
{
    public string? RefreshToken { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
public class TwoFactorPendingDto
{
    public bool RequiresTwoFactor { get; set; } = true;
    public string TempToken { get; set; } = string.Empty;
}

public class TwoFactorVerifyDto
{
    public string TempToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorSetupDto
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class TwoFactorConfirmDto
{
    public string Code { get; set; } = string.Empty;
}
