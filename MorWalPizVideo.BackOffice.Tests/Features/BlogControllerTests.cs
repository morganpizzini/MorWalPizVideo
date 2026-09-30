using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class BlogControllerTests : IClassFixture<PageControllerWebApplicationFactory>
{
    private readonly PageControllerWebApplicationFactory factory;
    public BlogControllerTests(PageControllerWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Publication_evicts_the_actual_public_cache_tag_and_cache_failure_keeps_committed_success()
    {
        factory.CrossApiService.Clear();
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PagesManage);
        var created = await client.PostAsJsonAsync("/api/blogposts", new SaveBlogPostRequest
        { Slug = $"cache-{Guid.NewGuid():N}", Draft = new BlogSnapshot { Title = "Cache" } });
        var post = (await created.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        Assert.Empty(factory.CrossApiService.PurgedTags);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/blogposts/{post.Id}/publish", new BlogRevisionRequest(1))).StatusCode);
        Assert.Contains(CacheKeys.Blog, factory.CrossApiService.ResetKeys);
        Assert.Contains(ApiTagCacheKeys.Blog, factory.CrossApiService.PurgedTags);
        foreach (var name in new[] { "List", "Get" })
        {
            var cache = (OutputCacheAttribute)Attribute.GetCustomAttribute(typeof(MorWalPizVideo.ServerAPI.Controllers.BlogController).GetMethod(name)!, typeof(OutputCacheAttribute))!;
            Assert.Contains(ApiTagCacheKeys.Blog, cache.Tags!);
            Assert.Equal(300, cache.Duration);
        }
        factory.CrossApiService.ShouldFail = true;
        try
        {
            var response = await client.PostAsJsonAsync($"/api/blogposts/{post.Id}/unpublish", new BlogRevisionRequest(2));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.Contains("X-Blog-Cache-Warning"));
            Assert.Null(await factory.Services.GetRequiredService<IBlogRepository>().GetPublishedAsync(PrimaryScenario.ChannelId, post.Slug));
        }
        finally { factory.CrossApiService.Clear(); }
    }

    [Fact]
    public async Task Owned_image_references_survive_draft_changes_without_exposing_draft_media()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PagesManage);
        var created = await client.PostAsJsonAsync("/api/blogposts", new SaveBlogPostRequest
        { Slug = $"media-{Guid.NewGuid():N}", Draft = new BlogSnapshot { Title = "Media" } });
        var post = (await created.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        using var image = new Image<Rgba32>(16, 16);
        await using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent(stream.ToArray()), "file", "photo.png");
        upload.Add(new StringContent("1"), "revision");
        upload.Add(new StringContent("Photo"), "altText");
        var uploaded = await client.PostAsync($"/api/blogposts/{post.Id}/images", upload);
        Assert.True(uploaded.StatusCode == HttpStatusCode.OK, await uploaded.Content.ReadAsStringAsync());
        post = (await uploaded.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        var media = Assert.Single(post.Images);
        Assert.StartsWith($"blog/{PrimaryScenario.ChannelId}/{post.Id}/", media.Id);
        var draft = post.Draft with { Document = new BlogDocument { Blocks =
            [new BlogBlock { Id = "photo", Type = "image", ImageIds = [media.Id] }] } };
        var saved = await client.PutAsJsonAsync($"/api/blogposts/{post.Id}", new SaveBlogPostRequest
        { Slug = post.Slug, Revision = 2, Draft = draft });
        Assert.True(saved.StatusCode == HttpStatusCode.OK, await saved.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/blogposts/{post.Id}/publish", new BlogRevisionRequest(3))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/blogposts/{post.Id}", new SaveBlogPostRequest
        { Slug = post.Slug, Revision = 4, Draft = new BlogSnapshot { Title = "Private", CoverAlt = "Private alt" } })).StatusCode);
        var publicPost = PublicBlogPost.From((await factory.Services.GetRequiredService<IBlogRepository>().GetPublishedAsync(PrimaryScenario.ChannelId, post.Slug))!);
        Assert.Equal("Media", publicPost.Metadata.Title);
        Assert.Equal(media.Id, Assert.Single(publicPost.Images).Id);
        Assert.Contains(media.PublicUrl, await factory.BlobService.GetImagesInFolderAsync($"blog/{PrimaryScenario.ChannelId}/{post.Id}"));
    }

    [Fact]
    public async Task Title_derived_slug_delete_and_invalid_image_are_http_contracts()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.PagesManage);
        client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        var request = new SaveBlogPostRequest { Draft = new BlogSnapshot { Title = $"Title {Guid.NewGuid():N}" } };
        var created = await client.PostAsJsonAsync("/api/blogposts", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var post = (await created.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        Assert.StartsWith("title-", post.Slug);
        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent([1, 2, 3]), "file", "invalid.png");
        upload.Add(new StringContent("1"), "revision");
        upload.Add(new StringContent("Alt text"), "altText");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/blogposts/{post.Id}/images", upload)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/api/blogposts/{post.Id}?revision=2")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/blogposts/{post.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/blogposts/{post.Id}?revision=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/blogposts/{post.Id}")).StatusCode);
    }

    [Fact]
    public async Task Malicious_and_null_documents_are_bad_requests()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.PagesManage);
        client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        var payloads = new[]
        {
            "{\"draft\":{\"title\":\"Null\",\"document\":null}}",
            "{\"draft\":{\"title\":\"Script\",\"document\":{\"blocks\":[{\"id\":\"script\",\"richText\":{\"type\":\"script\"}}]}}}",
            "{\"draft\":{\"title\":\"Media\",\"document\":{\"blocks\":[{\"id\":\"image\",\"type\":\"image\",\"imageIds\":[\"another-post/image.jpg\"]}]}}}"
        };
        foreach (var payload in payloads)
        {
            using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/blogposts", content)).StatusCode);
        }
    }

    [Fact]
    public async Task Permissions_ownership_conflicts_and_publication_are_enforced()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.PagesManage);
        client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        var request = new SaveBlogPostRequest { Slug = $"blog-{Guid.NewGuid():N}", Draft = new BlogSnapshot { Title = "Original" } };
        var response = await client.PostAsJsonAsync("/api/blogposts", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var post = (await response.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        Assert.False(post.IsPublished);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/blogposts", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/blogposts/{post.Id}/publish", new BlogRevisionRequest(1))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/blogposts/{post.Id}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/blogposts/{post.Id}", request with { Revision = 2, Draft = request.Draft with { Title = "New draft" } })).StatusCode);
        Assert.Equal("Original", (await factory.Services.GetRequiredService<IBlogRepository>().GetPublishedAsync(PrimaryScenario.ChannelId, post.Slug))!.Published!.Title);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/blogposts/{post.Id}/unpublish", new BlogRevisionRequest(3))).StatusCode);
        using var denied = factory.CreateClient();
        denied.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.BackofficeAccess);
        denied.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/api/blogposts")).StatusCode);
        var other = $"channel-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(other, "Other"));
        using var otherClient = factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add("X-Test-Permissions", AuthorizationPermissionKeys.BackofficeManageAll);
        otherClient.DefaultRequestHeaders.Add("X-Channel-Id", other);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/blogposts/{post.Id}")).StatusCode);
    }
}

