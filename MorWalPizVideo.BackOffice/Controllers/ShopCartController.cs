using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/shop/cart")]
public class ShopCartController : ControllerBase
{
    private readonly DataService _dataService;
    private readonly ILogger<ShopCartController> _logger;

    public ShopCartController(DataService dataService, ILogger<ShopCartController> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<Cart>> GetCart([FromQuery] string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest("Customer ID is required");
        }

        try
        {
            var cart = await _dataService.GetOrCreateCartAsync(customerId);
            return Ok(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Error fetching cart");
        }
    }

    [HttpPost("items")]
    public async Task<ActionResult<Cart>> AddItem([FromQuery] string customerId, [FromBody] AddToCartRequest request)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest("Customer ID is required");
        }

        try
        {
            // Get product to verify it exists and get price
            var product = await _dataService.GetDigitalProductByIdAsync(request.ProductId);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            if (!product.IsActive)
            {
                return BadRequest("Product is not available");
            }

            // Get or create cart
            var cart = await _dataService.GetOrCreateCartAsync(customerId);

            // Check if item already exists
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            
            List<CartItem> updatedItems;
            if (existingItem != null)
            {
                // Update quantity
                updatedItems = cart.Items.Select(i =>
                    i.ProductId == request.ProductId
                        ? i with { Quantity = i.Quantity + request.Quantity }
                        : i
                ).ToList();
            }
            else
            {
                // Add new item
                var newItem = new CartItem(
                    productId: request.ProductId,
                    productName: product.Name,
                    quantity: request.Quantity,
                    price: product.Price
                );
                updatedItems = new List<CartItem>(cart.Items) { newItem };
            }

            var updatedCart = cart with
            {
                Items = updatedItems,
                UpdatedAt = DateTime.UtcNow
            };

            await _dataService.SaveCartAsync(updatedCart);
            return Ok(updatedCart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Error adding item to cart");
        }
    }

    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<Cart>> RemoveItem([FromQuery] string customerId, string productId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest("Customer ID is required");
        }

        try
        {
            var cart = await _dataService.GetOrCreateCartAsync(customerId);
            
            var updatedItems = cart.Items.Where(i => i.ProductId != productId).ToList();
            
            var updatedCart = cart with
            {
                Items = updatedItems,
                UpdatedAt = DateTime.UtcNow
            };

            await _dataService.SaveCartAsync(updatedCart);
            return Ok(updatedCart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart for customer {CustomerId}", customerId);
            return StatusCode(500, "Error removing item from cart");
        }
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout([FromQuery] string customerId, [FromBody] CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest("Customer ID is required");
        }

        try
        {
            var cart = await _dataService.GetOrCreateCartAsync(customerId);

            if (cart.Items.Count == 0)
            {
                return BadRequest("Cart is empty");
            }

            // Calculate total
            var totalAmount = cart.Items.Sum(i => (i.Price ?? 0) * i.Quantity);

            // TODO: Integrate with payment provider (Stripe)
            // For now, simulate successful payment
            var orderId = Guid.NewGuid().ToString();

            // Mark cart as completed
            var completedCart = cart with
            {
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dataService.SaveCartAsync(completedCart);

            _logger.LogInformation("Order {OrderId} completed for customer {CustomerId}", orderId, customerId);

            return Ok(new CheckoutResponse(
                OrderId: orderId,
                Status: "completed",
                TotalAmount: totalAmount,
                PaymentIntentId: request.PaymentIntentId,
                PaymentClientSecret: null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout for customer {CustomerId}", customerId);
            return StatusCode(500, "Error during checkout");
        }
    }
}