using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPiz.Contracts;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:Shared")]
public sealed class VideoReferenceClassificationTests
{
    [Theory]
    [InlineData(0, YoutubeContentType.SingleVideo, YoutubeContentType.Collection)]
    [InlineData(1, YoutubeContentType.Collection, YoutubeContentType.SingleVideo)]
    [InlineData(2, YoutubeContentType.SingleVideo, YoutubeContentType.Collection)]
    [InlineData(4, YoutubeContentType.Collection, YoutubeContentType.Collection)]
    public void Classification_and_wire_projections_use_references_not_legacy_type(
        int count, YoutubeContentType legacyType, YoutubeContentType expected)
    {
        var match = Create(count, legacyType);
        Assert.Equal(expected, match.EffectiveContentType);
        Assert.Equal(count == 1, match.IsLink);
        Assert.Equal(expected, ContractUtils.Convert(match).ContentType);
        Assert.Equal(expected, ContractUtils.ConvertPublic(match).ContentType);
        Assert.Equal(count == 1, ContractUtils.ConvertPublic(match).IsLink);

        var json = JsonSerializer.SerializeToElement(ContractUtils.ConvertPublic(match),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal((int)expected, json.GetProperty("contentType").GetInt32());
        Assert.False(json.TryGetProperty("effectiveContentType", out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Bson_preserves_archival_discriminator_without_persisting_computed_fields(int count)
    {
        var match = Create(count, YoutubeContentType.SingleVideo);
        var document = match.ToBsonDocument();
        Assert.False(document.Contains("EffectiveContentType"));
        Assert.False(document.Contains("IsLink"));
        var restored = BsonSerializer.Deserialize<YouTubeContent>(document);
        Assert.Equal(match.ContentType, restored.ContentType);
        Assert.Equal(match.EffectiveContentType, restored.EffectiveContentType);
        Assert.Equal(document["contentType"], restored.ToBsonDocument()["contentType"]);

        document.Remove("contentType");
        var legacy = BsonSerializer.Deserialize<YouTubeContent>(document);
        Assert.Equal(match.EffectiveContentType, legacy.EffectiveContentType);
        var json = JsonSerializer.SerializeToElement(restored, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.False(json.TryGetProperty("effectiveContentType", out _));
    }

    [Fact]
    public void Empty_and_null_references_do_not_create_a_playable_video()
    {
        var match = Create(0, YoutubeContentType.SingleVideo) with { VideoRefs = null! };
        Assert.Equal(YoutubeContentType.Collection, match.EffectiveContentType);
        Assert.False(match.IsLink);
        Assert.Empty(match.ToDisplayItem().VideoIds);
    }

    [Fact]
    public void Attachment_transitions_and_display_conversion_use_actual_video_identity()
    {
        var empty = Create(0, YoutubeContentType.SingleVideo);
        var single = empty.AddVideo("attached-video", []);
        var collection = single.AddVideo("second-video", []);
        Assert.Equal(YoutubeContentType.Collection, empty.EffectiveContentType);
        Assert.Equal(YoutubeContentType.SingleVideo, single.EffectiveContentType);
        Assert.Equal(YoutubeContentType.Collection, collection.EffectiveContentType);
        Assert.Equal(YoutubeContentType.SingleVideo, collection.RemoveVideo("second-video").EffectiveContentType);
        Assert.Equal("attached-video", single.ToDisplayItem().PrimaryVideoId);
        Assert.Equal("unattached-thumbnail", single.ToDisplayItem().ThumbnailUrl);
        Assert.Equal(["attached-video", "second-video"], collection.ToDisplayItem().VideoIds);
        Assert.Single(YouTubeContent.FromDisplayItem(single.ToDisplayItem()).VideoRefs);
        Assert.Equal(2, YouTubeContent.FromDisplayItem(collection.ToDisplayItem()).VideoRefs.Length);
        Assert.Empty(YouTubeContent.FromDisplayItem(empty.ToDisplayItem()).VideoRefs);
    }

    private static YouTubeContent Create(int count, YoutubeContentType legacyType) =>
        new("editorial-content", "Title", "Description", "content-url", "unattached-thumbnail",
            Enumerable.Range(0, count).Select(index => new VideoRef($"video-{index}", [])).ToArray(),
            [], legacyType) { Id = "507f1f77bcf86cd799439011" };
}
