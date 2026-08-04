using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public class FormsMigrationSafetyTests
{
    [Fact]
    public async Task GetResponses_UsesStandaloneCollectionOnlyAndPreservesOrder()
    {
        var scenario = new PrimaryScenario();
        var formRepository = new CustomFormMockRepository(scenario);
        var responseRepository = new CustomFormResponseMockRepository(scenario);
        var service = new FormsService(formRepository, responseRepository);

        var both = new CustomFormResponse(
            responseId: "r-both",
            submittedAt: DateTime.UtcNow.AddMinutes(-5),
            answers: [new OpenAnswer("q1", "both")]);

        var newest = new CustomFormResponse(
            responseId: "r-newest",
            submittedAt: DateTime.UtcNow,
            answers: [new OpenAnswer("q1", "newest")]);

        var form = new CustomForm(
            title: "Form",
            description: "Desc",
            url: "form-url",
            questions: [new OpenQuestion("q1", "Question", true, 1)],
            active: true)
        {
            Id = "form-1",
            Responses = [new CustomFormResponse(
                "r-embedded",
                DateTime.UtcNow.AddMinutes(-10),
                [new OpenAnswer("q1", "embedded")]), both]
        };

        await formRepository.AddItemAsync(form);
        await responseRepository.UpsertByFormAndResponseIdAsync(CustomFormResponseDocument.FromResponse("form-1", both));
        await responseRepository.UpsertByFormAndResponseIdAsync(CustomFormResponseDocument.FromResponse("form-1", newest));

        var responses = await service.GetResponsesAsync("form-1", limit: 50);

        Assert.Equal(2, responses.Count);
        Assert.Equal(["r-newest", "r-both"], responses.Select(x => x.ResponseId));
    }

    [Fact]
    public async Task Backfill_IsIdempotent_AndReconcileMatchesAfterSecondRun()
    {
        var scenario = new PrimaryScenario();
        var formRepository = new CustomFormMockRepository(scenario);
        var responseRepository = new CustomFormResponseMockRepository(scenario);
        var service = new FormsService(formRepository, responseRepository);

        var form = new CustomForm(
            title: "Form",
            description: "Desc",
            url: "form-url",
            questions: [new OpenQuestion("q1", "Question", true, 1)],
            active: true)
        {
            Id = "form-2",
            Responses =
            [
                new CustomFormResponse("r1", DateTime.UtcNow.AddMinutes(-20), [new OpenAnswer("q1", "a1")]),
                new CustomFormResponse("r2", DateTime.UtcNow.AddMinutes(-10), [new OpenAnswer("q1", "a2")])
            ]
        };

        await formRepository.AddItemAsync(form);

        var firstRun = await service.BackfillEmbeddedResponsesAsync(continuationToken: null, batchSize: 10);
        var secondRun = await service.BackfillEmbeddedResponsesAsync(continuationToken: null, batchSize: 10);
        var reconciliation = await service.ReconcileCountsAsync("form-2");

        Assert.Equal(2, firstRun.UpsertedResponses);
        Assert.Equal(0, secondRun.UpsertedResponses);
        Assert.NotNull(reconciliation);
        Assert.True(reconciliation!.IsMatch);
        Assert.Equal(2, reconciliation.EmbeddedCount);
        Assert.Equal(2, reconciliation.CollectionCount);
    }
}
