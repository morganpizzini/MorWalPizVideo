using System.Security.Claims;
using MorWalPizVideo.BackOffice.Authentication;
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
    Task<bool> CanReadInChannelAsync(ClaimsPrincipal principal, YouTubeContent match, string channelId);
    Task<bool> CanMutateInChannelAsync(ClaimsPrincipal principal, YouTubeContent match, string channelId);
}

public sealed class VideoAuthorizationService(
    IUserAccessResolver userAccessResolver,
    IUserChannelOwnerRepository userChannelOwnerRepository) : IVideoAuthorizationService
{
    public async Task<bool> IsAdminAsync(ClaimsPrincipal principal)
    {
        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
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

        return !principal.HasClaim("impersonation", "true") && principal.FindAll("permission")
            .Select(claim => UserAccessResolver.Normalize(claim.Value))
            .Contains(AuthorizationPermissionKeys.BackofficeManageAll, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> CanAccessAsync(ClaimsPrincipal principal, YouTubeContent match)
    {
        if (await IsAdminAsync(principal))
        {
            return true;
        }

        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
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

        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
        return !string.IsNullOrWhiteSpace(userId) &&
            (await userChannelOwnerRepository.GetByUserIdAsync(userId)).Any(owner =>
                owner.IsActive && owner.ChannelId == channelId);
    }

    public async Task<bool> CanReadInChannelAsync(ClaimsPrincipal principal, YouTubeContent match, string channelId)
    {
        if (await IsAdminAsync(principal) || principal.FindFirst("ApiKeyId") is not null)
        {
            return match.OwnerChannelId == channelId || match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId));
        }

        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
        var ownsChannel = !string.IsNullOrWhiteSpace(userId) &&
            (await userChannelOwnerRepository.GetByUserIdAsync(userId)).Any(owner =>
                owner.IsActive && owner.ChannelId == channelId);
        return ownsChannel && (match.OwnerChannelId == channelId || match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)));
    }

    public async Task<bool> CanMutateInChannelAsync(ClaimsPrincipal principal, YouTubeContent match, string channelId)
    {
        if (principal.FindFirst("ApiKeyId") is not null)
        {
            return match.OwnerChannelId == channelId;
        }

        if (await IsAdminAsync(principal))
        {
            return true;
        }

        var userId = ImpersonationClaimsTransformation.GetEffectiveUserId(principal);
        return !string.IsNullOrWhiteSpace(userId) &&
            match.OwnerChannelId == channelId &&
            (await userChannelOwnerRepository.GetByUserIdAsync(userId)).Any(owner =>
                owner.IsActive && owner.ChannelId == channelId);
    }
}