public sealed class PublicBlogControllerTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;
    public PublicBlogControllerTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("")]
    [InlineData("unresolved-channel")]
    public async Task Missing_or_unresolved_configured_channel_fails_closed(string configuredChannel)
    {
        using var isolated = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["YouTubeChannelId"] = configuredChannel })));
        var service = isolated.Services.GetRequiredService<BlogService>();
        var post = await service.CreateAsync(PrimaryScenario.ChannelId, "isolated", new BlogSnapshot { Title = "Never fallback" });
        await service.PublishAsync(PrimaryScenario.ChannelId, post.Id, 1, true);
        using var client = isolated.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/blog?channelId=" + PrimaryScenario.ChannelId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/blog/isolated")).StatusCode);
    }

    [Fact]
    public async Task Public_queries_resolve_configured_channel_and_never_return_drafts_or_other_channels()
    {
        var service = new BlogService(factory.Services.GetRequiredService<IBlogRepository>());
        var slug = $"public-blog-{Guid.NewGuid():N}";
        var post = await service.CreateAsync(PrimaryScenario.ChannelId, slug, new BlogSnapshot { Title = "Public" });
        var other = await service.CreateAsync("other", slug, new BlogSnapshot { Title = "Secret" });
        await service.PublishAsync("other", other.Id, 1, true);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/blog/{slug}?channelId=other")).StatusCode);
        await service.PublishAsync(PrimaryScenario.ChannelId, post.Id, 1, true);
        var response = await client.GetAsync($"/api/blog/{slug}?channelId=other");
        var body = await response.Content.ReadFromJsonAsync<PublicBlogPost>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Public", body!.Metadata.Title);
        Assert.DoesNotContain("draft", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/blog?page=0")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/blog?page=1&pageSize=1")).StatusCode);
        await service.PublishAsync(PrimaryScenario.ChannelId, post.Id, 2, false);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/blog/{slug}")).StatusCode);
    }
}