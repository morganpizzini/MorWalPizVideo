using Microsoft.AspNetCore.Mvc;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Authorization;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowUser(AuthorizationPermissionKeys.CanAccessBackoffice)]
public class RbacController(
    IUserRepository userRepository,
    IUserGroupRepository userGroupRepository) : ControllerBase
{
  [HttpGet("users")]
  public async Task<ActionResult<IList<RbacUserSummaryContract>>> GetUsers()
  {
    var users = await userRepository.GetItemsAsync();
    var groups = await userGroupRepository.GetItemsAsync();
    var groupsById = groups.ToDictionary(group => group.Id, StringComparer.OrdinalIgnoreCase);

    var result = users
        .OrderBy(user => user.Username)
        .Select(user => ToUserSummary(user, groupsById))
        .ToList();

    return Ok(result);
  }

  [HttpPut("users/{id}/permissions")]
  public async Task<IActionResult> UpdateUserDirectPermissions(
      string id,
      [FromBody] UpdateUserDirectPermissionsRequestContract request)
  {
    var user = await userRepository.GetItemAsync(id);
    if (user is null)
    {
      return NotFound();
    }

    var normalizedPermissions = NormalizeMany(request.Permissions);
    var updatedUser = user with
    {
      DirectPermissions = normalizedPermissions,
      CanAccessBackoffice = normalizedPermissions.Contains(AuthorizationPermissionKeys.CanAccessBackoffice, StringComparer.OrdinalIgnoreCase)
    };

    await userRepository.UpdateItemAsync(updatedUser);
    return NoContent();
  }

  [HttpPut("users/{id}/groups")]
  public async Task<IActionResult> UpdateUserGroups(
      string id,
      [FromBody] UpdateUserGroupMembershipsRequestContract request)
  {
    var user = await userRepository.GetItemAsync(id);
    if (user is null)
    {
      return NotFound();
    }

    var requestedGroupIds = (request.GroupIds ?? [])
        .Where(groupId => !string.IsNullOrWhiteSpace(groupId))
        .Select(groupId => groupId.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (requestedGroupIds.Count > 0)
    {
      var existingGroups = await userGroupRepository.GetByIdsAsync(requestedGroupIds);
      var existingIds = existingGroups.Select(group => group.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
      var missing = requestedGroupIds.Where(groupId => !existingIds.Contains(groupId)).ToList();
      if (missing.Count > 0)
      {
        return BadRequest(new { message = "One or more groups do not exist", missingGroupIds = missing });
      }
    }

    var updatedUser = user with { GroupIds = requestedGroupIds };
    await userRepository.UpdateItemAsync(updatedUser);
    return NoContent();
  }

  [HttpPost("users/{id}/groups/{groupId}")]
  public async Task<IActionResult> AddUserToGroup(string id, string groupId)
  {
    var user = await userRepository.GetItemAsync(id);
    if (user is null)
    {
      return NotFound();
    }

    var group = await userGroupRepository.GetItemAsync(groupId);
    if (group is null)
    {
      return NotFound();
    }

    var groupIds = (user.GroupIds ?? [])
        .Concat([groupId])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    await userRepository.UpdateItemAsync(user with { GroupIds = groupIds });
    return NoContent();
  }

  [HttpDelete("users/{id}/groups/{groupId}")]
  public async Task<IActionResult> RemoveUserFromGroup(string id, string groupId)
  {
    var user = await userRepository.GetItemAsync(id);
    if (user is null)
    {
      return NotFound();
    }

    var groupIds = (user.GroupIds ?? [])
        .Where(existing => !string.Equals(existing, groupId, StringComparison.OrdinalIgnoreCase))
        .ToList();

    await userRepository.UpdateItemAsync(user with { GroupIds = groupIds });
    return NoContent();
  }

  [HttpGet("groups")]
  public async Task<ActionResult<IList<RbacGroupContract>>> GetGroups()
  {
    var groups = await userGroupRepository.GetItemsAsync();
    var users = await userRepository.GetItemsAsync();

    var result = groups
        .OrderBy(group => group.Code)
        .Select(group => new RbacGroupContract
        {
          Id = group.Id,
          Code = Normalize(group.Code),
          Name = group.Name,
          Description = group.Description,
          IsActive = group.IsActive,
          Permissions = NormalizeMany(group.Permissions),
          MemberCount = users.Count(user => (user.GroupIds ?? []).Contains(group.Id, StringComparer.OrdinalIgnoreCase))
        })
        .ToList();

    return Ok(result);
  }

  [HttpGet("groups/{id}")]
  public async Task<ActionResult<RbacGroupContract>> GetGroup(string id)
  {
    var group = await userGroupRepository.GetItemAsync(id);
    if (group is null)
    {
      return NotFound();
    }

    var users = await userRepository.GetItemsAsync();
    return Ok(new RbacGroupContract
    {
      Id = group.Id,
      Code = Normalize(group.Code),
      Name = group.Name,
      Description = group.Description,
      IsActive = group.IsActive,
      Permissions = NormalizeMany(group.Permissions),
      MemberCount = users.Count(user => (user.GroupIds ?? []).Contains(group.Id, StringComparer.OrdinalIgnoreCase))
    });
  }

  [HttpPost("groups")]
  public async Task<ActionResult<RbacGroupContract>> CreateGroup([FromBody] UpsertRbacGroupRequestContract request)
  {
    var normalizedCode = Normalize(request.Code);
    if (string.IsNullOrWhiteSpace(normalizedCode))
    {
      return BadRequest(new { message = "Group code is required" });
    }

    var existing = await userGroupRepository.GetByCodeAsync(normalizedCode);
    if (existing is not null)
    {
      return Conflict(new { message = "Group code already exists" });
    }

    var group = new UserGroup
    {
      Code = normalizedCode,
      Name = request.Name.Trim(),
      Description = request.Description?.Trim() ?? string.Empty,
      IsActive = request.IsActive,
      Permissions = NormalizeMany(request.Permissions)
    };

    var created = await userGroupRepository.AddItemAsync(group);
    return CreatedAtAction(nameof(GetGroup), new { id = created.Id }, new RbacGroupContract
    {
      Id = created.Id,
      Code = created.Code,
      Name = created.Name,
      Description = created.Description,
      IsActive = created.IsActive,
      Permissions = created.Permissions,
      MemberCount = 0
    });
  }

  [HttpPut("groups/{id}")]
  public async Task<IActionResult> UpdateGroup(string id, [FromBody] UpsertRbacGroupRequestContract request)
  {
    var existingGroup = await userGroupRepository.GetItemAsync(id);
    if (existingGroup is null)
    {
      return NotFound();
    }

    var normalizedCode = Normalize(request.Code);
    if (string.IsNullOrWhiteSpace(normalizedCode))
    {
      return BadRequest(new { message = "Group code is required" });
    }

    var groupWithSameCode = await userGroupRepository.GetByCodeAsync(normalizedCode);
    if (groupWithSameCode is not null && !string.Equals(groupWithSameCode.Id, id, StringComparison.OrdinalIgnoreCase))
    {
      return Conflict(new { message = "Group code already exists" });
    }

    var updatedGroup = existingGroup with
    {
      Code = normalizedCode,
      Name = request.Name.Trim(),
      Description = request.Description?.Trim() ?? string.Empty,
      IsActive = request.IsActive,
      Permissions = NormalizeMany(request.Permissions)
    };

    await userGroupRepository.UpdateItemAsync(updatedGroup);
    return NoContent();
  }

  [HttpPut("groups/{id}/permissions")]
  public async Task<IActionResult> UpdateGroupPermissions(
      string id,
      [FromBody] UpdateUserDirectPermissionsRequestContract request)
  {
    var group = await userGroupRepository.GetItemAsync(id);
    if (group is null)
    {
      return NotFound();
    }

    await userGroupRepository.UpdateItemAsync(group with
    {
      Permissions = NormalizeMany(request.Permissions)
    });

    return NoContent();
  }

  [HttpDelete("groups/{id}")]
  public async Task<IActionResult> DeleteGroup(string id)
  {
    var group = await userGroupRepository.GetItemAsync(id);
    if (group is null)
    {
      return NotFound();
    }

    await userGroupRepository.DeleteItemAsync(id);

    var users = (await userRepository.GetItemsAsync())
        .Where(user => (user.GroupIds ?? []).Contains(id, StringComparer.OrdinalIgnoreCase))
        .ToList();
    foreach (var user in users)
    {
      var updatedIds = (user.GroupIds ?? [])
          .Where(groupId => !string.Equals(groupId, id, StringComparison.OrdinalIgnoreCase))
          .ToList();

      await userRepository.UpdateItemAsync(user with { GroupIds = updatedIds });
    }

    return NoContent();
  }

  private static RbacUserSummaryContract ToUserSummary(
      User user,
      IReadOnlyDictionary<string, UserGroup> groupsById)
  {
    var directPermissions = NormalizeMany(user.DirectPermissions);
    if (user.CanAccessBackoffice)
    {
      directPermissions = directPermissions
          .Concat([AuthorizationPermissionKeys.CanAccessBackoffice])
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
    }

    var userGroupIds = user.GroupIds ?? [];
    var activeGroups = userGroupIds
        .Where(groupId => groupsById.TryGetValue(groupId, out var group) && group.IsActive)
        .Select(groupId => groupsById[groupId])
        .ToList();

    var groupCodes = NormalizeMany(activeGroups.Select(group => group.Code));
    var legacyRole = Normalize(user.Role);
    if (!string.IsNullOrWhiteSpace(legacyRole))
    {
      groupCodes = groupCodes
          .Concat([legacyRole])
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
    }

    var effectivePermissions = directPermissions
        .Concat(activeGroups.SelectMany(group => group.Permissions))
        .Select(Normalize)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    return new RbacUserSummaryContract
    {
      Id = user.Id,
      Username = user.Username,
      Email = user.Email,
      Role = user.Role,
      IsActive = user.IsActive,
      LastLogin = user.LastLogin,
      GroupIds = userGroupIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
      GroupCodes = groupCodes,
      DirectPermissions = directPermissions,
      EffectivePermissions = effectivePermissions,
      CanAccessBackoffice = effectivePermissions.Contains(AuthorizationPermissionKeys.CanAccessBackoffice, StringComparer.OrdinalIgnoreCase)
    };
  }

  private static List<string> NormalizeMany(IEnumerable<string>? values) =>
      (values ?? [])
          .Select(Normalize)
          .Where(value => !string.IsNullOrWhiteSpace(value))
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();

  private static string Normalize(string? value) => UserAccessResolver.Normalize(value);
}
