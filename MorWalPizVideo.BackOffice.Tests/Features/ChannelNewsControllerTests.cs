using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ChannelNewsControllerTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public ChannelNewsControllerTests(BackOfficeWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ChannelNews_requires_permission_and_keeps_records_in_the_selected_channel()
    {
        var otherChannelId = $"channel-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(otherChannelId, "Other channel"));
        using var createClient = CreateClient(AuthorizationPermissionKeys.ChannelNewsCreate, PrimaryScenario.ChannelId);
        using var viewClient = CreateClient(AuthorizationPermissionKeys.ChannelNewsManage, otherChannelId);
        using var deniedClient = CreateClient(AuthorizationPermissionKeys.BackofficeAccess, PrimaryScenario.ChannelId);

        var createResponse = await createClient.PostAsJsonAsync("/api/ChannelNews", new
        {
            title = "Private story",
            descriptionHtml = "<p>Body</p>",
            slug = $"private-{Guid.NewGuid():N}"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ChannelNewsContract>();

        var otherGetResponse = await viewClient.GetAsync($"/api/ChannelNews/{created!.Id}");
        var otherUpdateResponse = await viewClient.PutAsJsonAsync($"/api/ChannelNews/{created.Id}", new { title = "Cross-channel" });
        var deniedResponse = await deniedClient.GetAsync("/api/ChannelNews");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(PrimaryScenario.ChannelId, created.ChannelId);
        Assert.Equal("<p>Body</p>", created.DescriptionHtml);
        Assert.Equal(HttpStatusCode.NotFound, otherGetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherUpdateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
    }

    [Fact]
    public async Task ChannelNews_status_transitions_validate_scheduled_publication_time()
    {
        using var client = CreateClient(AuthorizationPermissionKeys.ChannelNewsManage, PrimaryScenario.ChannelId);
        var createResponse = await client.PostAsJsonAsync("/api/ChannelNews", new { title = "Status story" });
        var created = await createResponse.Content.ReadFromJsonAsync<ChannelNewsContract>();

        var missingTime = await client.PostAsJsonAsync($"/api/ChannelNews/{created!.Id}/status", new { status = ChannelNewsStatus.Scheduled });
        var scheduled = await client.PostAsJsonAsync($"/api/ChannelNews/{created.Id}/status", new
        {
            status = ChannelNewsStatus.Scheduled,
            publicationTimeUtc = DateTime.UtcNow.AddMinutes(5)
        });
        var archived = await client.PostAsJsonAsync($"/api/ChannelNews/{created.Id}/status", new { status = ChannelNewsStatus.Archived });
        var persisted = await factory.ChannelNewsRepository!.GetItemAsync(created.Id);

        Assert.Equal(HttpStatusCode.BadRequest, missingTime.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scheduled.StatusCode);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        Assert.Equal(ChannelNewsStatus.Archived, persisted!.Status);
    }

    [Fact]
    public async Task ChannelNews_images_upload_and_delete_preserve_server_metadata_and_cache_invalidation()
    {
        var item = await factory.ChannelNewsRepository!.AddItemAsync(new ChannelNews
        {
            Id = $"image-news-{Guid.NewGuid():N}",
            ChannelId = PrimaryScenario.ChannelId,
            Title = "Image story"
        });
        using var client = CreateClient(AuthorizationPermissionKeys.ChannelNewsUpdate, PrimaryScenario.ChannelId);
        using var viewClient = CreateClient(AuthorizationPermissionKeys.ChannelNewsView, PrimaryScenario.ChannelId);
        var image = await CreateImageAsync(4000, 2000);
        using var multipart = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(image.ToArray());
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(imageContent, "files", "landscape.png");

        factory.CrossApiService.Clear();
        var uploadResponse = await client.PostAsync($"/api/ChannelNews/{item.Id}/images", multipart);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ChannelNewsContract>();
        var uploadedEntity = await factory.ChannelNewsRepository.GetItemAsync(item.Id);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        var uploadedImage = Assert.Single(uploaded!.Images);
        Assert.Equal(1920, uploadedImage.Width);
        Assert.Equal(960, uploadedImage.Height);
        Assert.Equal(1920, uploadedEntity!.Images[0].Width);

        var listResponse = await viewClient.GetAsync("/api/ChannelNews");
        var listed = await listResponse.Content.ReadFromJsonAsync<List<ChannelNewsContract>>();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(listed!, news => news.Id == item.Id && news.Images.Count == 1);

        Assert.Equal([CacheKeys.ChannelNews], factory.CrossApiService.ResetKeys);
        Assert.Equal([ApiTagCacheKeys.ChannelNews], factory.CrossApiService.PurgedTags);

        factory.CrossApiService.Clear();
        var deleteResponse = await client.DeleteAsync($"/api/ChannelNews/{item.Id}/images/0");
        var deletedEntity = await factory.ChannelNewsRepository.GetItemAsync(item.Id);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Empty(deletedEntity!.Images);
        Assert.Equal([CacheKeys.ChannelNews], factory.CrossApiService.ResetKeys);
        Assert.Equal([ApiTagCacheKeys.ChannelNews], factory.CrossApiService.PurgedTags);
    }

    private HttpClient CreateClient(string permission, string channelId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
        client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        return client;
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
