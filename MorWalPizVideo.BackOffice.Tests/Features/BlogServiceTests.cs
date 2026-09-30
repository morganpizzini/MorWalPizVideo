using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class BlogServiceTests
{
    private static BlogSnapshot Draft(string title = "First") => new() { Title = title };

    [Fact]
    public void Versioned_document_columns_and_published_snapshot_roundtrip_through_bson()
    {
        var snapshot = Draft() with { Document = new BlogDocument { Blocks =
            [new BlogBlock { Id = "columns", Type = "columns", Columns =
                [[new BlogBlock { Id = "heading", Type = "heading", Text = "Title" }],
                 [new BlogBlock { Id = "text", RichText = new BlogText { Content =
                    [new BlogText { Type = "paragraph", Content = [new BlogText { Type = "text", Text = "Body" }] }] } }]] }] } };
        var post = new BlogPost { Id = ObjectId.GenerateNewId().ToString(), ChannelId = "one", Slug = "first", Draft = snapshot, Published = snapshot };
        var restored = BsonSerializer.Deserialize<BlogPost>(post.ToBson());
        Assert.Equal(post.Id, restored.Id);
        Assert.Equal(1, restored.Published!.Document.Version);
        Assert.Equal("Title", restored.Published.Document.Blocks[0].Columns[0][0].Text);
        Assert.Equal("Body", restored.Draft.Document.Blocks[0].Columns[1][0].RichText.Content[0].Content[0].Text);
        BlogService.Validate(restored);
    }

    [Fact]
    public async Task Future_revision_cannot_replace_an_older_snapshot_even_when_the_repository_version_advances()
    {
        var repository = new AdvancingRevisionRepository();
        var service = new BlogService(repository);
        await Assert.ThrowsAsync<BlogConflictException>(() => service.PublishAsync("one", "post", 2, true));
        await Assert.ThrowsAsync<BlogConflictException>(() => service.SaveAsync("one", "post", 2, "first", Draft("Overwrite")));
        await Assert.ThrowsAsync<BlogConflictException>(() => service.DeleteAsync("one", "post", 2));
        Assert.False(repository.Written);
    }

    private sealed class AdvancingRevisionRepository : IBlogRepository
    {
        public bool Written { get; private set; }
        public Task<BlogPost?> GetAsync(string channelId, string id) => Task.FromResult<BlogPost?>(new BlogPost
        { Id = id, ChannelId = channelId, Slug = "first", Revision = 1, Draft = Draft() });
        public Task<bool> ReplaceAsync(BlogPost post, long expectedRevision) { Written = true; return Task.FromResult(true); }
        public Task<bool> DeleteAsync(string channelId, string id, long expectedRevision) { Written = true; return Task.FromResult(true); }
        public Task CreateAsync(BlogPost post) => throw new NotSupportedException();
        public Task<BlogPost?> GetPublishedAsync(string channelId, string slug) => throw new NotSupportedException();
        public Task<IReadOnlyList<BlogPost>> ListAsync(string channelId, bool publishedOnly, int skip, int take) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Public_repository_queries_do_not_load_draft_metadata()
    {
        var service = new BlogService(new BlogMockRepository());
        var post = await service.CreateAsync("one", "public", Draft());
        await service.PublishAsync("one", post.Id, 1, true);
        await service.SaveAsync("one", post.Id, 2, "public", Draft("Private revision"));
        Assert.Equal(string.Empty, (await service.GetPublishedAsync("one", "public"))!.Draft.Title);
        Assert.Equal(string.Empty, Assert.Single(await service.ListAsync("one", true, 1, 10)).Draft.Title);
        Assert.Equal("Private revision", (await service.GetAsync("one", post.Id))!.Draft.Title);
    }

    [Fact]
    public void Inactive_fields_and_deep_documents_are_rejected_before_serialization()
    {
        var deepText = new BlogText();
        for (var depth = 0; depth < 70; depth++) deepText = new BlogText { Content = [deepText] };
        var invalidBlocks = new[]
        {
            new BlogBlock { Id = "heading", Type = "heading", Text = "Title", RichText = new BlogText { Type = "script" } },
            new BlogBlock { Id = "heading", Type = "heading", Text = "Title", ImageIds = ["other/post.jpg"] },
            new BlogBlock { Id = "text", RichText = deepText }
        };
        foreach (var block in invalidBlocks)
            Assert.Throws<BlogValidationException>(() => BlogService.Validate(new BlogPost { ChannelId = "one", Slug = "first", Draft = Draft() with
            { Document = new BlogDocument { Blocks = [block] } } }));
    }

    [Fact]
    public async Task Slug_is_derived_once_and_delete_requires_current_channel_revision()
    {
        var service = new BlogService(new BlogMockRepository());
        var post = await service.CreateAsync("one", "", Draft("My first post!"));
        Assert.Equal("my-first-post", post.Slug);
        post = (await service.SaveAsync("one", post.Id, 1, "", Draft("Changed title")))!;
        Assert.Equal("my-first-post", post.Slug);
        Assert.Null(await service.DeleteAsync("two", post.Id, post.Revision));
        await Assert.ThrowsAsync<BlogConflictException>(() => service.DeleteAsync("one", post.Id, 1));
        await Assert.ThrowsAsync<BlogValidationException>(() => service.PublishAsync("one", post.Id, long.MaxValue, true));
        Assert.NotNull(await service.DeleteAsync("one", post.Id, post.Revision));
        Assert.Null(await service.GetAsync("one", post.Id));
    }

    [Fact]
    public void Explicit_json_nulls_are_validation_errors()
    {
        var invalidDrafts = new[]
        {
            Draft() with { Summary = null! },
            Draft() with { Tags = [null!] },
            Draft() with { Document = null! },
            Draft() with { Document = new BlogDocument { Blocks = [null!] } },
            Draft() with { Document = new BlogDocument { Blocks = [new BlogBlock { Id = "text", RichText = null! }] } },
            Draft() with { Document = new BlogDocument { Blocks = [new BlogBlock { Id = "text", RichText = new BlogText { Marks = [null!] } }] } }
        };
        foreach (var draft in invalidDrafts)
            Assert.Throws<BlogValidationException>(() => BlogService.Validate(new BlogPost { ChannelId = "one", Slug = "first", Draft = draft }));
    }

    [Fact]
    public async Task Channel_isolation_and_slug_uniqueness()
    {
        var service = new BlogService(new BlogMockRepository());
        var post = await service.CreateAsync("one", " /SAME/ ", Draft());
        await service.CreateAsync("two", "same", Draft());
        await Assert.ThrowsAsync<BlogConflictException>(() => service.CreateAsync("one", "same", Draft()));
        Assert.Null(await service.GetAsync("two", post.Id));
        Assert.Null(await service.GetPublishedAsync("one", "same"));
        Assert.Null(await service.PublishAsync("two", post.Id, 1, true));
    }

    [Fact]
    public async Task Draft_edits_do_not_change_published_snapshot_and_slug_stays_frozen()
    {
        var service = new BlogService(new BlogMockRepository());
        var post = await service.CreateAsync("one", "first", Draft());
        Assert.Null(BlogPostResponse.From(post).FirstPublishedAt);
        post = (await service.PublishAsync("one", post.Id, 1, true))!;
        var firstPublishedAt = post.FirstPublishedAt;
        Assert.NotNull(firstPublishedAt);
        Assert.Equal(firstPublishedAt, BlogPostResponse.From(post).FirstPublishedAt);
        post = (await service.SaveAsync("one", post.Id, post.Revision, "first", Draft("Second")))!;
        Assert.Equal("First", (await service.GetPublishedAsync("one", "first"))!.Published!.Title);
        await Assert.ThrowsAsync<BlogConflictException>(() => service.SaveAsync("one", post.Id, post.Revision, "changed", Draft()));
        post = (await service.PublishAsync("one", post.Id, post.Revision, false))!;
        Assert.Null(await service.GetPublishedAsync("one", "first"));
        var reopened = BlogPostResponse.From((await service.GetAsync("one", post.Id))!);
        Assert.False(reopened.IsPublished);
        Assert.Null(reopened.PublishedAt);
        Assert.Equal(firstPublishedAt, reopened.FirstPublishedAt);
        await Assert.ThrowsAsync<BlogConflictException>(() => service.SaveAsync("one", post.Id, post.Revision, "changed", Draft()));
    }

    [Fact]
    public async Task Only_one_concurrent_writer_can_publish()
    {
        var service = new BlogService(new BlogMockRepository());
        var post = await service.CreateAsync("one", "first", Draft());
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            try { await service.PublishAsync("one", post.Id, 1, true); return true; }
            catch (BlogConflictException) { return false; }
        }));
        Assert.Single(results, success => success);
        await Assert.ThrowsAsync<BlogConflictException>(() => service.SaveAsync("one", post.Id, 1, "first", Draft()));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.test")]
    [InlineData("https://user:secret@evil.test")]
    public void Unsafe_links_are_rejected(string href)
    {
        var text = new BlogText { Type = "text", Text = "Link", Marks = [new BlogMark { Type = "link", Href = href }] };
        Assert.Throws<BlogValidationException>(() => BlogService.Validate(new BlogPost { ChannelId = "one", Slug = "first", Draft = Draft() with
        { Document = new BlogDocument { Blocks = [new BlogBlock { Id = "text", RichText = text }] } } }));
    }

    [Fact]
    public void Unowned_media_duplicate_ids_nested_columns_and_unknown_nodes_are_rejected()
    {
        var badBlocks = new[]
        {
            new BlogBlock { Id = "image", Type = "image", ImageIds = ["another-post/image"] },
            new BlogBlock { Id = "video", Type = "video", VideoId = "https://evil.test" },
            new BlogBlock { Id = "text", RichText = new BlogText { Type = "script" } },
            new BlogBlock { Id = "columns", Type = "columns", Columns = [[new BlogBlock { Id = "nested", Type = "columns", Columns = [[], []] }], []] }
        };
        foreach (var block in badBlocks)
            Assert.Throws<BlogValidationException>(() => BlogService.Validate(new BlogPost { ChannelId = "one", Slug = "first", Draft = Draft() with { Document = new BlogDocument { Blocks = [block] } } }));
    }
}