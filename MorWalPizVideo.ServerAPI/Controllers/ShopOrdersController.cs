using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.ServerAPI.Services;
using MorWalPizVideo.Domain;

namespace MorWalPizVideo.ServerAPI.Controllers;

[Route("api/shop/orders")]
[AllowAnonymous]
public sealed class ShopOrdersController(
    IShopOrderRepository orders,
    IShopCustomerSessionService sessions,
    IDigitalProductRepository products,
    IBlobService blobs) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders()
    {
        var customerId = await sessions.ResolveCustomerIdAsync(Request);
        if (customerId is null)
            return Unauthorized();

        return Ok((await orders.GetByCustomerIdAsync(customerId)).Select(ToContract));
    }

    [HttpGet("{id}/items/{productId}/download")]
    public async Task<IActionResult> GetDownload(string id, string productId)
    {
        var customerId = await sessions.ResolveCustomerIdAsync(Request);
        if (customerId is null)
            return Unauthorized();

        var order = await orders.GetItemAsync(id);
        if (order is null || order.CustomerId != customerId || order.Status is ShopOrderStatus.PendingPayment or ShopOrderStatus.Cancelled or ShopOrderStatus.AccessRevoked)
            return NotFound();
        if (!order.Items.Any(item => item.ProductId == productId))
            return NotFound();

        var product = await products.GetItemAsync(productId);
        if (product is null || string.IsNullOrWhiteSpace(product.ContentStorageKey))
            return NotFound();

        var url = await blobs.CreateReadSasUrlAsync(product.ContentStorageKey, TimeSpan.FromHours(24));
        return Ok(new { url, expiresAt = DateTime.UtcNow.AddHours(24) });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        var customerId = await sessions.ResolveCustomerIdAsync(Request);
        if (customerId is null)
            return Unauthorized();

        var order = await orders.GetItemAsync(id);
        return order is null || order.CustomerId != customerId ? NotFound() : Ok(ToContract(order));
    }

    private static ShopOrderContract ToContract(ShopOrder order) => new(
        order.Id,
        order.Items.Select(item => new ShopOrderItemContract(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)).ToList(),
        order.TotalAmount,
        order.Status,
        order.PaymentStatus,
        order.CreationDateTime);
}