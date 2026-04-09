using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using KMS.Application.DTOs.Identity;
using KMS.Application.Interfaces;
using KMS.Application.Validators;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Enums;
using KMS.Infrastructure.Data;

namespace KMS.Api.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IValidator<CreateUserDto> _createUserValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmailService _emailService;

    public AuthController(
        UserManager<AppUser> userManager,
        RoleManager<Role> roleManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration,
        IValidator<CreateUserDto> createUserValidator,
        IValidator<LoginDto> loginValidator,
        ApplicationDbContext dbContext,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _createUserValidator = createUserValidator;
        _loginValidator = loginValidator;
        _dbContext = dbContext;
        _emailService = emailService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register(CreateUserDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the request
            var validationResult = await _createUserValidator.ValidateAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(createDto.Email);
            if (existingUser != null)
            {
                return BadRequest($"User with email '{createDto.Email}' already exists.");
            }

            existingUser = await _userManager.FindByNameAsync(createDto.Username);
            if (existingUser != null)
            {
                return BadRequest($"User with username '{createDto.Username}' already exists.");
            }

            // Create new user
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = createDto.Username,
                Email = createDto.Email,
                FullNameTh = $"{createDto.FirstName ?? ""} {createDto.LastName ?? ""}".Trim(),
                FullNameEn = createDto.FirstName != null && createDto.LastName != null ? $"{createDto.FirstName} {createDto.LastName}" : null,
                Faculty = createDto.Faculty,
                Department = createDto.Department,
                Position = createDto.Position,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, createDto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Assign roles if specified
            if (createDto.RoleIds.Any())
            {
                foreach (var roleId in createDto.RoleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId.ToString());
                    if (role != null)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name!);
                    }
                }
            }
            else
            {
                // Try to assign default role (Student) if it exists
                var studentRole = await _roleManager.FindByNameAsync("Student");
                if (studentRole != null)
                {
                    await _userManager.AddToRoleAsync(user, studentRole.Name!);
                }
            }

            // Generate JWT token
            var token = await GenerateJwtToken(user);

            // Issue refresh token
            var (refreshTokenString, refreshTokenExpiresAt) = await IssueRefreshTokenAsync(user.Id);

            var userDto = user.Adapt<UserDto>();
            userDto.Roles = (await _userManager.GetRolesAsync(user))
                .Select(r => new RoleDto { Name = r })
                .ToList();

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes")),
                RefreshToken = refreshTokenString,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                User = userDto
            };

            _ = Task.Run(async () =>
            {
                try { await _emailService.SendWelcomeAsync(user.Email!, user.UserName!, CancellationToken.None); }
                catch { /* non-critical */ }
            }, CancellationToken.None);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the request
            var validationResult = await _loginValidator.ValidateAsync(loginDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // Find user by username or email
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(loginDto.Username);
            }

            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            if (!user.IsActive)
            {
                return Unauthorized("Account is deactivated.");
            }

            // Check password
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized("Invalid username or password.");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // If 2FA is enabled → return pending state with temp token
            if (user.IsTwoFactorAuthEnabled)
            {
                var tempToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                // Store temp token in a short-lived refresh token slot (15 min, flagged)
                var pending = new RefreshToken
                {
                    UserId = user.Id,
                    Token = "2fa_pending_" + tempToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };
                _dbContext.RefreshTokens.Add(pending);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Ok(new TwoFactorPendingDto { RequiresTwoFactor = true, TempToken = tempToken });
            }

            // Generate JWT token
            var token = await GenerateJwtToken(user);

            // Issue refresh token
            var (refreshTokenString, refreshTokenExpiresAt) = await IssueRefreshTokenAsync(user.Id);

            var userDto = user.Adapt<UserDto>();
            userDto.Roles = (await _userManager.GetRolesAsync(user))
                .Select(r => new RoleDto { Name = r })
                .ToList();

            var response = new LoginResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes")),
                RefreshToken = refreshTokenString,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                User = userDto
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/auth/refresh
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> RefreshToken(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest("Refresh token is required.");

            var storedToken = await _dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

            if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt <= DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");

            var user = storedToken.User;
            if (!user.IsActive)
                return Unauthorized("Account is deactivated.");

            // Revoke the old token (rotation)
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Issue new JWT + new refresh token
            var newJwt = await GenerateJwtToken(user);
            var (newRefreshToken, newRefreshTokenExpiresAt) = await IssueRefreshTokenAsync(user.Id);

            var userDto = user.Adapt<UserDto>();
            userDto.Roles = (await _userManager.GetRolesAsync(user))
                .Select(r => new RoleDto { Name = r })
                .ToList();

            var response = new LoginResponseDto
            {
                Token = newJwt,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes")),
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAt = newRefreshTokenExpiresAt,
                User = userDto
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequestDto? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            {
                var storedToken = await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

                if (storedToken != null && !storedToken.IsRevoked)
                {
                    storedToken.IsRevoked = true;
                    storedToken.RevokedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auth/profile
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User not found.");
            }

            var userDto = user.Adapt<UserDto>();
            userDto.Roles = (await _userManager.GetRolesAsync(user))
                .Select(r => new RoleDto { Name = r })
                .ToList();

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/auth/profile
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile(
        UpdateUserDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User not found.");
            }

            // Update user properties
            if (!string.IsNullOrEmpty(updateDto.FirstName) || !string.IsNullOrEmpty(updateDto.LastName))
            {
                var firstName = updateDto.FirstName ?? "";
                var lastName = updateDto.LastName ?? "";
                user.FullNameTh = $"{firstName} {lastName}".Trim();
                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                {
                    user.FullNameEn = $"{firstName} {lastName}";
                }
            }

            if (!string.IsNullOrEmpty(updateDto.Faculty))
                user.Faculty = updateDto.Faculty;

            if (!string.IsNullOrEmpty(updateDto.Department))
                user.Department = updateDto.Department;

            if (!string.IsNullOrEmpty(updateDto.Position))
                user.Position = updateDto.Position;

            if (!string.IsNullOrEmpty(updateDto.Bio))
                user.Bio = updateDto.Bio;

            if (updateDto.IsActive.HasValue)
                user.IsActive = updateDto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            // Update roles if specified
            if (updateDto.RoleIds != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                foreach (var roleId in updateDto.RoleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId.ToString());
                    if (role != null)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name!);
                    }
                }
            }

            var userDto = user.Adapt<UserDto>();
            userDto.Roles = (await _userManager.GetRolesAsync(user))
                .Select(r => new RoleDto { Name = r })
                .ToList();

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User not found.");
            }

            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                return BadRequest("New password and confirmation password do not match.");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Password changed successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // ─── 2FA endpoints ────────────────────────────────────────────────────────

    // POST: api/auth/2fa/verify  — complete login when 2FA is required
    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> VerifyTwoFactor(
        [FromBody] TwoFactorVerifyDto dto,
        CancellationToken cancellationToken = default)
    {
        var pendingKey = "2fa_pending_" + dto.TempToken;
        var pending = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == pendingKey && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

        if (pending == null)
            return Unauthorized("Invalid or expired 2FA session.");

        var user = pending.User;
        if (user == null || string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return Unauthorized("2FA not configured.");

        var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
        var totp = new Totp(secretBytes);
        var valid = totp.VerifyTotp(dto.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        if (!valid)
            return Unauthorized("Invalid 2FA code.");

        // Revoke the pending token
        pending.IsRevoked = true;
        pending.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var token = await GenerateJwtToken(user);
        var (refreshToken, refreshTokenExpiresAt) = await IssueRefreshTokenAsync(user.Id);
        var userDto = user.Adapt<UserDto>();
        userDto.Roles = (await _userManager.GetRolesAsync(user)).Select(r => new RoleDto { Name = r }).ToList();

        return Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes")),
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = userDto
        });
    }

    // GET: api/auth/2fa/setup  — generate new TOTP secret + QR URI
    [HttpGet("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupDto>> SetupTwoFactor(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();

        // Generate new secret
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);

        // Store as pending (not yet confirmed)
        user.TwoFactorSecretKey = secret;
        await _userManager.UpdateAsync(user);

        var issuer = Uri.EscapeDataString(_configuration["Jwt:Issuer"] ?? "KMS");
        var account = Uri.EscapeDataString(user.Email ?? user.UserName!);
        var otpUri = $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";

        return Ok(new TwoFactorSetupDto
        {
            Secret = secret,
            QrCodeUri = otpUri,
            IsEnabled = user.IsTwoFactorAuthEnabled
        });
    }

    // POST: api/auth/2fa/enable  — confirm code and enable 2FA
    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ActionResult> EnableTwoFactor([FromBody] TwoFactorConfirmDto dto, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
            return BadRequest("2FA not set up. Call GET /2fa/setup first.");

        var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey);
        var totp = new Totp(secretBytes);
        var valid = totp.VerifyTotp(dto.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        if (!valid) return BadRequest("Invalid 2FA code.");

        user.IsTwoFactorAuthEnabled = true;
        await _userManager.UpdateAsync(user);
        return Ok(new { message = "2FA enabled successfully." });
    }

    // POST: api/auth/2fa/disable
    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ActionResult> DisableTwoFactor([FromBody] TwoFactorConfirmDto dto, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();
        if (!user.IsTwoFactorAuthEnabled)
            return BadRequest("2FA is not enabled.");

        var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecretKey!);
        var totp = new Totp(secretBytes);
        var valid = totp.VerifyTotp(dto.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        if (!valid) return BadRequest("Invalid 2FA code.");

        user.IsTwoFactorAuthEnabled = false;
        user.TwoFactorSecretKey = null;
        await _userManager.UpdateAsync(user);
        return Ok(new { message = "2FA disabled successfully." });
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private async Task<(string token, DateTime expiresAt)> IssueRefreshTokenAsync(Guid userId)
    {
        var tokenString = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenString,
            ExpiresAt = expiresAt
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return (tokenString, expiresAt);
    }

    private async Task<string> GenerateJwtToken(AppUser user)
    {
        var userRoles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };

        // Add role claims
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpiryInMinutes"));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}