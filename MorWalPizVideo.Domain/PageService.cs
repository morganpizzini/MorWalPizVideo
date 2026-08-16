using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public interface IPageService
{
    Task<IList<Page>> GetForChannelAsync(string channelId);
    Task<Page?> GetByIdAsync(string id, string channelId);
    Task<Page?> GetPublishedByUrlAsync(string url);
    Task<bool> IsUrlAvailableAsync(string url, string channelId, string? excludingId = null);
    Task<Page> CreateAsync(Page page);
    Task<Page?> UpdateAsync(Page page, string channelId);
    Task<bool> DeleteAsync(string id, string channelId);
    IReadOnlyList<string> Validate(Page page);
}

public sealed class PageService(
    IPageRepository pageRepository,
    IChannelNavigationRepository navigationRepository,
    IBlobService blobService,
    IOptions<BlobStorageOptions> blobOptions) : IPageService
{
    public Task<IList<Page>> GetForChannelAsync(string channelId) =>
        pageRepository.GetItemsAsync(page => page.ChannelId == channelId);

    public async Task<Page?> GetByIdAsync(string id, string channelId) =>
        (await pageRepository.GetItemsAsync(page => page.Id == id && page.ChannelId == channelId))
            .FirstOrDefault();

    public async Task<Page?> GetPublishedByUrlAsync(string url)
    {
        var normalizedUrl = NormalizeUrl(url);
        var pages = await pageRepository.GetItemsAsync(page =>
            page.Url == normalizedUrl && page.Status == PageStatus.Published);
        return pages.OrderByDescending(page => page.UpdatedDateTime).FirstOrDefault();
    }

    public async Task<bool> IsUrlAvailableAsync(string url, string channelId, string? excludingId = null)
    {
        var existing = await pageRepository.GetByUrlAsync(NormalizeUrl(url));
        return existing is null || string.Equals(existing.Id, excludingId, StringComparison.Ordinal);
    }

    public async Task<Page> CreateAsync(Page page)
    {
        var normalized = Normalize(page) with
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CreationDateTime = DateTime.UtcNow,
            UpdatedDateTime = DateTime.UtcNow
        };
        await pageRepository.AddItemAsync(normalized);
        return normalized;
    }

    public async Task<Page?> UpdateAsync(Page page, string channelId)
    {
        var existing = await GetByIdAsync(page.Id, channelId);
        if (existing is null)
            return null;

        var normalized = Normalize(page) with
        {
            Id = existing.Id,
            ChannelId = existing.ChannelId,
            CreationDateTime = existing.CreationDateTime,
            UpdatedDateTime = DateTime.UtcNow
        };
        await pageRepository.UpdateItemAsync(normalized);
        return normalized;
    }

    public async Task<bool> DeleteAsync(string id, string channelId)
    {
        var existing = await GetByIdAsync(id, channelId);
        if (existing is null)
            return false;

        await pageRepository.DeleteItemAsync(existing.Id);
        var navigations = await navigationRepository.GetItemsAsync();
        foreach (var navigation in navigations.Where(item =>
            item.HeaderItems.Any(item => item.PageId == existing.Id) ||
            item.FooterItems.Any(item => item.PageId == existing.Id)))
        {
            await navigationRepository.UpdateItemAsync(navigation with
            {
                HeaderItems = navigation.HeaderItems.Where(item => item.PageId != existing.Id).ToArray(),
                FooterItems = navigation.FooterItems.Where(item => item.PageId != existing.Id).ToArray(),
                UpdatedDateTime = DateTime.UtcNow
            });
        }

        foreach (var image in existing.InlineImages.Where(image => !string.IsNullOrWhiteSpace(image.StorageKey)))
            await blobService.DeleteImageAsync(image.StorageKey, blobOptions.Value.PageContainerName);

        return true;
    }

    public IReadOnlyList<string> Validate(Page page)
    {
        var errors = new List<string>();
        var normalizedUrl = NormalizeUrl(page.Url);
        if (string.IsNullOrWhiteSpace(page.ChannelId))
            errors.Add("Channel owner is required.");
        if (string.IsNullOrWhiteSpace(page.Title) || page.Title.Trim().Length > 200)
            errors.Add("Title is required and must be at most 200 characters.");
        if (!Regex.IsMatch(normalizedUrl, "^[a-z0-9](?:[a-z0-9_-]{0,118}[a-z0-9])?$"))
            errors.Add("Url must be an ASCII slug between 1 and 120 characters.");
        if (!Enum.IsDefined(page.Status))
            errors.Add("Status is invalid.");
        return errors;
    }

    public static string NormalizeUrl(string value) =>
        value.Trim().Trim('/').ToLowerInvariant();

    private static Page Normalize(Page page) => page with
    {
        ChannelId = page.ChannelId.Trim(),
        ThumbnailUrl = page.ThumbnailUrl.Trim(),
        Title = page.Title.Trim(),
        Content = ChannelNewsHtmlSanitizer.Sanitize(
            page.Content,
            page.InlineImages.Select(image => image.PublicUrl).ToHashSet(StringComparer.Ordinal)),
        Url = NormalizeUrl(page.Url),
        VideoId = page.VideoId.Trim(),
        VideoReelIds = page.VideoReelIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray() ?? [],
        ShortReelIds = page.ShortReelIds?.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray() ?? []
    };
}

