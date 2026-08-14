using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ChannelNewsBehaviorTests
{
    [Fact]
    public void Sanitizer_removes_active_content_and_unsafe_links()
    {
        var sanitized = ChannelNewsHtmlSanitizer.Sanitize(
            "<p>Hello <strong>world</strong></p><script>alert(1)</script><a href=\"javascript:alert(1)\" onclick=\"bad()\">link</a>");

        sanitized.Should().Contain("<p>Hello <strong>world</strong></p>");
        sanitized.ToLowerInvariant().Should().NotContain("script");
        sanitized.ToLowerInvariant().Should().NotContain("onclick");
        sanitized.ToLowerInvariant().Should().NotContain("javascript:");
    }

    [Fact]
    public async Task News_images_resize_without_upscaling_or_distortion()
    {
        await using var landscape = await CreateImageAsync(4000, 2000, png: true);
        var preparedLandscape = await ChannelNewsMediaProcessor.PrepareImageAsync(landscape);
        preparedLandscape.Width.Should().Be(1920);
        preparedLandscape.Height.Should().Be(960);

        await using var portrait = await CreateImageAsync(2000, 4000, png: true);
        var preparedPortrait = await ChannelNewsMediaProcessor.PrepareImageAsync(portrait);
        preparedPortrait.Width.Should().Be(960);
        preparedPortrait.Height.Should().Be(1920);

        await using var small = await CreateImageAsync(500, 200, png: true);
        var preparedSmall = await ChannelNewsMediaProcessor.PrepareImageAsync(small);
        preparedSmall.Width.Should().Be(500);
        preparedSmall.Height.Should().Be(200);
    }

    [Fact]
    public async Task Logo_requires_png_and_resizes_width_to_500_without_upscaling()
    {
        await using var jpg = await CreateImageAsync(600, 300, png: false);
        var invalid = async () => await ChannelNewsMediaProcessor.PrepareLogoAsync(jpg);
        await invalid.Should().ThrowAsync<InvalidDataException>();

        await using var png = await CreateImageAsync(1000, 600, png: true);
        var prepared = await ChannelNewsMediaProcessor.PrepareLogoAsync(png);
        prepared.ContentType.Should().Be("image/png");
        prepared.Width.Should().Be(500);
        prepared.Height.Should().Be(300);
    }

    [Fact]
    public async Task Public_news_filters_channel_and_publication_state()
    {
        var repository = new InMemoryChannelNewsRepository();
        var service = new ChannelNewsService(repository);
        var now = DateTime.UtcNow;

        await repository.AddItemAsync(new ChannelNews { Id = "published", ChannelId = "shit", Status = ChannelNewsStatus.Published });
        await repository.AddItemAsync(new ChannelNews { Id = "scheduled-past", ChannelId = "shit", Status = ChannelNewsStatus.Scheduled, PublicationTimeUtc = now.AddMinutes(-1) });
        await repository.AddItemAsync(new ChannelNews { Id = "scheduled-future", ChannelId = "shit", Status = ChannelNewsStatus.Scheduled, PublicationTimeUtc = now.AddMinutes(1) });
        await repository.AddItemAsync(new ChannelNews { Id = "draft", ChannelId = "shit", Status = ChannelNewsStatus.Draft });
        await repository.AddItemAsync(new ChannelNews { Id = "archived", ChannelId = "shit", Status = ChannelNewsStatus.Archived });
        await repository.AddItemAsync(new ChannelNews { Id = "other-channel", ChannelId = "regular", Status = ChannelNewsStatus.Published });

        var result = await service.GetPublicAsync(["shit"], now);

        result.Select(item => item.Id).Should().BeEquivalentTo(["published", "scheduled-past"]);
    }

    [Fact]
    public async Task Service_rejects_more_than_ten_images()
    {
        var service = new ChannelNewsService(new InMemoryChannelNewsRepository());
        var item = new ChannelNews
        {
            Id = "too-many",
            ChannelId = "shit",
            Title = "Too many",
            Images = Enumerable.Range(0, 11).Select(index => new ChannelNewsImage { StorageKey = $"image-{index}" }).ToArray()
        };

        var act = async () => await service.CreateAsync(item);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static async Task<MemoryStream> CreateImageAsync(int width, int height, bool png)
    {
        var stream = new MemoryStream();
        using var image = new Image<Rgba32>(width, height);
        if (png)
            await image.SaveAsPngAsync(stream);
        else
            await image.SaveAsJpegAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class InMemoryChannelNewsRepository : IChannelNewsRepository
    {
        private readonly List<ChannelNews> items = [];

        public Task<ChannelNews> GetItemAsync(string id) =>
            Task.FromResult(items.First(item => item.Id == id));

        public Task<IList<ChannelNews>> GetItemsAsync() =>
            Task.FromResult<IList<ChannelNews>>(items.ToList());

        public Task<IList<ChannelNews>> GetItemsAsync(System.Linq.Expressions.Expression<Func<ChannelNews, bool>> predicate) =>
            Task.FromResult<IList<ChannelNews>>(items.AsQueryable().Where(predicate).ToList());

        public Task<ChannelNews> AddItemAsync(ChannelNews item)
        {
            items.Add(item);
            return Task.FromResult(item);
        }

        public Task UpdateItemAsync(ChannelNews item)
        {
            var index = items.FindIndex(existing => existing.Id == item.Id);
            items[index] = item;
            return Task.CompletedTask;
        }

        public Task DeleteItemAsync(string id)
        {
            items.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }
}
