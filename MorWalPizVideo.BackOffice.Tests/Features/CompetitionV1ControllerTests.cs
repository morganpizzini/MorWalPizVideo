using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class CompetitionV1ControllerTests(ServerApiWebApplicationFactory factory)
    : IClassFixture<ServerApiWebApplicationFactory>
{
    [Fact]
    public async Task V1_detail_returns_problem_details_for_missing_competition()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/competitions/missing-competition");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Competition not found", json.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task V1_index_returns_dto_shape_without_persistence_only_fields()
    {
        var repository = factory.Services.GetRequiredService<ICompetitionRepository>();
        var competition = new Competition
        {
            Id = "competition-v1-test",
            Name = "V1 competition",
            StartDate = DateTime.UtcNow,
            Stages =
            [
                new Stage
                {
                    StageId = "stage-1",
                    Name = "Stage 1",
                    Evaluations =
                    [
                        new StageEvaluation { UserId = "private-user-id", Comment = "private comment" }
                    ]
                }
            ]
        };
        await repository.AddItemAsync(competition);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/competitions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetString() == competition.Id);
        Assert.Equal("V1 competition", item.GetProperty("name").GetString());
        Assert.False(item.GetProperty("stages")[0].TryGetProperty("evaluations", out _));
    }
}
