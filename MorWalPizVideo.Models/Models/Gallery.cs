using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

[BsonIgnoreExtraElements]
[DataContract]
public sealed record GalleryImage
{
    [DataMember][BsonElement("storageKey")] public string StorageKey { get; init; } = string.Empty;
    [DataMember][BsonElement("publicUrl")] public string PublicUrl { get; init; } = string.Empty;
    [DataMember][BsonElement("previewStorageKey")] public string? PreviewStorageKey { get; init; }
    [DataMember][BsonElement("previewPublicUrl")] public string? PreviewPublicUrl { get; init; }
    [DataMember][BsonElement("contentType")] public string ContentType { get; init; } = string.Empty;
    [DataMember][BsonElement("width")] public int Width { get; init; }
    [DataMember][BsonElement("height")] public int Height { get; init; }
    [DataMember][BsonElement("displayOrder")] public int DisplayOrder { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public sealed record Gallery : BaseEntity
{
    [DataMember][BsonElement("name")] public string Name { get; init; } = string.Empty;
    [DataMember][BsonElement("description")] public string? Description { get; init; }
    [DataMember][BsonElement("downloadCount")] public int DownloadCount { get; init; }
    [DataMember][BsonElement("slug")] public string Slug { get; init; } = string.Empty;
    [DataMember][BsonElement("expiration")] public DateTime Expiration { get; init; }
    [DataMember][BsonElement("blobReference")] public IReadOnlyList<GalleryImage> BlobReference { get; init; } = [];
    [DataMember][BsonElement("channelId")] public string ChannelId { get; init; } = string.Empty;

    public static string NormalizeSlug(string? slug) => (slug ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
    public static bool IsAllowedExpirationDays(int days) => days is 3 or 7 or 21;
}