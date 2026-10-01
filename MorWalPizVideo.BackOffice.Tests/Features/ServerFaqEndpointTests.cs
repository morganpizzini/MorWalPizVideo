using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.ServerAPI.Controllers;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ServerFaqEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public ServerFaqEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Vote_rejects_undefined_enum_values(int value)
    {
        using var client = factory.CreateClient();
        AddAuthenticatedUser(client);
        var response = await client.PostAsJsonAsync("/api/faq/missing/answers/channel/vote", value);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Vote_rejects_malformed_enum_payload()
    {
        using var client = factory.CreateClient();
        AddAuthenticatedUser(client);
        var response = await client.PostAsJsonAsync("/api/faq/missing/answers/channel/vote", "not-a-vote");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void Vote_uses_authenticated_faq_rate_limit_policy()
    {
        var method = typeof(FaqController).GetMethod(nameof(FaqController.Vote));
        var attribute = method?.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: true)
            .Cast<EnableRateLimitingAttribute>()
            .SingleOrDefault();

        Assert.Equal("faq-vote", attribute?.PolicyName);
    }

    private static void AddAuthenticatedUser(HttpClient client)
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("test-only-signing-material-not-a-live-secret-123456"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "security-tests",
            audience: "security-tests",
            claims: [new Claim(ClaimTypes.NameIdentifier, "test-user-id")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            new JwtSecurityTokenHandler().WriteToken(token));
    }
}
