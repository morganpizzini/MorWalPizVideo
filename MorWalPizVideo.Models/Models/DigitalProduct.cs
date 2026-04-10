using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record DigitalProduct : BaseEntity
    {
        [JsonConstructor]
        public DigitalProduct(
            string name,
            string description,
            string previewImageUrl,
            string contentStorageKey,
            List<string> categoryIds,
            decimal? price,
            bool isActive)
        {
            Name = name;
            Description = description;
            PreviewImageUrl = previewImageUrl;
            ContentStorageKey = contentStorageKey;
            CategoryIds = categoryIds ?? new List<string>();
            Price = price;
            IsActive = isActive;
            UpdatedAt = DateTime.UtcNow;
        }

        [DataMember]
        [BsonElement("name")]
        public string Name { get; init; }

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        [DataMember]
        [BsonElement("previewImageUrl")]
        public string PreviewImageUrl { get; init; }

        [DataMember]
        [BsonElement("contentStorageKey")]
        public string ContentStorageKey { get; init; }

        [DataMember]
        [BsonElement("categoryIds")]
        public List<string> CategoryIds { get; init; } = new List<string>();

        [DataMember]
        [BsonElement("price")]
        public decimal? Price { get; init; }

        [DataMember]
        [BsonElement("isActive")]
        public bool IsActive { get; init; }

        [DataMember]
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; init; }
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record DigitalProductCategory : BaseEntity
    {
        [JsonConstructor]
        public DigitalProductCategory(string name, string description, int? displayOrder)
        {
            Name = name;
            Description = description;
            DisplayOrder = displayOrder;
        }

        [DataMember]
        [BsonElement("name")]
        public string Name { get; init; }

        [DataMember]
        [BsonElement("description")]
        public string Description { get; init; }

        [DataMember]
        [BsonElement("displayOrder")]
        public int? DisplayOrder { get; init; }
    }

    public record CreateDigitalProductRequest(
        string Name,
        string Description,
        string PreviewImageUrl,
        string ContentStorageKey,
        List<string> CategoryIds,
        decimal? Price,
        bool IsActive
    );

    public record UpdateDigitalProductRequest(
        string? Name,
        string? Description,
        string? PreviewImageUrl,
        string? ContentStorageKey,
        List<string>? CategoryIds,
        decimal? Price,
        bool? IsActive
    );
}