using MorWalPizVideo.Server.Models;
using MorWalPizVideo.ShortLinks.Services;

namespace MorWalPizVideo.ShortLinks.Tests.Features;

[Trait("Category", "TestGroup:ShortLinks")]
public sealed class ShortLinkRequestClassifierTests
{
    private readonly ShortLinkRequestClassifier classifier = new();

    [Theory]
    [InlineData("facebookexternalhit/1.1", ShortLinkRequestCategory.SocialPreview, "facebook-external-hit")]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1)", ShortLinkRequestCategory.OtherAutomation, "known-automation-token")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Version/17.0 Mobile/15E148 Safari/604.1", ShortLinkRequestCategory.HumanCandidate, "browser-token")]
    [InlineData("", ShortLinkRequestCategory.Unclassified, "missing-user-agent")]
    [InlineData("curl/8.0", ShortLinkRequestCategory.Unclassified, "unrecognized-user-agent")]
    public void Classify_returns_stable_category_and_rule(string userAgent, ShortLinkRequestCategory category, string ruleCode)
    {
        var result = classifier.Classify(userAgent);

        Assert.Equal(category, result.Category);
        Assert.Equal(ruleCode, result.RuleCode);
        Assert.Equal(ShortLinkRequestClassifier.CurrentVersion, result.Version);
    }

    [Fact]
    public void Automation_rule_precedes_browser_tokens()
    {
        var result = classifier.Classify("Mozilla/5.0 compatible; facebookexternalhit/1.1");

        Assert.Equal(ShortLinkRequestCategory.SocialPreview, result.Category);
    }
}