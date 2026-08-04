using System.Security.Claims;
using MorWalPizVideo.Domain.Interfaces;
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
    IUserRepository userRepository,
    IUserChannelOwnerRepository userChannelOwnerRepository) : IVideoAuthorizationService
{
    public async Task<bool> IsAdminAsync(ClaimsPrincipal principal)
    {
        if (principal.IsInRole("Admin"))
        {
            return true;
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var user = await userRepository.GetItemAsync(userId);
         return user is { IsActive: true } &&
             string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase);
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