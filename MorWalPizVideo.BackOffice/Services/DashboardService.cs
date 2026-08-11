using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public sealed record DashboardSummaryResponse(
    long TotalShortLinks,
    long TotalShortLinkClicks,
    DateTime? LastBackOfficeLoginAt,
    long ActiveUsers,
    int PublishedVideos,
    int ActiveForms,
    int FormResponses,
    int PendingInsights,
    DateTime GeneratedAt);

public sealed record VideoPublicationItemResponse(string Id, string Title, DateTime PublishedAt);

public sealed record VideoPublicationDayResponse(
    DateTime Date,
    int Count,
    IReadOnlyList<VideoPublicationItemResponse> Videos);

public sealed class DashboardService(
    IShortLinkRepository shortLinkRepository,
    IUserRepository userRepository,
    IUserGroupRepository userGroupRepository,
    IYouTubeContentRepository youTubeContentRepository,
    ICustomFormRepository customFormRepository,
    ICustomFormResponseRepository customFormResponseRepository,
    IInsightNewsItemRepository insightNewsItemRepository) : IDashboardService
{
    private const int DefaultPublicationDays = 21;

    public async Task<DashboardSummaryResponse> GetSummaryAsync(string channelId)
    {
        var users = await userRepository.GetItemsAsync(user => user.IsActive);
        var groups = await userGroupRepository.GetItemsAsync(group => group.IsActive);
        var groupPermissions = groups.ToDictionary(
            group => group.Id,
            group => group.Permissions.Select(UserAccessResolver.Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var backOfficeUsers = users.Where(user =>
            user.CanAccessBackoffice ||
            user.DirectPermissions.Any(permission =>
                string.Equals(UserAccessResolver.Normalize(permission), AuthorizationPermissionKeys.BackofficeAccess, StringComparison.OrdinalIgnoreCase)) ||
            user.GroupIds.Any(groupId => groupPermissions.TryGetValue(groupId, out var permissions) &&
                (permissions.Contains(AuthorizationPermissionKeys.BackofficeAccess) ||
                 permissions.Contains(AuthorizationPermissionKeys.BackofficeManageAll))));

        var standaloneLinks = await shortLinkRepository.GetItemsAsync(shortLink =>
            shortLink.LinkType != LinkType.YouTubeVideo &&
            (shortLink.ManagementChannelId == channelId || shortLink.ChannelId == channelId));
        var scopedMatches = await youTubeContentRepository.GetItemsAsync(match =>
            match.OwnerChannelId == channelId ||
            match.VideoRefs.Any(video => video.ChannelIds.Contains(channelId)));
        var embeddedVideoLinks = scopedMatches
            .SelectMany(match => match.ShortLinks.Where(link =>
                link.LinkType == LinkType.YouTubeVideo &&
                match.VideoRefs.Any(video => video.YoutubeId == link.Target)))
            .ToList();
        var shortLinks = standaloneLinks
            .Concat(embeddedVideoLinks)
            .GroupBy(link => link.NormalizedCode, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        var forms = await customFormRepository.GetActiveAsync();
        var responseCounts = await Task.WhenAll(forms.Select(form =>
            customFormResponseRepository.CountByFormIdAsync(form.Id)));
        var insights = await insightNewsItemRepository.GetItemsAsync(item => item.ChannelId == channelId);
        var publications = await GetVideoPublicationsAsync(DefaultPublicationDays, channelId);

        return new DashboardSummaryResponse(
            shortLinks.Count,
            shortLinks.Sum(shortLink => (long)shortLink.ClicksCount),
            backOfficeUsers.Select(user => user.LastLogin).Where(value => value.HasValue).Max(),
            backOfficeUsers.LongCount(),
            publications.Sum(day => day.Count),
            forms.Count,
            responseCounts.Sum(),
            insights.Count(item => item.Status == InsightNewsStatus.Pending || item.Status == InsightNewsStatus.AutoDetected),
            DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<VideoPublicationDayResponse>> GetVideoPublicationsAsync(int days, string channelId)
    {
        var safeDays = Math.Clamp(days, 1, 21);
        var today = DateTime.UtcNow.Date;
        var fromInclusive = today.AddDays(-(safeDays - 1));
        var toExclusive = today.AddDays(1);
        var publications = await youTubeContentRepository.GetPublicationsAsync(fromInclusive, toExclusive, channelId);

        return publications
            .GroupBy(publication => publication.PublishedAt.ToUniversalTime().Date)
            .OrderBy(group => group.Key)
            .Select(group => new VideoPublicationDayResponse(
                group.Key,
                group.Count(),
                group.Select(video => new VideoPublicationItemResponse(video.VideoId, video.Title, video.PublishedAt))
                    .OrderBy(video => video.PublishedAt)
                    .ToArray()))
            .ToArray();
    }
}