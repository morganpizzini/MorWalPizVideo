using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Domain;

public sealed record PublicChannelSocialLink(string Provider, string Url);

public static class ChannelSocialLinks
{
    private static readonly string[] Order =
        ["telegram", "discord", "instagram", "youtube", "facebook", "x", "reddit", "patreon"];

    public static IReadOnlyList<PublicChannelSocialLink> Resolve(IEnumerable<ChannelSocial>? socials)
    {
        var result = new List<PublicChannelSocialLink>();
        foreach (var social in socials ?? [])
        {
            var provider = social.Provider?.Trim().ToLowerInvariant();
            var url = ResolveUrl(provider, social.Handler);
            if (url is not null)
                result.Add(new(provider!, url));
        }

        return result.OrderBy(item => Array.IndexOf(Order, item.Provider)).ToArray();
    }

    private static string? ResolveUrl(string? provider, string? handler)
    {
        if (provider is null || string.IsNullOrWhiteSpace(handler) || !Order.Contains(provider))
            return null;
        var value = handler.Trim();
        if (value.Any(char.IsControl)) return null;

        var hosts = provider switch
        {
            "telegram" => new[] { "t.me", "telegram.me" },
            "discord" => new[] { "discord.gg", "discord.com" },
            "instagram" => new[] { "instagram.com" },
            "youtube" => new[] { "youtube.com", "youtu.be" },
            "facebook" => new[] { "facebook.com", "fb.com" },
            "x" => new[] { "x.com", "twitter.com" },
            "reddit" => new[] { "reddit.com", "www.reddit.com" },
            "patreon" => new[] { "patreon.com" },
            _ => []
        };

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(absolute.UserInfo))
                return null;
            var host = absolute.Host.ToLowerInvariant();
            if (!hosts.Any(allowed => host == allowed || host.EndsWith("." + allowed, StringComparison.Ordinal)))
                return null;
            return absolute.ToString();
        }

        value = value.TrimStart('@', '/');
        if (value.Length == 0 || value.Any(char.IsWhiteSpace))
            return null;
        return provider switch
        {
            "telegram" => $"https://t.me/{value}",
            "discord" => $"https://discord.gg/{value}",
            "instagram" => $"https://www.instagram.com/{value}",
            "youtube" => $"https://www.youtube.com/@{value.TrimStart('@')}",
            "facebook" => $"https://www.facebook.com/{value}",
            "x" => $"https://x.com/{value}",
            "reddit" => $"https://www.reddit.com/user/{value}",
            "patreon" => $"https://www.patreon.com/{value}",
            _ => null
        };
    }
}
