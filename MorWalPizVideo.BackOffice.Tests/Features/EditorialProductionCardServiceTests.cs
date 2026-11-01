using FluentAssertions;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class EditorialProductionCardServiceTests
{
    [Fact]
    public async Task Create_normalizes_sources_and_starts_at_revision_one()
    {
        var service = CreateService();

        var created = await service.CreateAsync(new EditorialProductionCard
        {
            ChannelId = "channel-a",
            Title = "  New production  ",
            SourceUrls = [" https://example.test/source ", "https://example.test/source"]
        });

        created.Title.Should().Be("New production");
        created.SourceUrls.Should().ContainSingle().Which.Should().Be("https://example.test/source");
        created.Revision.Should().Be(1);
    }

    [Fact]
    public async Task Update_rejects_stale_revision_and_invalid_transition()
    {
        var repository = new InMemoryRepository();
        var service = new EditorialProductionCardService(repository, new AllowAllReferenceValidator(), new NullPlanSource());
        var created = await service.CreateAsync(new EditorialProductionCard { ChannelId = "channel-a", Title = "Production" });

        var stale = await service.UpdateAsync(created with { Status = EditorialProductionCardStatus.Planned }, "channel-a", 99);
        stale.Should().BeNull();

        var invalid = async () => await service.UpdateAsync(created with { Status = EditorialProductionCardStatus.Published }, "channel-a", 1);
        await invalid.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Get_and_update_are_isolated_by_channel()
    {
        var repository = new InMemoryRepository();
        var service = new EditorialProductionCardService(repository, new AllowAllReferenceValidator(), new NullPlanSource());
        var created = await service.CreateAsync(new EditorialProductionCard { ChannelId = "channel-a", Title = "Private" });

        (await service.GetAsync(created.Id, "channel-b")).Should().BeNull();
        (await service.UpdateAsync(created with { Title = "Leaked" }, "channel-b", 1)).Should().BeNull();
        (await service.GetAsync(created.Id, "channel-a"))!.Title.Should().Be("Private");
    }

    [Fact]
    public async Task Create_rejects_non_http_sources()
    {
        var service = CreateService();

        var act = async () => await service.CreateAsync(new EditorialProductionCard
        {
            ChannelId = "channel-a",
            Title = "Production",
            SourceUrls = ["javascript:alert(1)"]
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Create_rejects_a_reference_that_is_not_owned_by_the_channel()
    {
        var service = new EditorialProductionCardService(new InMemoryRepository(), new RejectingReferenceValidator(), new NullPlanSource());

        var act = async () => await service.CreateAsync(new EditorialProductionCard
        {
            ChannelId = "channel-a",
            Title = "Production",
            ContentPlanId = "plan-from-another-channel"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*channel*");
    }

    [Fact]
    public async Task CreateFromPlan_persists_the_generated_draft_without_changing_its_status()
    {
        var source = new FixedPlanSource(new EditorialProductionCard
        {
            ChannelId = "channel-a",
            Title = "From plan",
            Brief = "Outline",
            ContentPlanId = "plan-a",
            Status = EditorialProductionCardStatus.Idea
        });
        var service = new EditorialProductionCardService(new InMemoryRepository(), new AllowAllReferenceValidator(), source);

        var created = await service.CreateFromPlanAsync("plan-a", "channel-a");

        created.Should().NotBeNull();
        created!.Title.Should().Be("From plan");
        created.ContentPlanId.Should().Be("plan-a");
        created.Status.Should().Be(EditorialProductionCardStatus.Idea);
        created.Revision.Should().Be(1);
    }

    private static EditorialProductionCardService CreateService() =>
        new(new InMemoryRepository(), new AllowAllReferenceValidator(), new NullPlanSource());

    private sealed class NullPlanSource : IEditorialProductionCardPlanSource
    {
        public Task<EditorialProductionCard?> CreateDraftAsync(string planId, string channelId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EditorialProductionCard?>(null);
    }

    private sealed class FixedPlanSource(EditorialProductionCard draft) : IEditorialProductionCardPlanSource
    {
        public Task<EditorialProductionCard?> CreateDraftAsync(string planId, string channelId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EditorialProductionCard?>(draft);
    }

    private sealed class AllowAllReferenceValidator : IEditorialProductionCardReferenceValidator
    {
        public Task ValidateAsync(EditorialProductionCard item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RejectingReferenceValidator : IEditorialProductionCardReferenceValidator
    {
        public Task ValidateAsync(EditorialProductionCard item, CancellationToken cancellationToken = default) =>
            throw new ArgumentException("The content plan does not belong to the selected channel.", nameof(item));
    }

    private sealed class InMemoryRepository : IEditorialProductionCardRepository
    {
        private readonly List<EditorialProductionCard> items = [];

        public Task<EditorialProductionCard> GetItemAsync(string id) =>
            Task.FromResult(items.First(item => item.Id == id));

        public Task<IList<EditorialProductionCard>> GetItemsAsync() =>
            Task.FromResult<IList<EditorialProductionCard>>(items.ToList());

        public Task<IList<EditorialProductionCard>> GetItemsAsync(System.Linq.Expressions.Expression<Func<EditorialProductionCard, bool>> predicate) =>
            Task.FromResult<IList<EditorialProductionCard>>(items.AsQueryable().Where(predicate).ToList());

        public Task<EditorialProductionCard> AddItemAsync(EditorialProductionCard item)
        {
            items.Add(item);
            return Task.FromResult(item);
        }

        public Task UpdateItemAsync(EditorialProductionCard item)
        {
            var index = items.FindIndex(existing => existing.Id == item.Id);
            items[index] = item;
            return Task.CompletedTask;
        }

        public Task DeleteItemAsync(string id)
        {
            items.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }
}