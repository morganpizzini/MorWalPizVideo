using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class PageNavigationServiceTests
{
    [Fact]
    public async Task Published_page_lookup_normalizes_url_and_excludes_drafts()
    {
        var pages = new InMemoryPageRepository
        {
            Items =
            [
                new Page("", "Draft", "", "about", "") { Id = "draft", Status = PageStatus.Draft },
                new Page("", "Published", "", "about", "") { Id = "published", Status = PageStatus.Published }
            ]
        };
        var service = CreatePageService(pages, new InMemoryNavigationRepository());

        var result = await service.GetPublishedByUrlAsync("/ABOUT/");

        Assert.Equal("published", result?.Id);
    }

    [Fact]
    public async Task Delete_removes_navigation_references_and_page_blobs()
    {
        var blob = new BlobServiceMock();
        var page = new Page("", "About", "<p><img src=\"mock://blob/pages/about.jpg\"></p>", "about", "")
        {
            Id = "page-1",
            ChannelId = "channel-1",
            InlineImages = [new PageImage { StorageKey = "pages/about.jpg", PublicUrl = "mock://blob/pages/about.jpg" }]
        };
        await blob.UploadImageAsync("pages/about.jpg", new MemoryStream("image"u8.ToArray()), "pages");
        var pages = new InMemoryPageRepository { Items = [page] };
        var navigation = new InMemoryNavigationRepository
        {
            Items =
            [new ChannelNavigation
            {
                Id = "navigation-1",
                ChannelId = "channel-1",
                HeaderItems = [new NavigationMenuItem { Type = NavigationItemType.Page, PageId = "page-1", DisplayText = "About" }],
                FooterItems = [new NavigationMenuItem { Type = NavigationItemType.Page, PageId = "page-1", DisplayText = "About", Column = 0 }]
            }]
        };
        var service = new PageService(pages, navigation, blob, Options.Create(new BlobStorageOptions { PageContainerName = "pages" }));

        Assert.True(await service.DeleteAsync("page-1", "channel-1"));
        Assert.Empty(pages.Items);
        Assert.Empty(navigation.Items[0].HeaderItems);
        Assert.Empty(navigation.Items[0].FooterItems);
        Assert.False((await blob.DownloadImageAsync("pages/about.jpg")) is not null);
    }

    [Fact]
    public async Task Navigation_public_projection_contains_published_pages_only()
    {
        var pages = new InMemoryPageRepository
        {
            Items =
            [new Page("", "Published", "", "published", "") { Id = "published", ChannelId = "channel-1", Status = PageStatus.Published },
             new Page("", "Draft", "", "draft", "") { Id = "draft", ChannelId = "channel-1", Status = PageStatus.Draft }]
        };
        var navigation = new InMemoryNavigationRepository
        {
            Items =
            [new ChannelNavigation
            {
                ChannelId = "channel-1",
                IsActive = true,
                HeaderItems =
                [new NavigationMenuItem { Type = NavigationItemType.Page, PageId = "published", DisplayText = "Published" },
                 new NavigationMenuItem { Type = NavigationItemType.Page, PageId = "draft", DisplayText = "Draft" },
                 new NavigationMenuItem { Type = NavigationItemType.External, TargetUrl = "https://example.test", DisplayText = "External" }]
            }]
        };
        var service = new ChannelNavigationService(navigation, pages);

        var result = await service.GetPublicAsync();

        Assert.Equal(["Published", "External"], result?.HeaderItems.Select(item => item.DisplayText));
        Assert.Equal("/pages/published", result?.HeaderItems[0].TargetUrl);
        Assert.True(result?.HeaderItems[1].OpenInNewTab);
    }

    [Fact]
    public void Sanitizer_keeps_page_columns_and_allowed_images_but_removes_xss()
    {
        var html = "<div class=\"page-columns\"><div class=\"page-column\"><p>Safe</p><img src=\"https://cdn.example.test/page.jpg\" onerror=\"alert(1)\"></div></div><script>alert(1)</script><a href=\"javascript:alert(1)\">bad</a>";

        var result = ChannelNewsHtmlSanitizer.Sanitize(html, new HashSet<string> { "https://cdn.example.test/page.jpg" });

        Assert.Contains("page-columns", result);
        Assert.Contains("page-column", result);
        Assert.Contains("https://cdn.example.test/page.jpg", result);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Image_processing_caps_long_side_at_1920_without_upscaling()
    {
        using var large = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(3000, 1500);
        using var largeStream = new MemoryStream();
        await large.SaveAsPngAsync(largeStream, new PngEncoder());
        largeStream.Position = 0;
        var resized = await ChannelNewsMediaProcessor.PrepareImageAsync(largeStream);

        using var small = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(800, 400);
        using var smallStream = new MemoryStream();
        await small.SaveAsPngAsync(smallStream, new PngEncoder());
        smallStream.Position = 0;
        var unchanged = await ChannelNewsMediaProcessor.PrepareImageAsync(smallStream);

        Assert.Equal((1920, 960), (resized.Width, resized.Height));
        Assert.Equal((800, 400), (unchanged.Width, unchanged.Height));
    }

    private static PageService CreatePageService(InMemoryPageRepository pages, InMemoryNavigationRepository navigation) =>
        new(pages, navigation, new BlobServiceMock(), Options.Create(new BlobStorageOptions { PageContainerName = "pages" }));

    private sealed class InMemoryPageRepository : IPageRepository
    {
        public List<Page> Items { get; set; } = [];
        public Task<Page> AddItemAsync(Page item) { Items.Add(item); return Task.FromResult(item); }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
        public Task<Page> GetItemAsync(string id) => Task.FromResult(Items.First(item => item.Id == id));
        public Task<IList<Page>> GetItemsAsync() => Task.FromResult<IList<Page>>(Items.ToList());
        public Task<IList<Page>> GetItemsAsync(System.Linq.Expressions.Expression<Func<Page, bool>> predicate) => Task.FromResult<IList<Page>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<Page?> GetByUrlAsync(string url, string? channelId = null) => Task.FromResult(Items.FirstOrDefault(item => item.Url == url && (channelId is null || item.ChannelId == channelId)));
        public Task UpdateItemAsync(Page item) { Items[Items.FindIndex(existing => existing.Id == item.Id)] = item; return Task.CompletedTask; }
    }

    private sealed class InMemoryNavigationRepository : IChannelNavigationRepository
    {
        public List<ChannelNavigation> Items { get; set; } = [];
        public Task<ChannelNavigation> AddItemAsync(ChannelNavigation item) { Items.Add(item); return Task.FromResult(item); }
        public Task DeleteItemAsync(string id) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
        public Task<ChannelNavigation> GetItemAsync(string id) => Task.FromResult(Items.First(item => item.Id == id));
        public Task<IList<ChannelNavigation>> GetItemsAsync() => Task.FromResult<IList<ChannelNavigation>>(Items.ToList());
        public Task<IList<ChannelNavigation>> GetItemsAsync(System.Linq.Expressions.Expression<Func<ChannelNavigation, bool>> predicate) => Task.FromResult<IList<ChannelNavigation>>(Items.AsQueryable().Where(predicate).ToList());
        public Task<ChannelNavigation?> GetByChannelIdAsync(string channelId) => Task.FromResult(Items.FirstOrDefault(item => item.ChannelId == channelId));
        public Task UpdateItemAsync(ChannelNavigation item) { Items[Items.FindIndex(existing => existing.Id == item.Id)] = item; return Task.CompletedTask; }
    }
}