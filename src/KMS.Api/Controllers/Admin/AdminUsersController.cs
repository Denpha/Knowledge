using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using KMS.Domain.Entities;
using KMS.Domain.Entities.Identity;
using KMS.Infrastructure.Data;

namespace KMS.Api.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public AdminUsersController(
        UserManager<AppUser> userManager,
        RoleManager<Role> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .OrderBy(user => user.FullNameTh)
            .ToListAsync(cancellationToken);

        var result = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleIds = await _dbContext.Roles
                .Where(role => roles.Contains(role.Name!))
                .Select(role => role.Id)
                .ToListAsync(cancellationToken);

            result.Add(new AdminUserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullNameTh = user.FullNameTh,
                FullNameEn = user.FullNameEn,
                Faculty = user.Faculty,
                Department = user.Department,
                Position = user.Position,
                IsActive = user.IsActive,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
                LockoutEnd = user.LockoutEnd,
                LastLoginAt = user.LastLoginAt,
                RoleIds = roleIds,
                RoleNames = roles.ToList(),
                ArticleCount = await _dbContext.KnowledgeArticles.CountAsync(article => article.AuthorId == user.Id, cancellationToken)
            });
        }

        return Ok(result);
    }

    [HttpPut("users/{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(updateResult.Errors);
        }

        if (request.RoleIds != null)
        {
            var roles = await _roleManager.Roles
                .Where(role => request.RoleIds.Contains(role.Id))
                .ToListAsync(cancellationToken);

            if (roles.Count != request.RoleIds.Count)
            {
                return BadRequest("One or more roles were not found.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(removeResult.Errors);
                }
            }

            var addResult = await _userManager.AddToRolesAsync(user, roles.Select(role => role.Name!).ToList());
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);
        var updatedRoleIds = await _dbContext.Roles
            .Where(role => updatedRoles.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        return Ok(new AdminUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullNameTh = user.FullNameTh,
            FullNameEn = user.FullNameEn,
            Faculty = user.Faculty,
            Department = user.Department,
            Position = user.Position,
            IsActive = user.IsActive,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            LockoutEnd = user.LockoutEnd,
            LastLoginAt = user.LastLoginAt,
            RoleIds = updatedRoleIds,
            RoleNames = updatedRoles.ToList(),
            ArticleCount = await _dbContext.KnowledgeArticles.CountAsync(article => article.AuthorId == user.Id, cancellationToken)
        });
    }

    [HttpPost("users/{id:guid}/lock")]
    public async Task<ActionResult<AdminUserDto>> LockUser(Guid id, [FromBody] UpdateUserLockoutRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        var lockMinutes = request.LockMinutes <= 0 ? 1440 : request.LockMinutes;
        if (!user.LockoutEnabled)
        {
            user.LockoutEnabled = true;
            await _userManager.UpdateAsync(user);
        }

        var lockUntil = DateTimeOffset.UtcNow.AddMinutes(lockMinutes);
        var lockResult = await _userManager.SetLockoutEndDateAsync(user, lockUntil);
        if (!lockResult.Succeeded)
        {
            return BadRequest(lockResult.Errors);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(await BuildAdminUserDtoAsync(user, cancellationToken));
    }

    [HttpPost("users/{id:guid}/unlock")]
    public async Task<ActionResult<AdminUserDto>> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!unlockResult.Succeeded)
        {
            return BadRequest(unlockResult.Errors);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(await BuildAdminUserDtoAsync(user, cancellationToken));
    }

    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<ActionResult<ResetUserPasswordResponse>> ResetUserPassword(
        Guid id,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(request.NewPassword)
            ? GenerateTemporaryPassword()
            : request.NewPassword;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, token, temporaryPassword);
        if (!resetResult.Succeeded)
        {
            return BadRequest(resetResult.Errors);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new ResetUserPasswordResponse
        {
            UserId = user.Id,
            TemporaryPassword = temporaryPassword,
            UpdatedUser = await BuildAdminUserDtoAsync(user, cancellationToken)
        });
    }

    private async Task<AdminUserDto> BuildAdminUserDtoAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var roleIds = await _dbContext.Roles
            .Where(role => roles.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullNameTh = user.FullNameTh,
            FullNameEn = user.FullNameEn,
            Faculty = user.Faculty,
            Department = user.Department,
            Position = user.Position,
            IsActive = user.IsActive,
            IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            LockoutEnd = user.LockoutEnd,
            LastLoginAt = user.LastLoginAt,
            RoleIds = roleIds,
            RoleNames = roles.ToList(),
            ArticleCount = await _dbContext.KnowledgeArticles.CountAsync(article => article.AuthorId == user.Id, cancellationToken)
        };
    }

    private static string GenerateTemporaryPassword()
    {
        return $"Temp#{Guid.NewGuid():N}"[..14] + "Aa1!";
    }
}
