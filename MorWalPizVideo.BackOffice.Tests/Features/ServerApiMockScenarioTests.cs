using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class ServerApiMockScenarioTests
{
    [Fact]
    public async Task Matches_endpoint_reads_the_canonical_scenario()
    {
        await using var factory = new ServerApiWebApplicationFactory();
        using var client = factory.CreateClient();
        var scenario = factory.Services.GetRequiredService<IMockScenario>();
    var expectedMatchId = scenario.Read<YouTubeContent>("matches").First().Id;

        var response = await client.GetAsync("/api/Matches?take=1000");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedMatchId, content, StringComparison.Ordinal);
    }
}
