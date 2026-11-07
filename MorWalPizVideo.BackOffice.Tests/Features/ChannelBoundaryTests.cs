using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class ChannelBoundaryTests
{
    [Fact]
    public void Terminology_normalizer_treats_missing_sections_as_empty_but_rejects_null_mapping()
    {
        var empty = ChannelTerminologyNormalizer.Normalize(null);
        Assert.True(empty.IsValid);
        Assert.Empty(empty.Configuration.ItalianToEnglish!);

        var corrupt = ChannelTerminologyNormalizer.Normalize(new ChannelTerminologyConfiguration
        {
            ItalianToEnglish = new List<TerminologyMapping> { null! }
        });
        Assert.False(corrupt.IsValid);
    }

    [Fact]
    public void Social_links_only_project_safe_configured_profiles()
    {
        var links = ChannelSocialLinks.Resolve([
            new() { Provider = "facebook", Handler = "profile.php?id=42" },
            new() { Provider = "telegram", Handler = "morwalpiz" },
            new() { Provider = "discord", Handler = "https://discord.evil.example/invite/x" },
            new() { Provider = "instagram", Handler = "https://instagram.com/morwalpiz" }
        ]);

        Assert.Equal(["telegram", "instagram", "facebook"], links.Select(link => link.Provider));
        Assert.DoesNotContain(links, link => link.Url.Contains("evil", StringComparison.Ordinal));
    }
}
