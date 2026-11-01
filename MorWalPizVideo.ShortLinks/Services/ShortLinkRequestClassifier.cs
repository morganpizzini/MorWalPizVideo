using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.ShortLinks.Services;

public interface IShortLinkRequestClassifier
{
    ShortLinkRequestClassification Classify(string? userAgent);
}

public sealed class ShortLinkRequestClassifier : IShortLinkRequestClassifier
{
    public const string CurrentVersion = "1";

    public ShortLinkRequestClassification Classify(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return Create(ShortLinkRequestCategory.Unclassified, "missing-user-agent");

        if (Contains(userAgent, "facebookexternalhit"))
            return Create(ShortLinkRequestCategory.SocialPreview, "facebook-external-hit");

        if (ContainsAny(userAgent, "googlebot", "bingbot", "twitterbot", "linkedinbot", "slackbot", "discordbot", "telegrambot", "crawler", "spider", "scrapy", "headlesschrome"))
            return Create(ShortLinkRequestCategory.OtherAutomation, "known-automation-token");

        if (ContainsAny(userAgent, "mozilla/", "chrome/", "safari/", "firefox/", "edg/", "opr/", "android", "iphone", "ipad"))
            return Create(ShortLinkRequestCategory.HumanCandidate, "browser-token");

        return Create(ShortLinkRequestCategory.Unclassified, "unrecognized-user-agent");
    }

    private static ShortLinkRequestClassification Create(ShortLinkRequestCategory category, string ruleCode)
        => new(category, ruleCode, CurrentVersion);

    private static bool Contains(string value, string token)
        => value.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string value, params string[] tokens)
        => tokens.Any(token => Contains(value, token));
}