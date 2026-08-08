using System.Security.Claims;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public interface IVideoAuthorizationService
{
    Task<bool> IsAdminAsync(ClaimsPrincipal principal);
    Task<bool> CanAccessAsync(ClaimsPrincipal principal, YouTubeContent match);
    Task<bool> CanManageChannelAsync(ClaimsPrincipal principal, string channelId);
}

public sealed class VideoAuthorizationService(
    IUserAccessResolver userAccessResolver,
    IUserChannelOwnerRepository userChannelOwnerRepository) : IVideoAuthorizationService
{
    public async Task<bool> IsAdminAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var profile = await userAccessResolver.ResolveAsync(userId);
        if (profile?.GroupCodes.Contains(AuthorizationGroupCodes.Admin, StringComparer.OrdinalIgnoreCase) == true ||
            profile?.EffectivePermissions.Contains(AuthorizationPermissionKeys.BackofficeManageAll, StringComparer.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        return principal.FindAll("permission")
            .Select(claim => UserAccessResolver.Normalize(claim.Value))
            .Contains(AuthorizationPermissionKeys.BackofficeManageAll, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> CanAccessAsync(ClaimsPrincipal principal, YouTubeContent match)
    {
        if (await IsAdminAsync(principal))
        {
            return true;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) &&
               (string.Equals(match.CreatorUserId, userId, StringComparison.Ordinal) ||
                (await userChannelOwnerRepository.GetByUserIdAsync(userId)).Any(owner =>
                    owner.IsActive && match.VideoRefs.Any(video => video.ChannelIds.Contains(owner.ChannelId))));
    }

    public async Task<bool> CanManageChannelAsync(ClaimsPrincipal principal, string channelId)
    {
        if (await IsAdminAsync(principal))
        {
            return true;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) &&
            (await userChannelOwnerRepository.GetByUserIdAsync(userId)).Any(owner =>
                owner.IsActive && owner.ChannelId == channelId);
    }
}