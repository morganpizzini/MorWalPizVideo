using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Controllers;

[Authorize]
[Route("api/admin/shop/orders")]
public sealed class AdminShopOrdersController(IShopOrderRepository orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders() => Ok(await orders.GetItemsAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(string id)
    {
        var order = await orders.GetItemAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id}/payment-confirmation")]
    public async Task<IActionResult> ConfirmPayment(string id)
    {
        var order = await orders.GetItemAsync(id);
        if (order is null) return NotFound();
        if (order.Status != ShopOrderStatus.PendingPayment || order.PaymentStatus != ShopPaymentStatus.Pending)
            return Conflict(new { code = "order_not_pending_payment" });

        var confirmed = order with { Status = ShopOrderStatus.PaymentConfirmed, PaymentStatus = ShopPaymentStatus.Confirmed, PaymentConfirmedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await orders.UpdateItemAsync(confirmed);
        return Ok(confirmed);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id)
    {
        var order = await orders.GetItemAsync(id);
        if (order is null) return NotFound();
        if (order.Status is ShopOrderStatus.Cancelled or ShopOrderStatus.AccessRevoked)
            return Conflict(new { code = "order_already_cancelled" });

        var cancelled = order with { Status = ShopOrderStatus.Cancelled, CancelledAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await orders.UpdateItemAsync(cancelled);
        return Ok(cancelled);
    }

    [HttpPost("{id}/restore-access")]
    public async Task<IActionResult> RestoreAccess(string id)
    {
        var order = await orders.GetItemAsync(id);
        if (order is null) return NotFound();
        if (order.PaymentStatus != ShopPaymentStatus.Confirmed || order.Status != ShopOrderStatus.Cancelled)
            return Conflict(new { code = "order_not_restoreable" });

        var restored = order with { Status = ShopOrderStatus.PaymentConfirmed, CancelledAt = null, UpdatedAt = DateTime.UtcNow };
        await orders.UpdateItemAsync(restored);
        return Ok(restored);
    }
}