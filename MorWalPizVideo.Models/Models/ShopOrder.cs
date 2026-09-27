using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;

namespace MorWalPizVideo.Server.Models;

public enum ShopOrderStatus
{
    PendingPayment,
    PaymentConfirmed,
    Cancelled,
    Fulfilled,
    AccessRevoked
}

public enum ShopPaymentStatus
{
    Pending,
    Confirmed
}

[BsonIgnoreExtraElements]
[DataContract]
public record ShopOrder : BaseEntity
{
    public ShopOrder(
        string customerId,
        string idempotencyKey,
        IReadOnlyList<ShopOrderItem> items,
        decimal totalAmount)
    {
        CustomerId = customerId;
        IdempotencyKey = idempotencyKey;
        Items = items.ToList();
        TotalAmount = totalAmount;
        Status = ShopOrderStatus.PendingPayment;
        PaymentStatus = ShopPaymentStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
    }

    [BsonElement("customerId")] public string CustomerId { get; init; }
    [BsonElement("idempotencyKey")] public string IdempotencyKey { get; init; }
    [BsonElement("items")] public List<ShopOrderItem> Items { get; init; }
    [BsonElement("totalAmount")] public decimal TotalAmount { get; init; }
    [BsonElement("status")] public ShopOrderStatus Status { get; init; }
    [BsonElement("paymentStatus")] public ShopPaymentStatus PaymentStatus { get; init; }
    [BsonElement("updatedAt")] public DateTime UpdatedAt { get; init; }
    [BsonElement("paymentConfirmedAt")] public DateTime? PaymentConfirmedAt { get; init; }
    [BsonElement("cancelledAt")] public DateTime? CancelledAt { get; init; }
}

[BsonIgnoreExtraElements]
[DataContract]
public record ShopOrderItem
{
    public ShopOrderItem(string productId, string productName, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    [BsonElement("productId")] public string ProductId { get; init; }
    [BsonElement("productName")] public string ProductName { get; init; }
    [BsonElement("quantity")] public int Quantity { get; init; }
    [BsonElement("unitPrice")] public decimal UnitPrice { get; init; }
    public decimal LineTotal => Quantity * UnitPrice;
}

[BsonIgnoreExtraElements]
[DataContract]
public record ShopCustomerSession : BaseEntity
{
    public ShopCustomerSession(string customerId, string tokenHash, DateTime expiresAt)
    {
        CustomerId = customerId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    [BsonElement("customerId")] public string CustomerId { get; init; }
    [BsonElement("tokenHash")] public string TokenHash { get; init; }
    [BsonElement("expiresAt")] public DateTime ExpiresAt { get; init; }
    [BsonElement("revokedAt")] public DateTime? RevokedAt { get; init; }
}

public record CreateShopOrderRequest(string IdempotencyKey);
public record ShopOrderContract(
    string Id,
    IReadOnlyList<ShopOrderItemContract> Items,
    decimal TotalAmount,
    ShopOrderStatus Status,
    ShopPaymentStatus PaymentStatus,
    DateTime CreationDateTime);
public record ShopOrderItemContract(string ProductId, string ProductName, int Quantity, decimal UnitPrice);