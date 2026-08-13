using System.Text.RegularExpressions;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Server.Services;

public interface IQuickLinksService
{
    Task<IList<QuickLinks>> GetAllAsync();
    Task<QuickLinks?> GetByIdAsync(string id);
    Task<QuickLinks?> GetByUrlAsync(string url);
    Task<bool> IsUrlAvailableAsync(string url, string? excludingId = null);
    Task<QuickLinks> CreateAsync(QuickLinks entity);
    Task UpdateAsync(QuickLinks entity);
    Task DeleteAsync(string id);
}

public sealed class QuickLinksService(IQuickLinksRepository repository) : IQuickLinksService
{
    public async Task<IList<QuickLinks>> GetAllAsync()
        => (await repository.GetItemsAsync()).OrderBy(entity => entity.Title).ToList();

    public Task<QuickLinks?> GetByIdAsync(string id) => repository.GetItemAsync(id);

    public Task<QuickLinks?> GetByUrlAsync(string url)
        => repository.GetByUrlAsync(QuickLinks.NormalizeUrl(url));

    public async Task<bool> IsUrlAvailableAsync(string url, string? excludingId = null)
    {
        var existing = await repository.GetByUrlAsync(QuickLinks.NormalizeUrl(url));
        return existing is null || string.Equals(existing.Id, excludingId, StringComparison.Ordinal);
    }

    public Task<QuickLinks> CreateAsync(QuickLinks entity)
        => repository.AddItemAsync(Normalize(entity));

    public Task UpdateAsync(QuickLinks entity)
        => repository.UpdateItemAsync(Normalize(entity));

    public Task DeleteAsync(string id) => repository.DeleteItemAsync(id);

    public static QuickLinks Normalize(QuickLinks entity)
        => entity with
        {
            Title = CleanText(entity.Title, 160),
            Subtitle = CleanText(entity.Subtitle, 300),
            Url = QuickLinks.NormalizeUrl(entity.Url),
            Links = entity.Links?.Select(Normalize).ToArray() ?? []
        };

    public static IReadOnlyList<string> Validate(QuickLinks entity)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(entity.Title) || entity.Title.Trim().Length > 160)
            errors.Add("Title is required and must be at most 160 characters.");

        var normalizedUrl = QuickLinks.NormalizeUrl(entity.Url);
        if (!Regex.IsMatch(normalizedUrl, "^[a-z0-9](?:[a-z0-9_-]{0,78}[a-z0-9])?$"))
            errors.Add("Url must be an ASCII shortlink slug between 1 and 80 characters.");

        if (entity.Links is null || entity.Links.Length > 100)
            errors.Add("Links must contain between 0 and 100 items.");
        else
        {
            for (var index = 0; index < entity.Links.Length; index++)
            {
                var link = entity.Links[index];
                if (!Enum.IsDefined(link.Kind) || !IsSafeHttpUrl(link.TargetUrl) || !IsAllowedHost(link.Kind, link.TargetUrl))
                    errors.Add($"Link {index + 1} must use a supported kind and safe target URL.");
            }
        }

        return errors;
    }

    private static QuickLink Normalize(QuickLink link)
        => link with
        {
            TargetUrl = link.TargetUrl.Trim(),
            Title = CleanText(link.Title, 160),
            Subtitle = CleanText(link.Subtitle, 300),
            Label = CleanText(link.Label, 120),
            ImageUrl = CleanText(link.ImageUrl, 2000),
            Icon = CleanText(link.Icon, 80),
            Provider = CleanText(link.Provider, 80)
        };

    private static string? CleanText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new string(value.Trim().Where(character => !char.IsControl(character)).Take(maxLength).ToArray());
    }

    private static bool IsSafeHttpUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
           string.IsNullOrWhiteSpace(uri.UserInfo);

    private static bool IsAllowedHost(QuickLinkKind kind, string targetUrl)
    {
        if (kind == QuickLinkKind.External)
            return true;

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.TrimStart('.').ToLowerInvariant();
        return kind switch
        {
            QuickLinkKind.Telegram => IsHost(host, "t.me") || IsHost(host, "telegram.me") || IsHost(host, "telegram.org"),
            QuickLinkKind.Instagram => IsHost(host, "instagram.com"),
            QuickLinkKind.Facebook => IsHost(host, "facebook.com") || IsHost(host, "fb.com"),
            QuickLinkKind.Video => IsHost(host, "youtube.com") || IsHost(host, "youtu.be") || IsHost(host, "vimeo.com"),
            _ => false
        };
    }

    private static bool IsHost(string host, string domain)
        => host == domain || host.EndsWith($".{domain}", StringComparison.Ordinal);
}
