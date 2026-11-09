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

[Trait("Category", "TestGroup:BackOffice")]
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
    public async Task Save_and_publish_endpoints_publish_at_one_revision()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PagesManage);
        var create = await client.PostAsJsonAsync("/api/blogposts/save-and-publish", new SaveBlogPostRequest
        {
            Slug = $"atomic-{Guid.NewGuid():N}",
            Draft = new BlogSnapshot { Title = "Published once", Tags = ["first"] }
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        Assert.Equal(1, created.Revision);
        Assert.True(created.IsPublished);

        var save = await client.PutAsJsonAsync($"/api/blogposts/{created.Id}/save-and-publish",
            new SaveBlogPostRequest
            {
                Slug = created.Slug,
                Revision = created.Revision,
                Draft = created.Draft with { Title = "Updated once", Tags = ["second"] }
            });

        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var published = (await save.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        Assert.Equal(2, published.Revision);
        Assert.True(published.IsPublished);
        Assert.Equal("Updated once", published.Draft.Title);
        var stored = await factory.Services.GetRequiredService<IBlogRepository>()
            .GetPublishedAsync(PrimaryScenario.ChannelId, created.Slug);
        Assert.Equal(["second"], stored!.Published!.Tags);
    }

    [Fact]
    public async Task Image_deletion_persists_retry_state_then_removes_exact_owned_blob()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PagesManage);
        var (post, image) = await CreatePostWithImageAsync(client);
        var folder = $"blog/{PrimaryScenario.ChannelId}/{post.Id}";
        factory.BlobService.Outcome = BlobMockOutcome.TransientFailure;

        try
        {
            var failure = await client.DeleteAsync(
                $"/api/blogposts/{post.Id}/images?imageId={Uri.EscapeDataString(image.Id)}&revision={post.Revision}");
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
            var pending = (await failure.Content.ReadFromJsonAsync<BlogPostResponse>())!;
            Assert.Equal(post.Revision + 1, pending.Revision);
            Assert.Contains(image.Id, pending.PendingImageDeletionIds);

            factory.BlobService.Outcome = BlobMockOutcome.Success;
            Assert.Contains($"mock://blob/{image.Id}", await factory.BlobService.GetImagesInFolderAsync(folder));
            var retry = await client.DeleteAsync(
                $"/api/blogposts/{post.Id}/images?imageId={Uri.EscapeDataString(image.Id)}&revision={pending.Revision}");
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            var deleted = (await retry.Content.ReadFromJsonAsync<BlogPostResponse>())!;
            Assert.Equal(pending.Revision + 1, deleted.Revision);
            Assert.DoesNotContain(image.Id, deleted.PendingImageDeletionIds);
            Assert.DoesNotContain(deleted.Images, item => item.Id == image.Id);
            Assert.DoesNotContain($"mock://blob/{image.Id}", await factory.BlobService.GetImagesInFolderAsync(folder));
            Assert.Equal("page-media", factory.BlobService.LastContainerName);
        }
        finally
        {
            factory.BlobService.Outcome = BlobMockOutcome.Success;
        }
    }

    [Fact]
    public async Task Image_deletion_rejects_a_published_reference_without_touching_storage()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PagesManage);
        var (post, image) = await CreatePostWithImageAsync(client);
        var referenced = await client.PutAsJsonAsync($"/api/blogposts/{post.Id}/save-and-publish",
            new SaveBlogPostRequest
            {
                Slug = post.Slug,
                Revision = post.Revision,
                Draft = post.Draft with { CoverImageId = image.Id, CoverAlt = image.AltText }
            });
        Assert.Equal(HttpStatusCode.OK, referenced.StatusCode);
        var published = (await referenced.Content.ReadFromJsonAsync<BlogPostResponse>())!;

        var deletion = await client.DeleteAsync(
            $"/api/blogposts/{post.Id}/images?imageId={Uri.EscapeDataString(image.Id)}&revision={published.Revision}");

        Assert.Equal(HttpStatusCode.Conflict, deletion.StatusCode);
        Assert.Contains($"mock://blob/{image.Id}", await factory.BlobService.GetImagesInFolderAsync(
            $"blog/{PrimaryScenario.ChannelId}/{post.Id}"));
        var unchanged = await factory.Services.GetRequiredService<IBlogRepository>()
            .GetAsync(PrimaryScenario.ChannelId, post.Id);
        Assert.Empty(unchanged!.PendingImageDeletionIds);
        Assert.Contains(image.Id, unchanged.Published!.CoverImageId);
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
    public async Task Cover_upload_rejects_non_full_hd_before_storage_and_stores_full_hd_jpeg()
    {
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.PagesManage);
        var created = await client.PostAsJsonAsync("/api/blogposts", new SaveBlogPostRequest
        { Slug = $"cover-{Guid.NewGuid():N}", Draft = new BlogSnapshot { Title = "Cover" } });
        var post = (await created.Content.ReadFromJsonAsync<BlogPostResponse>())!;

        using (var smallImage = new Image<Rgba32>(16, 16))
        await using (var smallStream = new MemoryStream())
        {
            await smallImage.SaveAsPngAsync(smallStream);
            using var invalidUpload = new MultipartFormDataContent();
            invalidUpload.Add(new ByteArrayContent(smallStream.ToArray()), "file", "small.png");
            invalidUpload.Add(new StringContent("1"), "revision");
            invalidUpload.Add(new StringContent("Cover"), "altText");
            invalidUpload.Add(new StringContent("cover"), "purpose");
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync($"/api/blogposts/{post.Id}/images", invalidUpload)).StatusCode);

            using var unknownPurpose = new MultipartFormDataContent();
            unknownPurpose.Add(new ByteArrayContent(smallStream.ToArray()), "file", "small.png");
            unknownPurpose.Add(new StringContent("1"), "revision");
            unknownPurpose.Add(new StringContent("Cover"), "altText");
            unknownPurpose.Add(new StringContent("thumbnail"), "purpose");
            Assert.Equal(HttpStatusCode.BadRequest,
                (await client.PostAsync($"/api/blogposts/{post.Id}/images", unknownPurpose)).StatusCode);
        }

        Assert.Empty(await factory.BlobService.GetImagesInFolderAsync($"blog/{PrimaryScenario.ChannelId}/{post.Id}"));
        post = (await client.GetFromJsonAsync<BlogPostResponse>($"/api/blogposts/{post.Id}"))!;
        Assert.Equal(1, post.Revision);
        Assert.Empty(post.Images);

        using (var image = new Image<Rgba32>(2560, 1440))
        await using (var stream = new MemoryStream())
        {
            await image.SaveAsPngAsync(stream);
            using var upload = new MultipartFormDataContent();
            upload.Add(new ByteArrayContent(stream.ToArray()), "file", "large-cover.png");
            upload.Add(new StringContent("1"), "revision");
            upload.Add(new StringContent("Cover"), "altText");
            upload.Add(new StringContent("cover"), "purpose");
            var response = await client.PostAsync($"/api/blogposts/{post.Id}/images", upload);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            post = (await response.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        }

        var stored = Assert.Single(post.Images);
        Assert.Equal(1920, stored.Width);
        Assert.Equal(1080, stored.Height);
        await using var blob = await factory.BlobService.DownloadImageAsync(stored.Id);
        Assert.NotNull(blob);
        using var storedImage = await Image.LoadAsync(blob);
        Assert.Equal(1920, storedImage.Width);
        Assert.Equal(1080, storedImage.Height);
    }

    private async Task<(BlogPostResponse Post, BlogImageResponse Image)> CreatePostWithImageAsync(HttpClient client)
    {
        var created = await client.PostAsJsonAsync("/api/blogposts", new SaveBlogPostRequest
        {
            Slug = $"delete-{Guid.NewGuid():N}",
            Draft = new BlogSnapshot { Title = "Delete one image" }
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var post = (await created.Content.ReadFromJsonAsync<BlogPostResponse>())!;

        using var image = new Image<Rgba32>(16, 16);
        await using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        using var upload = new MultipartFormDataContent();
        upload.Add(new ByteArrayContent(stream.ToArray()), "file", "delete.png");
        upload.Add(new StringContent(post.Revision.ToString()), "revision");
        upload.Add(new StringContent("Delete image"), "altText");
        var response = await client.PostAsync($"/api/blogposts/{post.Id}/images", upload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        post = (await response.Content.ReadFromJsonAsync<BlogPostResponse>())!;
        return (post, Assert.Single(post.Images));
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