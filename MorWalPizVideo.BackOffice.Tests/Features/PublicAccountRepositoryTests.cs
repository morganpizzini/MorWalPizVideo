using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MongoDB.Bson;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class PublicAccountRepositoryTests
{
    [Fact]
    public void Directory_sort_uses_only_username()
    {
        Assert.Equal(new BsonDocument { { "username", 1 } }, PublicAccountRepository.ListSortKeys);
    }

    [Fact]
    public async Task Mock_directory_sort_preserves_source_order_for_equal_usernames()
    {
        var scenario = new PrimaryScenario();
        var repository = new PublicAccountMockRepository(scenario);
        await repository.AddAccountAsync(new PublicAccount
        {
            Id = "z-account",
            Username = "same",
            DisplayName = "Later",
            CreationDateTime = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        });
        await repository.AddAccountAsync(new PublicAccount
        {
            Id = "a-account",
            Username = "same",
            DisplayName = "Earlier",
            CreationDateTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var results = await repository.ListAccountsAsync(null, 1, 50);

        Assert.Equal(["z-account", "a-account"], results.Select(account => account.Id));
    }
}
