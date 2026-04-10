using Microsoft.AspNetCore.Mvc;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using System.Security.Cryptography;
using System.Text;

namespace MorWalPizVideo.BackOffice.Controllers;

[ApiController]
[Route("api/shop/auth")]
public class ShopAuthController : ControllerBase
{
    private readonly DataService _dataService;
    private readonly ILogger<ShopAuthController> _logger;
    private readonly IConfiguration _configuration;

    // List of disposable email domains to block
    private static readonly HashSet<string> DisposableEmailDomains = new()
    {
        "10minutemail.com",
        "guerrillamail.com",
        "mailinator.com",
        "tempmail.com",
        "throwaway.email",
        "temp-mail.org"
    };

    public ShopAuthController(
        DataService dataService,
        ILogger<ShopAuthController> logger,
        IConfiguration configuration)
    {
        _dataService = dataService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] EmailLoginRequest request)
    {
        try
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            {
                return BadRequest("Invalid email format");
            }

            // Normalize email
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            // Check for disposable email
            if (IsDisposableEmail(normalizedEmail))
            {
                _logger.LogWarning("Disposable email attempt: {Email}", normalizedEmail);
                return BadRequest("Disposable email addresses are not allowed");
            }

            // Verify reCAPTCHA token (in production)
            // For now, we'll skip this validation in the controller
            // It should be done via a middleware or service

            if (!request.TermsAccepted)
            {
                return BadRequest("Terms and conditions must be accepted");
            }

            // Get or create customer
            var customers = await _dataService.GetCustomersAsync();
            var customer = customers.FirstOrDefault(c => c.Email == normalizedEmail);

            if (customer == null)
            {
                // Create new customer
                customer = new Customer(
                    email: normalizedEmail,
                    name: null,
                    newsletterAccepted: request.NewsletterAccepted,
                    termsAccepted: request.TermsAccepted,
                    termsAcceptedAt: DateTime.UtcNow
                )
                {
                    Id = Guid.NewGuid().ToString()
                };

                await _dataService.SaveCustomerAsync(customer);
                _logger.LogInformation("New customer created: {CustomerId}", customer.Id);
            }
            else
            {
                // Update last login
                customer = customer with
                {
                    LastLoginAt = DateTime.UtcNow
                };
                await _dataService.SaveCustomerAsync(customer);
                _logger.LogInformation("Customer login: {CustomerId}", customer.Id);
            }

            // Generate session token
            var sessionToken = GenerateSessionToken(customer);

            return Ok(new ShopLoginResponse {
                CustomerId= customer.Id,
                Email= customer.Email,
                SessionToken= sessionToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, "Error during login");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDisposableEmail(string email)
    {
        var domain = email.Split('@').LastOrDefault();
        return domain != null && DisposableEmailDomains.Contains(domain);
    }

    private static string GenerateSessionToken(Customer customer)
    {
        // Generate a secure session token
        var data = $"{customer.Id}:{customer.Email}:{DateTime.UtcNow.Ticks}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}