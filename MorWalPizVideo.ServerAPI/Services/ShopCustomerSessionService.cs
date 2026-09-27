using System.Security.Cryptography;
using System.Text;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.ServerAPI.Services;

public interface IShopCustomerSessionService
{
    Task<string> CreateAsync(string customerId, HttpResponse response, CancellationToken cancellationToken = default);
    Task<string?> ResolveCustomerIdAsync(HttpRequest request, CancellationToken cancellationToken = default);
}

public sealed class ShopCustomerSessionService(IShopCustomerSessionRepository sessions) : IShopCustomerSessionService
{
    public async Task<string> CreateAsync(string customerId, HttpResponse response, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var session = new ShopCustomerSession(customerId, Hash(token), DateTime.UtcNow.AddHours(24))
        {
            Id = Guid.NewGuid().ToString("N")
        };
        await sessions.AddItemAsync(session);
        response.Cookies.Append("shop_session", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = session.ExpiresAt,
            IsEssential = true
        });
        return token;
    }

    public async Task<string?> ResolveCustomerIdAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var token = request.Cookies["shop_session"];
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var session = await sessions.GetActiveByTokenHashAsync(Hash(token), DateTime.UtcNow);
        return session?.CustomerId;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}