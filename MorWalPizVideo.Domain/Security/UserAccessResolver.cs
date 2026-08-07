using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Security;

public interface IUserAccessResolver
{
  Task<UserAccessProfile?> ResolveAsync(string userId);
}

public sealed record UserAccessProfile(
    User User,
    IReadOnlySet<string> GroupCodes,
    IReadOnlySet<string> DirectPermissions,
    IReadOnlySet<string> EffectivePermissions);

public sealed class UserAccessResolver(
    IUserRepository userRepository,
    IUserGroupRepository userGroupRepository) : IUserAccessResolver
{
  public async Task<UserAccessProfile?> ResolveAsync(string userId)
  {
    if (string.IsNullOrWhiteSpace(userId))
    {
      return null;
    }

    var user = await userRepository.GetItemAsync(userId);
    if (user is null || !user.IsActive)
    {
      return null;
    }

    var directPermissions = (user.DirectPermissions ?? [])
        .Select(Normalize)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (user.CanAccessBackoffice)
    {
      directPermissions.Add(AuthorizationPermissionKeys.CanAccessBackoffice);
    }

    var groups = await userGroupRepository.GetByIdsAsync(user.GroupIds ?? []);
    var activeGroups = groups.Where(group => group.IsActive).ToList();

    var groupCodes = activeGroups
        .Select(group => Normalize(group.Code))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var inheritedPermissions = activeGroups
        .SelectMany(group => group.Permissions)
        .Select(Normalize)
        .Where(value => !string.IsNullOrWhiteSpace(value));

    var effectivePermissions = directPermissions
        .Union(inheritedPermissions, StringComparer.OrdinalIgnoreCase)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    return new UserAccessProfile(user, groupCodes, directPermissions, effectivePermissions);
  }

  public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
