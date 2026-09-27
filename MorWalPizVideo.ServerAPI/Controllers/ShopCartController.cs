using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.ServerAPI.Services;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [Route("api/shop/cart")]
    [AllowAnonymous]
    public class ShopCartController : ApplicationController
    {
        private readonly ICartRepository _cartRepository;
        private readonly IDigitalProductRepository _productRepository;

        public ShopCartController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache,
            ICartRepository cartRepository,
            IDigitalProductRepository productRepository,
            IShopOrderRepository orderRepository,
            IShopCustomerSessionService sessionService) : base(dataService, memoryCache)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _sessionService = sessionService;
        }

        private readonly IShopOrderRepository _orderRepository;
        private readonly IShopCustomerSessionService _sessionService;

        [HttpGet]
        public async Task<IActionResult> GetCurrentCart()
        {
            var customerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (customerId is null)
                return Unauthorized();

            var cart = (await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId)).FirstOrDefault();
            return Ok(cart ?? new Cart(customerId, [], false, null) { Id = Guid.NewGuid().ToString("N") });
        }

        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCart(string customerId)
        {
            var sessionCustomerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (sessionCustomerId is null) return Unauthorized();
            customerId = sessionCustomerId;
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
            var sessionCustomerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (sessionCustomerId is null) return Unauthorized();
            customerId = sessionCustomerId;
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
            var sessionCustomerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (sessionCustomerId is null) return Unauthorized();
            customerId = sessionCustomerId;
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
            var sessionCustomerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (sessionCustomerId is null) return Unauthorized();
            customerId = sessionCustomerId;
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
            var sessionCustomerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (sessionCustomerId is null) return Unauthorized();
            customerId = sessionCustomerId;
            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
                return NotFound(new { message = "Cart not found" });

            cart = cart with { Items = new List<CartItem>() };
            await _cartRepository.UpdateItemAsync(cart);

            return Ok(cart);
        }

        [HttpPost("{customerId}/checkout")]
        public async Task<IActionResult> Checkout(string customerId, [FromBody] CreateShopOrderRequest request)
        {
            var sessionCustomerId = await _sessionService.ResolveCustomerIdAsync(Request);
            if (sessionCustomerId is null)
                return Unauthorized();
            customerId = sessionCustomerId;

            var carts = await _cartRepository.GetItemsAsync(c => c.CustomerId == customerId);
            var cart = carts.FirstOrDefault();

            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Cart is empty" });

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return BadRequest(new { message = "IdempotencyKey is required" });

            var existingOrder = await _orderRepository.GetByCustomerAndIdempotencyKeyAsync(customerId, request.IdempotencyKey);
            if (existingOrder is not null)
                return Ok(existingOrder);

            var orderItems = cart.Items.Select(item => new ShopOrderItem(item.ProductId, item.ProductName, item.Quantity, item.Price ?? 0m)).ToList();
            var order = new ShopOrder(customerId, request.IdempotencyKey, orderItems, orderItems.Sum(item => item.LineTotal))
            {
                Id = Guid.NewGuid().ToString("N")
            };
            await _orderRepository.AddItemAsync(order);

            cart = cart with { Items = new List<CartItem>() };
            await _cartRepository.UpdateItemAsync(cart);

            return Ok(order);
        }
    }

    public record AddToCartRequest(string ProductId, int Quantity);
    public record UpdateCartItemRequest(int Quantity);
}