using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
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
        var response = await client.PostAsJsonAsync("/api/faq/missing/answers/channel/vote", value);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Vote_rejects_malformed_enum_payload()
    {
        using var client = factory.CreateClient();
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
}
