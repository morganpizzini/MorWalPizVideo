using System.Net;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public interface IChannelNewsService
{
    Task<IList<ChannelNews>> GetForChannelAsync(string channelId);
    Task<ChannelNews?> GetByIdOrSlugAsync(string identifier, string channelId);
    Task<IList<ChannelNews>> GetPublicAsync(IReadOnlyCollection<string> channelIds, DateTime utcNow);
    Task<ChannelNews> CreateAsync(ChannelNews item);
    Task<ChannelNews?> UpdateAsync(ChannelNews item, string channelId);
    Task<bool> DeleteAsync(string id, string channelId);
}

public sealed class ChannelNewsService(IChannelNewsRepository repository) : IChannelNewsService
{
    public Task<IList<ChannelNews>> GetForChannelAsync(string channelId) =>
        repository.GetItemsAsync(item => item.ChannelId == channelId);

    public async Task<ChannelNews?> GetByIdOrSlugAsync(string identifier, string channelId) =>
        (await repository.GetItemsAsync(item => item.ChannelId == channelId &&
            (item.Id == identifier || item.Slug == identifier))).FirstOrDefault();

    public Task<IList<ChannelNews>> GetPublicAsync(IReadOnlyCollection<string> channelIds, DateTime utcNow) =>
        repository.GetItemsAsync(item => channelIds.Contains(item.ChannelId) &&
            (item.Status == ChannelNewsStatus.Published ||
             (item.Status == ChannelNewsStatus.Scheduled && item.PublicationTimeUtc <= utcNow)));

    public async Task<ChannelNews> CreateAsync(ChannelNews item)
    {
        var normalized = Normalize(item) with
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CreationDateTime = DateTime.UtcNow,
            UpdatedDateTime = DateTime.UtcNow
        };
        await repository.AddItemAsync(normalized);
        return normalized;
    }

    public async Task<ChannelNews?> UpdateAsync(ChannelNews item, string channelId)
    {
        var existing = await GetByIdOrSlugAsync(item.Id, channelId);
        if (existing is null)
            return null;

        var normalized = Normalize(item) with
        {
            Id = existing.Id,
            ChannelId = existing.ChannelId,
            CreationDateTime = existing.CreationDateTime,
            UpdatedDateTime = DateTime.UtcNow
        };
        await repository.UpdateItemAsync(normalized);
        return normalized;
    }

    public async Task<bool> DeleteAsync(string id, string channelId)
    {
        var existing = await GetByIdOrSlugAsync(id, channelId);
        if (existing is null)
            return false;

        await repository.DeleteItemAsync(existing.Id);
        return true;
    }

    private static ChannelNews Normalize(ChannelNews item)
    {
        if (item.Images.Count > 10)
            throw new ArgumentException("A ChannelNews item can contain at most 10 images.", nameof(item));

        var slug = string.IsNullOrWhiteSpace(item.Slug) ? item.Title : item.Slug;
        slug = Regex.Replace(slug.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return item with
        {
            Title = item.Title.Trim(),
            Subtitle = item.Subtitle.Trim(),
            DescriptionHtml = ChannelNewsHtmlSanitizer.Sanitize(item.DescriptionHtml),
            Slug = slug,
            Images = item.Images.Select((image, index) => image with { DisplayOrder = index }).ToArray(),
            PublicationTimeUtc = item.PublicationTimeUtc?.ToUniversalTime()
        };
    }
}

public static partial class ChannelNewsHtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "em", "u", "s", "ol", "ul", "li", "a", "h2", "h3", "h4", "blockquote"
    };

    public static string Sanitize(string? html)
    {
        var value = html ?? string.Empty;
        value = Regex.Replace(value, "<!--[\\s\\S]*?-->", string.Empty, RegexOptions.IgnoreCase);
        value = Regex.Replace(value, "<\\s*(script|style|iframe|object|embed)[^>]*>[\\s\\S]*?<\\s*/\\s*\\1\\s*>", string.Empty, RegexOptions.IgnoreCase);
        return Regex.Replace(value, "<\\s*(/?)\\s*([a-zA-Z0-9]+)([^>]*)>", match =>
        {
            var closing = match.Groups[1].Value.Length > 0;
            var tag = match.Groups[2].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(tag))
                return string.Empty;
            return closing ? $"</{tag}>" : $"<{tag}{SanitizeAttributes(tag, match.Groups[3].Value)}>";
        }, RegexOptions.IgnoreCase);
    }

    private static string SanitizeAttributes(string tag, string attributes)
    {
        if (tag is not ("a" or "br"))
            return string.Empty;

        var safe = new List<string>();
        foreach (Match attribute in Regex.Matches(attributes, "([a-zA-Z][a-zA-Z0-9-]*)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+))"))
        {
            var name = attribute.Groups[1].Value.ToLowerInvariant();
            var value = attribute.Groups[2].Success ? attribute.Groups[2].Value :
                attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[4].Value;
            if (name == "href" && tag == "a" && IsSafeUrl(value))
                safe.Add($" href=\"{WebUtility.HtmlEncode(value)}\"");
            if (name == "target" && tag == "a" && value is "_blank" or "_self")
                safe.Add($" target=\"{value}\"");
        }
        return string.Concat(safe);
    }

    private static bool IsSafeUrl(string value) =>
        Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri) &&
        (!uri.IsAbsoluteUri || uri.Scheme is "http" or "https" or "mailto");
}