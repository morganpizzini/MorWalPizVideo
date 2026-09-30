using System.Text.Json;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public interface IBlogRepository
{
    Task<BlogPost?> GetAsync(string channelId, string id);
    Task<IReadOnlyList<BlogPost>> ListAsync(string channelId, bool publishedOnly, int skip, int take);
    Task<BlogPost?> GetPublishedAsync(string channelId, string slug);
    Task CreateAsync(BlogPost post);
    Task<bool> ReplaceAsync(BlogPost post, long expectedRevision);
    Task<bool> DeleteAsync(string channelId, string id, long expectedRevision);
}

public sealed class BlogConflictException(string message) : Exception(message);
public sealed class BlogValidationException(string message) : Exception(message);

public sealed class BlogService(IBlogRepository repository)
{
    public Task<BlogPost?> GetAsync(string channelId, string id) => repository.GetAsync(channelId, id);
    public Task<IReadOnlyList<BlogPost>> ListAsync(string channelId, bool publishedOnly, int page, int pageSize, int extra = 0) =>
        repository.ListAsync(channelId, publishedOnly, (page - 1) * pageSize, pageSize + extra);
    public Task<BlogPost?> GetPublishedAsync(string channelId, string slug) => repository.GetPublishedAsync(channelId, NormalizeSlug(slug));

    public async Task<BlogPost> CreateAsync(string channelId, string slug, BlogSnapshot draft)
    {
        var post = new BlogPost { Id = ObjectId.GenerateNewId().ToString(), ChannelId = channelId,
            Slug = string.IsNullOrWhiteSpace(slug) ? SlugFromTitle(draft?.Title ?? string.Empty) : NormalizeSlug(slug),
            Draft = draft is null ? null! : draft with { UpdatedAt = DateTime.UtcNow }, CreationDateTime = DateTime.UtcNow };
        Validate(post);
        try { await repository.CreateAsync(post); }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { throw new BlogConflictException("Slug already exists in this channel."); }
        return post;
    }

    public async Task<BlogPost?> SaveAsync(string channelId, string id, long revision, string slug, BlogSnapshot draft)
    {
        var existing = await GetAsync(channelId, id);
        if (existing is null) return null;
        if (draft is null) throw new BlogValidationException("Draft is required.");
        var normalizedSlug = string.IsNullOrWhiteSpace(slug) ? existing.Slug : NormalizeSlug(slug);
        if (existing.FirstPublishedAt is not null && normalizedSlug != existing.Slug)
            throw new BlogConflictException("A previously published slug cannot change.");
        return await ReplaceAsync(existing with { Slug = normalizedSlug, Draft = draft with { UpdatedAt = DateTime.UtcNow } }, revision);
    }

    public async Task<BlogPost?> PublishAsync(string channelId, string id, long revision, bool publish)
    {
        var existing = await GetAsync(channelId, id);
        if (existing is null) return null;
        var now = DateTime.UtcNow;
        return await ReplaceAsync(existing with
        {
            Published = publish ? existing.Draft : null,
            FirstPublishedAt = publish ? existing.FirstPublishedAt ?? now : existing.FirstPublishedAt,
            PublishedAt = publish ? now : null
        }, revision);
    }

    public async Task<BlogPost?> AddImageAsync(string channelId, string id, long revision, PageImage image)
    {
        var existing = await GetAsync(channelId, id);
        if (existing is null) return null;
        if (existing.Images.Count >= 100) throw new BlogValidationException("At most 100 images per post.");
        return await ReplaceAsync(existing with { Images = existing.Images.Append(image).ToArray() }, revision);
    }

