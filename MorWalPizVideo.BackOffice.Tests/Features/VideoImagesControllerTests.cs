using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPiz.Contracts;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class VideoImagesControllerTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public VideoImagesControllerTests(BackOfficeWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Video_images_upload_and_delete_persist_safe_ordered_metadata_and_invalidate_match_cache()
    {
        var match = await factory.MatchRepository!.AddItemAsync(YouTubeContent.CreateSingleVideo(
            $"image-video-{Guid.NewGuid():N}", []) with
        {
            Id = $"image-match-{Guid.NewGuid():N}",
            Url = $"image-match-{Guid.NewGuid():N}",
            CreatorUserId = "test-user-id",
            OwnerChannelId = PrimaryScenario.ChannelId,
            VideoRefs = [new VideoRef("image-video", [], channelIds: [PrimaryScenario.ChannelId])]
        });
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.VideosUpdate);
        var firstImage = await CreateImageAsync(4000, 2000);
        var secondImage = await CreateImageAsync(800, 600);
        using var multipart = new MultipartFormDataContent();
        AddImage(multipart, firstImage, "first.png");
        AddImage(multipart, secondImage, "second.png");

        factory.CrossApiService.Clear();
        var uploadResponse = await client.PostAsync($"/api/Videos/{match.Id}/images", multipart);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<YouTubeContentContract>();
        var persisted = await factory.MatchRepository.GetItemAsync(match.Id);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        Assert.NotNull(uploaded);
        Assert.Equal(2, uploaded!.Images.Count);
        Assert.Equal("first", uploaded.Images[0].AltText);
        Assert.Equal(1920, uploaded.Images[0].Width);
        Assert.Equal(960, uploaded.Images[0].Height);
        Assert.Equal("second", uploaded.Images[1].AltText);
        Assert.DoesNotContain("storageKey", await uploadResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, persisted!.Images.Count);
        Assert.False(string.IsNullOrWhiteSpace(persisted.Images[0].StorageKey));
        Assert.Contains(CacheKeys.Matches, factory.CrossApiService.ResetKeys);
        Assert.Equal([CacheKeys.Matches], factory.CrossApiService.PurgedTags);

        factory.CrossApiService.Clear();
        var deleteResponse = await client.DeleteAsync($"/api/Videos/{match.Id}/images/0");
        var afterDelete = await factory.MatchRepository.GetItemAsync(match.Id);

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Single(afterDelete!.Images);
        Assert.Equal("second", afterDelete.Images[0].AltText);
        Assert.Contains(CacheKeys.Matches, factory.CrossApiService.ResetKeys);
        Assert.Equal([CacheKeys.Matches], factory.CrossApiService.PurgedTags);
    }

    [Fact]
    public async Task Video_images_require_manageable_match_channel_scope()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.VideosUpdate);
        using var multipart = new MultipartFormDataContent();
        AddImage(multipart, await CreateImageAsync(100, 100), "image.png");

        var response = await client.PostAsync("/api/Videos/does-not-exist/images", multipart);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Public_video_contract_exposes_safe_image_metadata_only()
    {
        var entity = YouTubeContent.CreateSingleVideo("public-image-video", []) with
        {
            Images = [new YouTubeContentImage
            {
                StorageKey = "private/storage-key.jpg",
                PublicUrl = "https://cdn.example/image.jpg",
                ContentType = "image/jpeg",
                Width = 1920,
                Height = 1080,
                AltText = "Public image"
            }]
        };

        var contract = ContractUtils.ConvertPublic(entity);
        var json = JsonSerializer.Serialize(contract);

        Assert.Single(contract.Images);
        Assert.Equal("https://cdn.example/image.jpg", contract.Images[0].PublicUrl);
        Assert.DoesNotContain("storageKey", json, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddImage(MultipartFormDataContent multipart, MemoryStream image, string fileName)
    {
        var content = new StreamContent(image);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(content, "files", fileName);
    }

    private static async Task<MemoryStream> CreateImageAsync(int width, int height)
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(width, height);
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }
}