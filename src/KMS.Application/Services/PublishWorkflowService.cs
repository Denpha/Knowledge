using Microsoft.AspNetCore.Identity;
using KMS.Application.Interfaces;
using KMS.Domain.Entities.Identity;
using KMS.Domain.Enums;

namespace KMS.Application.Services;

public class PublishWorkflowService : IPublishWorkflowService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public PublishWorkflowService(
        UserManager<AppUser> userManager,
        RoleManager<Role> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> CanPublishDirectlyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        // Check if user has Admin or Faculty role
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(r => r == "Admin" || r == "Faculty");
    }

    public async Task<bool> RequiresReviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return true; // Default to requiring review for unknown users

        // Check if user is a Researcher
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(r => r == "Researcher");
    }

    public async Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return new List<string>();

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        return await _userManager.IsInRoleAsync(user, roleName);
    }

    public async Task<PublishMode> GetPublishModeForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return PublishMode.ReviewRequired;

        var roles = await _userManager.GetRolesAsync(user);
        
        if (roles.Any(r => r == "Admin" || r == "Faculty"))
        {
            return PublishMode.Direct;
        }
        else if (roles.Any(r => r == "Researcher"))
        {
            return PublishMode.ReviewRequired;
        }
        else
        {
            return PublishMode.ReviewRequired; // Default for other roles
        }
    }

    public async Task<bool> CanReviewArticlesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        // Admin, Faculty, and certain other roles can review articles
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(r => r == "Admin" || r == "Faculty");
    }

    public async Task<bool> CanApprovePublicationAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        // Only Admin and Faculty can approve publications
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(r => r == "Admin" || r == "Faculty");
    }

    public async Task<bool> CanBypassReviewAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return false;

        // Only Admin and Faculty can bypass review
        var roles = await _userManager.GetRolesAsync(user);
        return roles.Any(r => r == "Admin" || r == "Faculty");
    }

    public async Task<List<Guid>> GetReviewersAsync(CancellationToken cancellationToken = default)
    {
        // Get all users with Admin or Faculty role
        var reviewers = new List<Guid>();
        
        // Get Admin role users
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        reviewers.AddRange(adminUsers.Select(u => u.Id));

        // Get Faculty role users
        var facultyUsers = await _userManager.GetUsersInRoleAsync("Faculty");
        reviewers.AddRange(facultyUsers.Select(u => u.Id));

        return reviewers.Distinct().ToList();
    }
}

public interface IPublishWorkflowService
{
    Task<bool> CanPublishDirectlyAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> RequiresReviewAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);
    Task<PublishMode> GetPublishModeForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanReviewArticlesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanApprovePublicationAsync(Guid userId, Guid articleId, CancellationToken cancellationToken = default);
    Task<bool> CanBypassReviewAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetReviewersAsync(CancellationToken cancellationToken = default);
}