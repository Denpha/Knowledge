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
public class AdminSettingsController : ControllerBase
{
    private static readonly Regex SettingKeyPattern = new("^[a-zA-Z0-9._:-]+$", RegexOptions.Compiled);
    private static readonly HashSet<string> LockedSettingKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "system.version",
        "database.provider",
        "jwt.issuer",
        "jwt.audience"
    };

    private static readonly HashSet<string> ProtectedKeyPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "security.",
        "jwt.",
        "auth."
    };

    private static readonly Dictionary<string, AdminSettingPolicyDto> SettingPolicies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ui.pageSize.default"] = new AdminSettingPolicyDto
            {
                Key = "ui.pageSize.default",
                ValueType = "int",
                Description = "Default page size for list pages.",
                MinValue = 5,
                MaxValue = 200
            },
            ["feature.ai.enabled"] = new AdminSettingPolicyDto
            {
                Key = "feature.ai.enabled",
                ValueType = "bool",
                Description = "Toggle AI features globally."
            },
            ["search.synonyms.enabled"] = new AdminSettingPolicyDto
            {
                Key = "search.synonyms.enabled",
                ValueType = "bool",
                Description = "Enable synonym expansion in search."
            },
            ["search.boost.weights"] = new AdminSettingPolicyDto
            {
                Key = "search.boost.weights",
                ValueType = "json",
                Description = "JSON object for search field boost weights."
            },
            ["site.baseUrl"] = new AdminSettingPolicyDto
            {
                Key = "site.baseUrl",
                ValueType = "url",
                Description = "Base URL for absolute links in notifications."
            },
            ["jwt.issuer"] = new AdminSettingPolicyDto
            {
                Key = "jwt.issuer",
                ValueType = "string",
                IsLocked = true,
                Description = "Locked auth config."
            },
            ["jwt.audience"] = new AdminSettingPolicyDto
            {
                Key = "jwt.audience",
                ValueType = "string",
                IsLocked = true,
                Description = "Locked auth config."
            },
            ["database.provider"] = new AdminSettingPolicyDto
            {
                Key = "database.provider",
                ValueType = "string",
                IsLocked = true,
                Description = "Locked infrastructure config."
            },
            ["system.version"] = new AdminSettingPolicyDto
            {
                Key = "system.version",
                ValueType = "string",
                IsLocked = true,
                Description = "Locked system version value."
            },
        };

    private readonly ApplicationDbContext _dbContext;

    public AdminSettingsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<List<AdminSystemSettingDto>>> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SystemSettings
            .OrderBy(setting => setting.Group)
            .ThenBy(setting => setting.Key)
            .ToListAsync(cancellationToken);

        return Ok(settings.Select(MapSetting).ToList());
    }

    [HttpGet("settings/policies")]
    public ActionResult<List<AdminSettingPolicyDto>> GetSettingPolicies()
    {
        return Ok(SettingPolicies.Values
            .OrderBy(policy => policy.Key)
            .ToList());
    }

    [HttpPut("settings/{key}")]
    public async Task<ActionResult<AdminSystemSettingDto>> UpsertSetting(
        string key,
        [FromBody] UpsertAdminSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Setting key is required.");
        }

        var trimmedKey = key.Trim();
        if (trimmedKey.Length > 200)
        {
            return BadRequest("Setting key must be 200 characters or less.");
        }

        if (!SettingKeyPattern.IsMatch(trimmedKey))
        {
            return BadRequest("Setting key contains invalid characters.");
        }

        if ((request.Group?.Length ?? 0) > 100)
        {
            return BadRequest("Setting group must be 100 characters or less.");
        }

        if ((request.Description?.Length ?? 0) > 500)
        {
            return BadRequest("Setting description must be 500 characters or less.");
        }

        if ((request.Value?.Length ?? 0) > 4000)
        {
            return BadRequest("Setting value must be 4000 characters or less.");
        }

        var setting = await _dbContext.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == trimmedKey, cancellationToken);

        if (SettingPolicies.TryGetValue(trimmedKey, out var policy) && policy.IsLocked)
        {
            return BadRequest("This setting is locked by policy and cannot be modified.");
        }

        if (setting == null && ProtectedKeyPrefixes.Any(prefix => trimmedKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Protected setting keys cannot be created from admin UI.");
        }

        if (LockedSettingKeys.Contains(trimmedKey))
        {
            return BadRequest("This setting is locked and cannot be modified here.");
        }

        var isSensitiveKey = trimmedKey.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || trimmedKey.Contains("token", StringComparison.OrdinalIgnoreCase)
            || trimmedKey.Contains("password", StringComparison.OrdinalIgnoreCase)
            || trimmedKey.EndsWith(".key", StringComparison.OrdinalIgnoreCase);

        if (isSensitiveKey && !string.IsNullOrWhiteSpace(request.Value) && !request.IsEncrypted)
        {
            return BadRequest("Sensitive keys must be saved with IsEncrypted=true.");
        }

        if (!TryValidateTypedSettingValue(trimmedKey, request.Value, out var typedValidationError))
        {
            return BadRequest(typedValidationError);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? updatedById = null;
        if (Guid.TryParse(userIdValue, out var parsedUserId))
        {
            updatedById = parsedUserId;
        }

        if (setting == null)
        {
            setting = new SystemSetting
            {
                Id = Guid.NewGuid(),
                Key = trimmedKey,
                Value = request.Value,
                Description = request.Description,
                Group = request.Group,
                IsEncrypted = request.IsEncrypted,
                UpdatedAt = DateTime.UtcNow,
                UpdatedById = updatedById
            };

            await _dbContext.SystemSettings.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = request.Value;
            setting.Description = request.Description;
            setting.Group = request.Group;
            setting.IsEncrypted = request.IsEncrypted;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedById = updatedById;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(MapSetting(setting));
    }

    private static AdminSystemSettingDto MapSetting(SystemSetting setting)
    {
        return new AdminSystemSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            Group = setting.Group,
            IsEncrypted = setting.IsEncrypted,
            UpdatedAt = setting.UpdatedAt,
            UpdatedById = setting.UpdatedById
        };
    }

    private static bool TryValidateTypedSettingValue(string key, string? value, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!SettingPolicies.TryGetValue(key, out var policy))
        {
            return true;
        }

        switch (policy.ValueType.ToLowerInvariant())
        {
            case "bool":
                if (!bool.TryParse(value, out _))
                {
                    error = "Value must be boolean (true/false).";
                    return false;
                }
                return true;

            case "int":
                if (!int.TryParse(value, out var intValue))
                {
                    error = "Value must be integer.";
                    return false;
                }

                if (policy.MinValue.HasValue && intValue < policy.MinValue.Value)
                {
                    error = $"Value must be >= {policy.MinValue.Value}.";
                    return false;
                }

                if (policy.MaxValue.HasValue && intValue > policy.MaxValue.Value)
                {
                    error = $"Value must be <= {policy.MaxValue.Value}.";
                    return false;
                }

                return true;

            case "url":
                if (!Uri.TryCreate(value, UriKind.Absolute, out _))
                {
                    error = "Value must be a valid absolute URL.";
                    return false;
                }
                return true;

            case "json":
                try
                {
                    JsonDocument.Parse(value);
                    return true;
                }
                catch
                {
                    error = "Value must be valid JSON.";
                    return false;
                }

            default:
                return true;
        }
    }
}
