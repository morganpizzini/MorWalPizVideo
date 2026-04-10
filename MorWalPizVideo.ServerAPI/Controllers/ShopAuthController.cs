using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Controllers;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Controllers
{
    [Route("api/shop/auth")]
    public class ShopAuthController : ApplicationController
    {
        private readonly ICustomerRepository _customerRepository;

        public ShopAuthController(
            IGenericDataService dataService,
            IMorWalPizCache memoryCache,
            ICustomerRepository customerRepository) : base(dataService, memoryCache)
        {
            _customerRepository = customerRepository;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { message = "Email is required" });

            // Validate terms acceptance
            if (!request.TermsAccepted)
                return BadRequest(new { message = "Terms must be accepted" });

            // Find or create customer
            var customers = await _customerRepository.GetItemsAsync(c => c.Email == request.Email);
            var customer = customers.FirstOrDefault();

            if (customer == null)
            {
                // Create new customer
                customer = new Customer(
                    email: request.Email,
                    name: request.Name,
                    newsletterAccepted: request.NewsletterAccepted,
                    termsAccepted: request.TermsAccepted,
                    termsAcceptedAt: DateTime.UtcNow
                )
                {
                    Id = Guid.NewGuid().ToString()
                };
                await _customerRepository.AddItemAsync(customer);
            }
            else
            {
                // Update last login
                customer = customer with { LastLoginAt = DateTime.UtcNow };
                await _customerRepository.UpdateItemAsync(customer);
            }

            // Generate session token (simple implementation)
            var sessionToken = Guid.NewGuid().ToString();

            return Ok(new
            {
                sessionToken,
                customer = new
                {
                    customer.Id,
                    customer.Email,
                    customer.Name
                }
            });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify([FromBody] VerifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
                return Unauthorized();

            // Simple verification - in production, validate token properly
            return Ok(new { valid = true });
        }
    }

    public record LoginRequest(string Email, string? Name, bool TermsAccepted, bool NewsletterAccepted);
    public record VerifyRequest(string SessionToken);
}