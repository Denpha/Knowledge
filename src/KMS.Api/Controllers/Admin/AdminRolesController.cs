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
public class AdminRolesController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public AdminRolesController(
        UserManager<AppUser> userManager,
        RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<AdminRoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var result = new List<AdminRoleDto>(roles.Count);
        foreach (var role in roles)
        {
            result.Add(new AdminRoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                UserCount = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty) is var usersInRole
                    ? usersInRole.Count
                    : 0
            });
        }

        return Ok(result);
    }

    [HttpPost("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminRoleDto>> CreateRole([FromBody] CreateAdminRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Role name is required.");

        if (await _roleManager.RoleExistsAsync(request.Name))
            return Conflict($"Role '{request.Name}' already exists.");

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return CreatedAtAction(nameof(GetRoles), new AdminRoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            UserCount = 0
        });
    }

    [HttpPut("roles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminRoleDto>> UpdateRole(Guid id, [FromBody] UpdateAdminRoleRequest request)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return NotFound("Role not found.");

        if (request.Name is not null)
        {
            if (await _roleManager.RoleExistsAsync(request.Name) && role.Name != request.Name)
                return Conflict($"Role '{request.Name}' already exists.");
            role.Name = request.Name;
        }

        if (request.Description is not null)
            role.Description = request.Description;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var userCount = (await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty)).Count;
        return Ok(new AdminRoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            UserCount = userCount
        });
    }

    [HttpDelete("roles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return NotFound("Role not found.");

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        if (usersInRole.Count > 0)
            return BadRequest($"Cannot delete role '{role.Name}' — {usersInRole.Count} user(s) are assigned to it.");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }
}