    private async Task<BlogPost> ReplaceAsync(BlogPost post, long revision)
    {
        ValidateRevision(revision);
        if (post.Revision != revision) throw new BlogConflictException("Post changed. Reload before saving.");
        Validate(post);
        var updated = post with { Revision = revision + 1 };
        try
        {
            if (!await repository.ReplaceAsync(updated, revision)) throw new BlogConflictException("Post changed. Reload before saving.");
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { throw new BlogConflictException("Slug already exists in this channel."); }
        return updated;
    }

    public async Task<BlogPost?> DeleteAsync(string channelId, string id, long revision)
    {
        ValidateRevision(revision);
        var existing = await GetAsync(channelId, id);
        if (existing is null) return null;
        if (existing.Revision != revision) throw new BlogConflictException("Post changed. Reload before deleting.");
        if (!await repository.DeleteAsync(channelId, id, revision))
            throw new BlogConflictException("Post changed. Reload before deleting.");
        return existing;
    }

    private static void ValidateRevision(long revision)
    {
        if (revision is < 1 or long.MaxValue) throw new BlogValidationException("Invalid expected revision.");
    }

    private static string SlugFromTitle(string title)
    {
        var decomposed = title.Normalize(NormalizationForm.FormD);
        var ascii = new string(decomposed.Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
        var slug = Regex.Replace(ascii.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return slug[..Math.Min(slug.Length, 120)].TrimEnd('-');
    }

    public static string NormalizeSlug(string slug) => (slug ?? string.Empty).Trim().Trim('/').ToLowerInvariant();

    public static void Validate(BlogPost post)
    {
        if (string.IsNullOrWhiteSpace(post.ChannelId) || post.Slug is null || !Regex.IsMatch(post.Slug, "^[a-z0-9](?:[a-z0-9-]{0,118}[a-z0-9])?$"))
            throw new BlogValidationException("Channel and ASCII slug (1-120 characters) are required.");
        ValidateSnapshot(post.Draft, post.Images);
        if (post.Published is not null) ValidateSnapshot(post.Published, post.Images);
    }

    private static void ValidateSnapshot(BlogSnapshot snapshot, IReadOnlyList<PageImage> images)
    {
        if (snapshot is null || snapshot.Summary is null || snapshot.Author is null || snapshot.SeoTitle is null ||
            snapshot.SeoDescription is null || snapshot.CoverAlt is null || snapshot.Tags is null ||
            snapshot.Document is null || snapshot.Document.Blocks is null ||
            string.IsNullOrWhiteSpace(snapshot.Title) || snapshot.Title.Length > 200 || snapshot.Summary.Length > 1000 ||
            snapshot.Author.Length > 120 || snapshot.SeoTitle.Length > 200 || snapshot.SeoDescription.Length > 320 ||
            snapshot.CoverAlt.Length > 300 || snapshot.Tags.Count > 20 || snapshot.Tags.Any(tag => tag is null || tag.Length > 60) ||
            snapshot.Document.Version != 1)
            throw new BlogValidationException("Invalid metadata or document size/version.");
        var owned = images.Select(image => image.StorageKey).ToHashSet(StringComparer.Ordinal);
        if (snapshot.CoverImageId is not null && (!owned.Contains(snapshot.CoverImageId) || string.IsNullOrWhiteSpace(snapshot.CoverAlt)))
            throw new BlogValidationException("Cover must reference an owned image with alt text.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        ValidateBlocks(snapshot.Document.Blocks, owned, ids, false);
        if (JsonSerializer.SerializeToUtf8Bytes(snapshot).Length > 256_000)
            throw new BlogValidationException("Document exceeds 256 KB.");
    }

    private static void ValidateBlocks(IReadOnlyList<BlogBlock> blocks, HashSet<string> owned, HashSet<string> ids, bool inColumns)
    {
        if (blocks is null) throw new BlogValidationException("Blocks are required.");
        foreach (var block in blocks)
        {
            if (block is null || block.Id is null || block.Text is null || block.ImageIds is null || block.Columns is null ||
                !Regex.IsMatch(block.Id, "^[a-zA-Z0-9_-]{1,80}$") || !ids.Add(block.Id) || ids.Count > 100 || block.Text.Length > 2000)
                throw new BlogValidationException("Invalid, duplicate or excessive block IDs.");
            ValidateText(block.RichText, 0);
            if (block.Type is not ("image" or "gallery" or "carousel") && block.ImageIds.Count != 0 ||
                block.Type != "video" && !string.IsNullOrEmpty(block.VideoId))
                throw new BlogValidationException("Unexpected media fields.");
            switch (block.Type)
            {
                case "richText": break;
                case "heading":
                    if (block.Level is < 2 or > 4 || string.IsNullOrWhiteSpace(block.Text)) throw new BlogValidationException("Invalid heading.");
                    break;
                case "image":
                case "gallery":
                case "carousel":
                    if (block.ImageIds.Count < 1 || block.ImageIds.Count > (block.Type == "image" ? 1 : 20) || block.ImageIds.Any(image => !owned.Contains(image)))
                        throw new BlogValidationException("Images must belong to this post.");
                    break;
                case "video":
                    if (block.VideoId is null || !Regex.IsMatch(block.VideoId, "^[a-zA-Z0-9_-]{11}$")) throw new BlogValidationException("Only YouTube video IDs are supported.");
                    break;
                case "columns":
                    if (inColumns || block.Columns.Count is < 1 or > 3) throw new BlogValidationException("Use 1-3 columns without nesting.");
                    foreach (var column in block.Columns) ValidateBlocks(column, owned, ids, true);
                    break;
                default: throw new BlogValidationException("Unknown block type.");
            }
            if (block.Type != "columns" && block.Columns.Count != 0) throw new BlogValidationException("Unexpected columns.");
        }
    }

    private static void ValidateText(BlogText node, int depth)
    {
        if (node is null || node.Content is null || node.Marks is null || depth > 12 || node.Content.Count > 200 || node.Text?.Length > 20_000 ||
            node.Type is not ("doc" or "paragraph" or "text" or "bulletList" or "orderedList" or "listItem" or "blockquote" or "hardBreak"))
            throw new BlogValidationException("Invalid rich text node.");
        if (node.Marks.Count > 6) throw new BlogValidationException("Too many text marks.");
        if (node.Type is "text" or "hardBreak" && node.Content.Count != 0 ||
            node.Type != "text" && (node.Text is not null || node.Marks.Count != 0))
            throw new BlogValidationException("Invalid rich text structure.");
        foreach (var mark in node.Marks)
        {
            if (mark is null || mark.Type is not ("bold" or "italic" or "strike" or "code" or "link") ||
                (mark.Type == "link" ? !IsSafeLink(mark.Href) : mark.Href is not null)) throw new BlogValidationException("Invalid rich text mark or URL.");
        }
        foreach (var child in node.Content) ValidateText(child, depth + 1);
    }

    private static bool IsSafeLink(string? value) => value is not null &&
        ((value.StartsWith('/') && !value.StartsWith("//") && !value.Contains('\\') && !value.Any(char.IsControl)) ||
         (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo)));
}

public sealed class BlogRepository(IMongoDatabase database) : BaseRepository<BlogPost>(database, DbCollections.BlogPosts), IBlogRepository
{
    private IMongoCollection<BlogPost> collection => _collection;
    private Task? indexTask;
    private Task EnsureIndexAsync() => indexTask ??= collection.Indexes.CreateOneAsync(new CreateIndexModel<BlogPost>(
        Builders<BlogPost>.IndexKeys.Ascending(post => post.ChannelId).Ascending(post => post.Slug),
        new CreateIndexOptions { Name = "blog_channel_slug_unique", Unique = true }));
    public Task<BlogPost?> GetAsync(string channelId, string id) => collection.Find(post => post.ChannelId == channelId && post.Id == id).FirstOrDefaultAsync()!;
    public Task<BlogPost?> GetPublishedAsync(string channelId, string slug) => collection.Find(post => post.ChannelId == channelId && post.Slug == slug && post.Published != null)
        .Project<BlogPost>(Builders<BlogPost>.Projection.Exclude(post => post.Draft)).FirstOrDefaultAsync()!;
    public async Task<IReadOnlyList<BlogPost>> ListAsync(string channelId, bool publishedOnly, int skip, int take)
    {
        var query = collection.Find(post => post.ChannelId == channelId && (!publishedOnly || post.Published != null))
            .SortByDescending(post => post.PublishedAt).ThenBy(post => post.Id).Skip(skip).Limit(take);
        return publishedOnly
            ? await query.Project<BlogPost>(Builders<BlogPost>.Projection.Exclude(post => post.Draft)).ToListAsync()
            : await query.ToListAsync();
    }
    public async Task CreateAsync(BlogPost post) { await EnsureIndexAsync(); await collection.InsertOneAsync(post); }
    public async Task<bool> ReplaceAsync(BlogPost post, long expectedRevision)
    {
        await EnsureIndexAsync();
        var result = await collection.ReplaceOneAsync(item => item.ChannelId == post.ChannelId && item.Id == post.Id && item.Revision == expectedRevision, post);
        return result.ModifiedCount == 1;
    }
    public async Task<bool> DeleteAsync(string channelId, string id, long expectedRevision) =>
        (await collection.DeleteOneAsync(post => post.ChannelId == channelId && post.Id == id && post.Revision == expectedRevision)).DeletedCount == 1;
}

public sealed class BlogMockRepository : IBlogRepository
{
    private readonly object gate = new();
    private readonly List<BlogPost> items = [];
    public Task<BlogPost?> GetAsync(string channelId, string id) { lock (gate) return Task.FromResult(items.FirstOrDefault(post => post.ChannelId == channelId && post.Id == id)); }
    public Task<BlogPost?> GetPublishedAsync(string channelId, string slug)
    {
        lock (gate)
        {
            var post = items.FirstOrDefault(post => post.ChannelId == channelId && post.Slug == slug && post.Published != null);
            return Task.FromResult(post is null ? null : post with { Draft = new() });
        }
    }
    public Task<IReadOnlyList<BlogPost>> ListAsync(string channelId, bool publishedOnly, int skip, int take)
    { lock (gate) return Task.FromResult<IReadOnlyList<BlogPost>>(items.Where(post => post.ChannelId == channelId && (!publishedOnly || post.Published != null)).OrderByDescending(post => post.PublishedAt).ThenBy(post => post.Id).Skip(skip).Take(take).Select(post => publishedOnly ? post with { Draft = new() } : post).ToArray()); }
    public Task CreateAsync(BlogPost post)
    {
        lock (gate) { EnsureUnique(post); items.Add(post); return Task.CompletedTask; }
    }
    public Task<bool> ReplaceAsync(BlogPost post, long expectedRevision)
    {
        lock (gate)
        {
            var index = items.FindIndex(item => item.Id == post.Id && item.ChannelId == post.ChannelId && item.Revision == expectedRevision);
            if (index < 0) return Task.FromResult(false);
            EnsureUnique(post); items[index] = post; return Task.FromResult(true);
        }
    }
    private void EnsureUnique(BlogPost post)
    {
        if (items.Any(item => item.ChannelId == post.ChannelId && item.Slug == post.Slug && item.Id != post.Id))
            throw new BlogConflictException("Slug already exists in this channel.");
    }
    public Task<bool> DeleteAsync(string channelId, string id, long expectedRevision)
    {
        lock (gate)
            return Task.FromResult(items.RemoveAll(post => post.ChannelId == channelId && post.Id == id && post.Revision == expectedRevision) == 1);
    }
}