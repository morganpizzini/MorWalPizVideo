using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PageNavigationControllerTests : IClassFixture<PageControllerWebApplicationFactory>
{
    private readonly PageControllerWebApplicationFactory factory;

    public PageNavigationControllerTests(PageControllerWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Page_permissions_and_channel_scope_are_enforced()
    {
        var page = await AddPageAsync(PrimaryScenario.ChannelId, "scoped-page", PageStatus.Draft);
        var otherChannelId = await AddChannelAsync();
        using var deniedClient = CreateClient(AuthorizationPermissionKeys.BackofficeAccess, PrimaryScenario.ChannelId);
        using var otherChannelClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, otherChannelId);
        using var allowedClient = CreateClient(AuthorizationPermissionKeys.PagesView, PrimaryScenario.ChannelId);

        Assert.Equal(HttpStatusCode.Forbidden, (await deniedClient.GetAsync("/api/Pages")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherChannelClient.GetAsync($"/api/Pages/{page.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await allowedClient.GetAsync($"/api/Pages/{page.Id}")).StatusCode);
    }

    [Fact]
    public async Task Page_urls_are_global_and_duplicate_create_returns_conflict()
    {
        var otherChannelId = await AddChannelAsync();
        using var primaryClient = CreateClient(AuthorizationPermissionKeys.PagesCreate, PrimaryScenario.ChannelId);
        using var otherClient = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, otherChannelId);
        var payload = new { title = "Global page", url = "global-page", content = "<p>Body</p>", status = PageStatus.Published };

        Assert.Equal(HttpStatusCode.Created, (await primaryClient.PostAsJsonAsync("/api/Pages", payload)).StatusCode);
        var duplicate = await otherClient.PostAsJsonAsync("/api/Pages", payload);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Page_images_resize_and_delete_use_the_page_blob_lifecycle()
    {
        using var client = CreateClient(AuthorizationPermissionKeys.PagesManage, PrimaryScenario.ChannelId);
        var createResponse = await client.PostAsJsonAsync("/api/Pages", new
        {
            title = "Image page",
            url = $"image-page-{Guid.NewGuid():N}",
            content = "<p>Body</p>",
            status = PageStatus.Draft
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PageContract>();
        using var multipart = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(await CreatePngAsync(4000, 2000));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(imageContent, "files", "page.png");

        var uploadResponse = await client.PostAsync($"/api/Pages/{created!.Id}/images", multipart);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<List<PageImageContract>>();
        var page = await GetPages().GetItemAsync(created.Id);

        Assert.Equal(HttpStatusCode.OK, uploadResponse.StatusCode);
        Assert.Equal(1920, Assert.Single(uploaded!).Width);
        Assert.Equal(960, uploaded[0].Height);
        Assert.Equal("page-media", factory.BlobService.LastContainerName);
        Assert.NotNull(page);
        Assert.NotNull(await factory.BlobService.DownloadImageAsync(page!.InlineImages[0].StorageKey));

        var deleteImageResponse = await client.DeleteAsync($"/api/Pages/{created.Id}/images/0");

        Assert.Equal(HttpStatusCode.OK, deleteImageResponse.StatusCode);
        Assert.Empty((await GetPages().GetItemAsync(created.Id))!.InlineImages);
    }

    [Fact]
    public async Task Deleting_a_page_detaches_navigation_items_and_deletes_page_blobs()
    {
        using var client = CreateClient(AuthorizationPermissionKeys.PagesManage, PrimaryScenario.ChannelId);
        var createResponse = await client.PostAsJsonAsync("/api/Pages", new
        {
            title = "Delete page",
            url = $"delete-page-{Guid.NewGuid():N}",
            content = "<p>Body</p>",
            status = PageStatus.Published
        });
        var created = await createResponse.Content.ReadFromJsonAsync<PageContract>();
        await GetNavigations().AddItemAsync(new ChannelNavigation
        {
            Id = $"navigation-{Guid.NewGuid():N}",
            ChannelId = PrimaryScenario.ChannelId,
            HeaderItems = [new NavigationMenuItem { Type = NavigationItemType.Page, PageId = created!.Id, DisplayText = "Delete" }],
            FooterItems = [new NavigationMenuItem { Type = NavigationItemType.Page, PageId = created.Id, DisplayText = "Delete" }]
        });
        using var multipart = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(await CreatePngAsync(100, 50));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(imageContent, "files", "page.png");
        await client.PostAsync($"/api/Pages/{created.Id}/images", multipart);
        var storageKey = (await GetPages().GetItemAsync(created.Id))!.InlineImages[0].StorageKey;

        var deleteResponse = await client.DeleteAsync($"/api/Pages/{created.Id}");
        var persistedNavigation = (await GetNavigations().GetItemsAsync()).Single();

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.DoesNotContain(await GetPages().GetItemsAsync(), page => page.Id == created.Id);
        Assert.Empty(persistedNavigation.HeaderItems);
        Assert.Empty(persistedNavigation.FooterItems);
        Assert.Null(await factory.BlobService.DownloadImageAsync(storageKey));
    }

    [Fact]
    public async Task Navigation_is_channel_scoped_and_rejects_page_links_from_another_channel()
    {
        var otherChannelId = await AddChannelAsync();
        var page = await AddPageAsync(PrimaryScenario.ChannelId, "navigation-page", PageStatus.Published);
        using var client = CreateClient(AuthorizationPermissionKeys.BackofficeManageAll, otherChannelId);

        var response = await client.PutAsJsonAsync("/api/Navigation", new
        {
            isActive = true,
            footerColumnCount = 2,
            headerItems = new[] { new { type = NavigationItemType.Page, pageId = page.Id, targetUrl = "", displayText = "Wrong channel", column = 0 } },
            footerItems = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Navigation_normalizes_internal_external_items_and_preserves_order()
    {
        using var client = CreateClient(AuthorizationPermissionKeys.NavigationManage, PrimaryScenario.ChannelId);
        var response = await client.PutAsJsonAsync("/api/Navigation", new
        {
            isActive = true,
            footerColumnCount = 2,
            headerItems = new[]
            {
                new { type = NavigationItemType.Internal, pageId = (string?)null, targetUrl = "/first", displayText = "First", column = 0 },
                new { type = NavigationItemType.External, pageId = (string?)null, targetUrl = "https://example.test", displayText = "External", column = 0 }
            },
            footerItems = new[] { new { type = NavigationItemType.Internal, pageId = (string?)null, targetUrl = "/footer", displayText = "Footer", column = 1 } }
        });
        var saved = await response.Content.ReadFromJsonAsync<ChannelNavigationContract>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PrimaryScenario.ChannelId, saved!.ChannelId);
        Assert.Equal([0, 1], saved.HeaderItems.Select(item => item.DisplayOrder));
        Assert.False(saved.HeaderItems[0].OpenInNewTab);
        Assert.True(saved.HeaderItems[1].OpenInNewTab);
        Assert.Equal(1, saved.FooterItems[0].Column);
    }

    private PageMockRepository GetPages() => factory.Services.GetRequiredService<IPageRepository>() as PageMockRepository
        ?? throw new InvalidOperationException("Page mock repository is unavailable.");

    private ChannelNavigationMockRepository GetNavigations() => factory.Services.GetRequiredService<IChannelNavigationRepository>() as ChannelNavigationMockRepository
        ?? throw new InvalidOperationException("Navigation mock repository is unavailable.");

    private HttpClient CreateClient(string permission, string channelId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permission);
        client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        return client;
    }

    private async Task<string> AddChannelAsync()
    {
        var channelId = $"channel-{Guid.NewGuid():N}";
        await factory.YTChannelRepository!.AddItemAsync(new YTChannel(channelId, "Other channel"));
        return channelId;
    }

    private Task<Page> AddPageAsync(string channelId, string url, PageStatus status) =>
        GetPages().AddItemAsync(new Page("", "Test page", "<p>Body</p>", url, "")
        {
            Id = $"page-{Guid.NewGuid():N}",
            ChannelId = channelId,
            Status = status
        });

    private static async Task<byte[]> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        await using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        return stream.ToArray();
    }
}

public sealed class PageControllerWebApplicationFactory : BackOfficeWebApplicationFactory
{
    public BlobServiceMock BlobService => Services.GetRequiredService<BlobServiceMock>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobService>();
            services.AddSingleton<BlobServiceMock>();
            services.AddSingleton<IBlobService>(provider => provider.GetRequiredService<BlobServiceMock>());
            services.Configure<BlobStorageOptions>(options => options.PageContainerName = "page-media");
        });
    }
}