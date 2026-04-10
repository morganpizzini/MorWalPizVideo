using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MorWalPizVideo.Server.Models
{
    [BsonIgnoreExtraElements]
    [DataContract]
    public record Cart : BaseEntity
    {
        [JsonConstructor]
        public Cart(
            string customerId,
            List<CartItem> items,
            bool isCompleted,
            DateTime? completedAt)
        {
            CustomerId = customerId;
            Items = items ?? new List<CartItem>();
            IsCompleted = isCompleted;
            CompletedAt = completedAt;
            UpdatedAt = DateTime.UtcNow;
        }

        [DataMember]
        [BsonElement("customerId")]
        public string CustomerId { get; init; }

        [DataMember]
        [BsonElement("items")]
        public List<CartItem> Items { get; init; } = new List<CartItem>();

        [DataMember]
        [BsonElement("isCompleted")]
        public bool IsCompleted { get; init; }

        [DataMember]
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; init; }

        [DataMember]
        [BsonElement("completedAt")]
        public DateTime? CompletedAt { get; init; }
    }

    [BsonIgnoreExtraElements]
    [DataContract]
    public record CartItem
    {
        [JsonConstructor]
        public CartItem(
            string productId,
            string productName,
            int quantity,
            decimal? price)
        {
            ProductId = productId;
            ProductName = productName;
            Quantity = quantity;
            Price = price;
        }

        [DataMember]
        [BsonElement("productId")]
        public string ProductId { get; init; }

        [DataMember]
        [BsonElement("productName")]
        public string ProductName { get; init; }

        [DataMember]
        [BsonElement("quantity")]
        public int Quantity { get; init; }

        [DataMember]
        [BsonElement("price")]
        public decimal? Price { get; init; }
    }

    public record AddToCartRequest(
        string ProductId,
        int Quantity
    );

    public record UpdateCartItemRequest(
        string ProductId,
        int Quantity
    );

    public record CheckoutRequest(
        string PaymentMethod,
        string? PaymentIntentId
    );

    public record CheckoutResponse(
        string OrderId,
        string Status,
        decimal TotalAmount,
        string? PaymentIntentId,
        string? PaymentClientSecret
    );
}