public interface IChannelNavigationService
{
    Task<ChannelNavigation?> GetForChannelAsync(string channelId);
    Task<ChannelNavigation> SaveAsync(ChannelNavigation navigation);
    Task<PublicNavigation?> GetPublicAsync();
    IReadOnlyList<string> Validate(ChannelNavigation navigation, IReadOnlyCollection<Page> pages);
}

public sealed class ChannelNavigationService(
    IChannelNavigationRepository navigationRepository,
    IPageRepository pageRepository) : IChannelNavigationService
{
    public Task<ChannelNavigation?> GetForChannelAsync(string channelId) =>
        navigationRepository.GetByChannelIdAsync(channelId);

    public async Task<ChannelNavigation> SaveAsync(ChannelNavigation navigation)
    {
        var normalized = Normalize(navigation);
        var existing = await navigationRepository.GetByChannelIdAsync(normalized.ChannelId);
        if (existing is null)
        {
            return await navigationRepository.AddItemAsync(normalized with
            {
                Id = ObjectId.GenerateNewId().ToString(),
                CreationDateTime = DateTime.UtcNow,
                UpdatedDateTime = DateTime.UtcNow
            });
        }

        var updated = normalized with
        {
            Id = existing.Id,
            CreationDateTime = existing.CreationDateTime,
            UpdatedDateTime = DateTime.UtcNow
        };
        await navigationRepository.UpdateItemAsync(updated);
        return updated;
    }

    public async Task<PublicNavigation?> GetPublicAsync()
    {
        var navigations = await navigationRepository.GetItemsAsync(item => item.IsActive);
        if (navigations.Count > 1)
            throw new InvalidOperationException("Public navigation is misconfigured: more than one active channel navigation exists.");

        var navigation = navigations.SingleOrDefault();
        if (navigation is null)
            return null;

        var pages = (await pageRepository.GetItemsAsync(page =>
            page.ChannelId == navigation.ChannelId && page.Status == PageStatus.Published))
            .ToDictionary(page => page.Id, StringComparer.Ordinal);
        return new PublicNavigation(
            Project(navigation.HeaderItems, pages),
            navigation.FooterColumnCount,
            Project(navigation.FooterItems, pages));
    }

    public IReadOnlyList<string> Validate(ChannelNavigation navigation, IReadOnlyCollection<Page> pages)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(navigation.ChannelId))
            errors.Add("Channel owner is required.");
        if (navigation.FooterColumnCount is < 1 or > 8)
            errors.Add("Footer columns must be between 1 and 8.");

        foreach (var item in navigation.HeaderItems.Concat(navigation.FooterItems))
        {
            if (string.IsNullOrWhiteSpace(item.DisplayText) || item.DisplayText.Trim().Length > 120)
                errors.Add("Menu item display text is required and must be at most 120 characters.");
            if (!Enum.IsDefined(item.Type))
                errors.Add("Menu item type is invalid.");
            if (item.Type == NavigationItemType.Page &&
                (string.IsNullOrWhiteSpace(item.PageId) || pages.All(page => page.Id != item.PageId)))
                errors.Add("Menu page links must reference a page in the selected channel.");
            if (item.Type != NavigationItemType.Page && !IsSafeTarget(item.TargetUrl, item.Type))
                errors.Add("Menu links must use safe internal or external URLs.");
            if (item.Column < 0 || item.Column >= navigation.FooterColumnCount)
                errors.Add("Footer item column is outside the configured column count.");
        }
        return errors;
    }

    private static ChannelNavigation Normalize(ChannelNavigation navigation) => navigation with
    {
        ChannelId = navigation.ChannelId.Trim(),
        FooterColumnCount = Math.Clamp(navigation.FooterColumnCount, 1, 8),
        HeaderItems = NormalizeItems(navigation.HeaderItems, false),
        FooterItems = NormalizeItems(navigation.FooterItems, true)
    };

    private static IReadOnlyList<NavigationMenuItem> NormalizeItems(IEnumerable<NavigationMenuItem>? items, bool footer) =>
        (items ?? []).Select((item, index) => item with
        {
            DisplayText = item.DisplayText.Trim(),
            TargetUrl = item.TargetUrl.Trim(),
            Column = footer ? Math.Max(0, item.Column) : 0,
            DisplayOrder = index
        }).ToArray();

    private static IReadOnlyList<PublicNavigationItem> Project(
        IEnumerable<NavigationMenuItem> items,
        IReadOnlyDictionary<string, Page> pages) =>
        items.OrderBy(item => item.Column).ThenBy(item => item.DisplayOrder)
            .Where(item => item.Type != NavigationItemType.Page ||
                (item.PageId is not null && pages.ContainsKey(item.PageId)))
            .Select(item => new PublicNavigationItem(
                item.DisplayText,
                item.Type == NavigationItemType.Page
                    ? $"/pages/{pages[item.PageId!].Url}"
                    : item.TargetUrl,
                item.Type == NavigationItemType.External,
                item.Column,
                item.DisplayOrder))
            .ToArray();

    private static bool IsSafeTarget(string value, NavigationItemType type)
    {
        if (type == NavigationItemType.Internal)
            return value.StartsWith('/') && !value.StartsWith("//") && !value.Contains('\\');
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrWhiteSpace(uri.UserInfo);
    }
}
