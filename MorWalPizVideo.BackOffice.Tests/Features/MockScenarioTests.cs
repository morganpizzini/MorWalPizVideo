using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class MockScenarioTests
{
    [Fact]
    public async Task Scenario_loads_admin_and_keeps_repository_writes_in_memory()
    {
        var scenario = new PrimaryScenario();
        var firstRepository = new UserMockRepository(scenario);
        var secondRepository = new UserMockRepository(scenario);

        var administrator = (await firstRepository.GetItemsAsync())
            .Single(user => user.Username == "MorWalPiz");
        Assert.True(administrator.IsActive);
        Assert.True(administrator.CanAccessBackoffice);

        var added = await firstRepository.AddItemAsync(new User
        {
            Username = "scenario-user",
            Email = "scenario-user@example.test"
        });

        Assert.NotNull(await secondRepository.GetItemAsync(added.Id));

        scenario.Reset();

        Assert.Null(await secondRepository.GetItemAsync(added.Id));
        Assert.NotNull(await secondRepository.AuthenticateAsync(
            PrimaryScenario.AdminUsername,
            PrimaryScenario.AdminPassword));
    }

    [Fact]
    public async Task Repository_reads_return_detached_snapshots()
    {
        var scenario = new PrimaryScenario();
        var repository = new MatchMockRepository(scenario);
        var match = (await repository.GetItemsAsync()).First();
        var originalTitle = match.VideoRefs.FirstOrDefault()?.Title;

        if (match.VideoRefs.Length > 0)
            match.VideoRefs[0] = match.VideoRefs[0] with { Title = "mutated without update" };

        var storedMatch = await repository.GetItemAsync(match.Id);
        Assert.Equal(originalTitle, storedMatch.VideoRefs.FirstOrDefault()?.Title);
    }

    [Fact]
    public async Task Mock_authentication_rejects_an_invalid_password()
    {
        var scenario = new PrimaryScenario();
        var repository = new UserMockRepository(scenario);

        var user = await repository.AuthenticateAsync("MorWalPiz", "not-the-password");

        Assert.Null(user);
    }
}
