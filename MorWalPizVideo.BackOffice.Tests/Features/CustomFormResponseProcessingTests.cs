using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MorWalPizVideo.BackOffice.Jobs;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Converters;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class CustomFormResponseProcessingTests
{
    [Fact]
    public async Task HistoricalResponse_IsClaimedOnce_AndProcessedStateIsNotClaimedAgain()
    {
        var repository = new CustomFormResponseMockRepository(new PrimaryScenario());
        var document = CustomFormResponseDocument.FromResponse(
            "form-1",
            new CustomFormResponse("response-1", DateTime.UtcNow.AddDays(-1), [new EmailAnswer("email", "person@example.com")]));

        await repository.UpsertByFormAndResponseIdAsync(document);

        var claimed = await repository.ClaimBatchAsync(
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15),
            "worker-1",
            10);

        Assert.Single(claimed);
        Assert.Equal(ResponseProcessingStatus.Claimed, claimed[0].ProcessingStatus);
        Assert.Empty(await repository.ClaimBatchAsync(
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15),
            "worker-2",
            10));

        Assert.True(await repository.MarkProcessedAsync("form-1", "response-1", "worker-1"));
        Assert.Empty(await repository.ClaimBatchAsync(
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15),
            "worker-3",
            10));
    }

    [Fact]
    public void EmailQuestionAndAnswer_RoundTripThroughPolymorphicJson()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CustomFormQuestionJsonConverter());
        options.Converters.Add(new CustomFormAnswerJsonConverter());

        var questionJson = JsonSerializer.Serialize<CustomFormQuestion>(
            new EmailQuestion("email", "Email address", true, 1), options);
        var answerJson = JsonSerializer.Serialize<CustomFormAnswer>(
            new EmailAnswer("email", "person@example.com"), options);

        var question = JsonSerializer.Deserialize<CustomFormQuestion>(questionJson, options);
        var answer = JsonSerializer.Deserialize<CustomFormAnswer>(answerJson, options);

        Assert.IsType<EmailQuestion>(question);
        Assert.Equal(QuestionType.Email, question!.QuestionType);
        Assert.IsType<EmailAnswer>(answer);
        Assert.Equal("person@example.com", ((EmailAnswer)answer!).Email);
    }

    [Fact]
    public async Task Job_BackfillsHistoricalResponses_SendsAcknowledgement_AndSkipsInvalidEmailPermanently()
    {
        var scenario = new PrimaryScenario();
        var formRepository = new CustomFormMockRepository(scenario);
        var responseRepository = new CustomFormResponseMockRepository(scenario);
        var emailService = new SmtpMockService();
        var formsService = new FormsService(formRepository, responseRepository);
        var submittedAt = DateTime.UtcNow.AddDays(-2);
        var emailForm = new CustomForm(
            "Email form",
            "Description",
            "email-form",
            [new EmailQuestion("email", "Email address", true, 1)]) { Id = "email-form" };
        var validResponse = new CustomFormResponse(
            "valid-response",
            submittedAt,
            [new EmailAnswer("email", "person@example.com")]);
        await formRepository.AddItemAsync(emailForm.AddResponse(validResponse));

        var invalidForm = new CustomForm(
            "Invalid form",
            "Description",
            "invalid-form",
            [new EmailQuestion("email", "Email address", false, 1)]) { Id = "invalid-form" };
        var invalidResponse = new CustomFormResponse(
            "invalid-response",
            submittedAt,
            [new EmailAnswer("email", "not-an-email")]);
        await formRepository.AddItemAsync(invalidForm.AddResponse(invalidResponse));

        var job = new CustomFormResponseEmailJob(
            responseRepository,
            formRepository,
            formsService,
            emailService,
            NullLogger<CustomFormResponseEmailJob>.Instance);

        await job.ExecuteAsync();

        var processed = (await responseRepository.GetItemsAsync(item => item.ResponseId == "valid-response")).Single();
        var skipped = (await responseRepository.GetItemsAsync(item => item.ResponseId == "invalid-response")).Single();
        Assert.Equal(ResponseProcessingStatus.Processed, processed.ProcessingStatus);
        Assert.Equal(1, processed.ProcessingAttemptCount);
        Assert.Equal(ResponseProcessingStatus.Skipped, skipped.ProcessingStatus);
        Assert.Single(emailService.Messages);
        Assert.Equal("person@example.com", emailService.Messages[0].Recipient);

        await job.ExecuteAsync();

        var processedAfterSecondRun = (await responseRepository.GetItemsAsync(item => item.ResponseId == "valid-response")).Single();
        Assert.Equal(1, processedAfterSecondRun.ProcessingAttemptCount);
        Assert.Single(emailService.Messages);
    }
}
