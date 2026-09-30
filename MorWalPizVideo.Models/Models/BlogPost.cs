using MongoDB.Bson.Serialization.Attributes;

namespace MorWalPizVideo.Server.Models;

public sealed record BlogMark
{
    public string Type { get; init; } = string.Empty;
    public string? Href { get; init; }
}

public sealed record BlogText
{
    public string Type { get; init; } = "doc";
    public string? Text { get; init; }
    public IReadOnlyList<BlogMark> Marks { get; init; } = [];
    public IReadOnlyList<BlogText> Content { get; init; } = [];
}

public sealed record BlogBlock
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = "richText";
    public string Text { get; init; } = string.Empty;
    public int Level { get; init; } = 2;
    public BlogText RichText { get; init; } = new();
    public IReadOnlyList<string> ImageIds { get; init; } = [];
    public string VideoId { get; init; } = string.Empty;
    public IReadOnlyList<IReadOnlyList<BlogBlock>> Columns { get; init; } = [];
}

public sealed record BlogDocument
{
    public int Version { get; init; } = 1;
    public IReadOnlyList<BlogBlock> Blocks { get; init; } = [];
}

public sealed record BlogSnapshot
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? CoverImageId { get; init; }
    public string CoverAlt { get; init; } = string.Empty;
    public string SeoTitle { get; init; } = string.Empty;
    public string SeoDescription { get; init; } = string.Empty;
    public BlogDocument Document { get; init; } = new();
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public sealed record BlogPost : BaseEntity
{
    public string ChannelId { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public long Revision { get; init; } = 1;
    public BlogSnapshot Draft { get; init; } = new();
    public BlogSnapshot? Published { get; init; }
    public DateTime? FirstPublishedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
    public IReadOnlyList<PageImage> Images { get; init; } = [];
}