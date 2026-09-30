using System.ComponentModel.DataAnnotations;
using MorWalPizVideo.Server.Models;

namespace MorWalPiz.Contracts.Contracts;

public sealed record SaveBlogPostRequest
{
    [MaxLength(120)] public string Slug { get; init; } = string.Empty;
    [Range(1, long.MaxValue)] public long Revision { get; init; } = 1;
    [Required] public required BlogSnapshot Draft { get; init; }
}

public sealed record BlogRevisionRequest([Range(1, long.MaxValue)] long Revision);
public sealed record BlogImageResponse(string Id, string PublicUrl, int Width, int Height, string AltText);
public sealed record BlogPostResponse(string Id, string Slug, long Revision, BlogSnapshot Draft,
    bool IsPublished, DateTime? PublishedAt, IReadOnlyList<BlogImageResponse> Images)
{
    public DateTime? FirstPublishedAt { get; init; }

    public static BlogPostResponse From(BlogPost post) => new(post.Id, post.Slug, post.Revision, post.Draft,
        post.Published is not null, post.PublishedAt, post.Images.Select(image =>
            new BlogImageResponse(image.StorageKey, image.PublicUrl, image.Width, image.Height, image.AltText)).ToArray())
        { FirstPublishedAt = post.FirstPublishedAt };
}

public sealed record PublicBlogSummary(string Slug, string Title, string Summary, string Author,
    IReadOnlyList<string> Tags, string? CoverUrl, string CoverAlt, DateTime PublishedAt)
{
    public static PublicBlogSummary From(BlogPost post)
    {
        var snapshot = post.Published!;
        return new(post.Slug, snapshot.Title, snapshot.Summary, snapshot.Author, snapshot.Tags,
            post.Images.FirstOrDefault(image => image.StorageKey == snapshot.CoverImageId)?.PublicUrl,
            snapshot.CoverAlt, post.PublishedAt!.Value);
    }
}

public sealed record PublicBlogPost(PublicBlogSummary Metadata, BlogDocument Document,
    string SeoTitle, string SeoDescription, DateTime UpdatedAt, IReadOnlyList<BlogImageResponse> Images)
{
    public static PublicBlogPost From(BlogPost post)
    {
        var snapshot = post.Published!;
        var referenced = References(snapshot.Document.Blocks).Append(snapshot.CoverImageId).ToHashSet();
        return new(PublicBlogSummary.From(post), snapshot.Document, snapshot.SeoTitle, snapshot.SeoDescription,
            snapshot.UpdatedAt, post.Images.Where(image => referenced.Contains(image.StorageKey)).Select(image =>
                new BlogImageResponse(image.StorageKey, image.PublicUrl, image.Width, image.Height, image.AltText)).ToArray());
    }
    private static IEnumerable<string> References(IEnumerable<BlogBlock> blocks) => blocks.SelectMany(block =>
        block.ImageIds.Concat(block.Columns.SelectMany(column => References(column))));
}

public sealed record PublicBlogPage(IReadOnlyList<PublicBlogSummary> Items, int Page, int PageSize, bool HasMore);