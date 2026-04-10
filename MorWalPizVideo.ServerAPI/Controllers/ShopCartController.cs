using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [Route("api/shop/cart")]
    public class ShopCartController : ApplicationController
    {
        private readonly ICartRepository _cartRepository;
        private readonly IDigitalProductRepository _productRepository;

        public ShopCartController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache,
            ICartRepository cartRepository,
            IDigitalProductRepository productRepository) : base(dataService, memoryCache)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCart(string customerId)
        {
            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
            {
                // Create empty cart
                cart = new Cart(
                    customerId: customerId,
                    items: new List<CartItem>(),
                    isCompleted: false,
                    completedAt: null
                )
                {
                    Id = Guid.NewGuid().ToString()
                };
                await _cartRepository.AddItemAsync(cart);
            }

            return Ok(cart);
        }

        [HttpPost("{customerId}/items")]
        public async Task<IActionResult> AddToCart(string customerId, [FromBody] AddToCartRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ProductId))
                return BadRequest(new { message = "ProductId is required" });

            if (request.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0" });

            // Get product
            var product = await _productRepository.GetItemAsync(request.ProductId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            if (!product.IsActive)
                return BadRequest(new { message = "Product is not available" });

            // Get or create cart
            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
            {
                cart = new Cart(
                    customerId: customerId,
                    items: new List<CartItem>(),
                    isCompleted: false,
                    completedAt: null
                )
                {
                    Id = Guid.NewGuid().ToString()
                };
            }

            // Update cart items
            var items = cart.Items.ToList();
            var existingItem = items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                // Update quantity
                items.Remove(existingItem);
                items.Add(new CartItem(
                    productId: existingItem.ProductId,
                    productName: product.Name,
                    quantity: existingItem.Quantity + request.Quantity,
                    price: product.Price
                ));
            }
            else
            {
                // Add new item
                items.Add(new CartItem(
                    productId: product.Id,
                    productName: product.Name,
                    quantity: request.Quantity,
                    price: product.Price
                ));
            }

            cart = cart with { Items = items };

            if (string.IsNullOrEmpty(cart.Id))
            {
                cart = cart with { Id = Guid.NewGuid().ToString() };
                await _cartRepository.AddItemAsync(cart);
            }
            else
            {
                await _cartRepository.UpdateItemAsync(cart);
            }

            return Ok(cart);
        }

        [HttpPut("{customerId}/items/{productId}")]
        public async Task<IActionResult> UpdateCartItem(string customerId, string productId, [FromBody] UpdateCartItemRequest request)
        {
            if (request.Quantity < 0)
                return BadRequest(new { message = "Quantity cannot be negative" });

            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
                return NotFound(new { message = "Cart not found" });

            var items = cart.Items.ToList();
            var existingItem = items.FirstOrDefault(i => i.ProductId == productId);

            if (existingItem == null)
                return NotFound(new { message = "Item not found in cart" });

            if (request.Quantity == 0)
            {
                // Remove item
                items.Remove(existingItem);
            }
            else
            {
                // Update quantity
                items.Remove(existingItem);
                items.Add(existingItem with { Quantity = request.Quantity });
            }

            cart = cart with { Items = items };
            await _cartRepository.UpdateItemAsync(cart);

            return Ok(cart);
        }

        [HttpDelete("{customerId}/items/{productId}")]
        public async Task<IActionResult> RemoveFromCart(string customerId, string productId)
        {
            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
                return NotFound(new { message = "Cart not found" });

            var items = cart.Items.Where(i => i.ProductId != productId).ToList();
            cart = cart with { Items = items };

            await _cartRepository.UpdateItemAsync(cart);

            return Ok(cart);
        }

        [HttpDelete("{customerId}")]
        public async Task<IActionResult> ClearCart(string customerId)
        {
            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
                return NotFound(new { message = "Cart not found" });

            cart = cart with { Items = new List<CartItem>() };
            await _cartRepository.UpdateItemAsync(cart);

            return Ok(cart);
        }

        [HttpPost("{customerId}/checkout")]
        public async Task<IActionResult> Checkout(string customerId)
        {
            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Cart is empty" });

            // Since checkout is free, just clear the cart and return success
            cart = cart with { Items = new List<CartItem>() };
            await _cartRepository.UpdateItemAsync(cart);

            return Ok(new
            {
                success = true,
                message = "Checkout completed successfully",
                orderId = Guid.NewGuid().ToString()
            });
        }
    }

    public record AddToCartRequest(string ProductId, int Quantity);
    public record UpdateCartItemRequest(int Quantity);
}