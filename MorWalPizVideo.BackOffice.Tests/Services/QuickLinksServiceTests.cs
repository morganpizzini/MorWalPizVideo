using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class QuickLinksServiceTests
{
    [Theory]
    [InlineData(QuickLinkKind.External, "https://example.com/", true)]
    [InlineData(QuickLinkKind.Telegram, "https://t.me/channel", true)]
    [InlineData(QuickLinkKind.Instagram, "https://instagram.com/channel", true)]
    [InlineData(QuickLinkKind.Facebook, "https://facebook.com/channel", true)]
    [InlineData(QuickLinkKind.Video, "https://vimeo.com/123", true)]
    [InlineData(QuickLinkKind.Video, "https://youtube.com/watch?v=abc", true)]
    [InlineData(QuickLinkKind.YouTube, "https://youtube.com/watch?v=abc", true)]
    [InlineData(QuickLinkKind.YouTube, "https://studio.youtube.com/channel", true)]
    [InlineData(QuickLinkKind.YouTube, "https://youtu.be/abc", true)]
    [InlineData(QuickLinkKind.YouTube, "https://vimeo.com/123", false)]
    [InlineData(QuickLinkKind.YouTube, "https://youtube.com.evil.example/watch?v=abc", false)]
    [InlineData((QuickLinkKind)6, "https://youtube.com/watch?v=abc", false)]
    public void Validate_enforces_kind_specific_hosts_without_changing_existing_kinds(
        QuickLinkKind kind,
        string targetUrl,
        bool expectedValid)
    {
        var entity = new QuickLinks("Links", null, "links", [new QuickLink(kind, targetUrl)])
        {
            ChannelId = "channel"
        };

        Assert.Equal(expectedValid, QuickLinksService.Validate(entity).Count == 0);
    }

    [Fact]
    public void YouTube_kind_is_additive_and_existing_values_remain_stable()
    {
        Assert.Equal(0, (int)QuickLinkKind.External);
        Assert.Equal(1, (int)QuickLinkKind.Telegram);
        Assert.Equal(2, (int)QuickLinkKind.Instagram);
        Assert.Equal(3, (int)QuickLinkKind.Facebook);
        Assert.Equal(4, (int)QuickLinkKind.Video);
        Assert.Equal(5, (int)QuickLinkKind.YouTube);
    }

    [Fact]
    public void YouTube_kind_round_trips_as_its_numeric_bson_value()
    {
        var link = new QuickLink(QuickLinkKind.YouTube, "https://youtu.be/video");
        var document = link.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<QuickLink>(document);

        Assert.Equal(5, document["kind"].AsInt32);
        Assert.Equal(QuickLinkKind.YouTube, restored.Kind);
    }
}
