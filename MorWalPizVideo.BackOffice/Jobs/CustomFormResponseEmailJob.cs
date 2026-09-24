using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Jobs;

public sealed class CustomFormResponseEmailJob(
    ICustomFormResponseRepository responseRepository,
    ICustomFormRepository formRepository,
    IFormsService formsService,
    INewsletterEmailService emailService,
    ILogger<CustomFormResponseEmailJob> logger)
{
    public const string JobId = "custom-form-response-email-job";
    public const string CronConfigurationKey = "CustomForms:ResponseProcessingCron";
    public const string DefaultCronSchedule = "*/10 * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        string? continuationToken = null;
        do
        {
            var backfill = await formsService.BackfillEmbeddedResponsesAsync(continuationToken, 200);
            continuationToken = backfill.NextContinuationToken;
        }
        while (continuationToken is not null);

        var workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
        var claimed = await responseRepository.ClaimBatchAsync(
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(15),
            workerId,
            100);

        foreach (var response in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessAsync(response, workerId, cancellationToken);
        }
    }

    private async Task ProcessAsync(CustomFormResponseDocument response, string workerId, CancellationToken cancellationToken)
    {
        try
        {
            var form = await formRepository.GetItemAsync(response.FormId);
            var emailAnswer = response.Answers.OfType<EmailAnswer>().FirstOrDefault();
            if (form is null || emailAnswer is null || !TryGetEmail(emailAnswer.Email, out var recipient))
            {
                await responseRepository.MarkSkippedAsync(
                    response.FormId,
                    response.ResponseId,
                    workerId,
                    form is null ? "form-not-found" : emailAnswer is null ? "email-answer-not-found" : "invalid-email");
                return;
            }

            var subject = $"We received your response to {form.Title}";
            var body = BuildAcknowledgementBody(form, response);
            var providerKey = $"custom-form-response:{response.FormId}:{response.ResponseId}";
            await emailService.SendAsync(recipient, subject, body, providerKey, cancellationToken);
            await responseRepository.MarkProcessedAsync(response.FormId, response.ResponseId, workerId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var error = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
            await responseRepository.MarkFailedAsync(response.FormId, response.ResponseId, workerId, error);
            logger.LogError(exception, "Custom form response email failed for form {FormId} and response {ResponseId}", response.FormId, response.ResponseId);
        }
    }

    private static bool TryGetEmail(string? value, out string email)
    {
        email = value?.Trim() ?? string.Empty;
        if (email.Length > 254 || email.Any(char.IsWhiteSpace))
            return false;

        try
        {
            var address = new MailAddress(email);
            return string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase)
                && email.Contains('@', StringComparison.Ordinal)
                && email[(email.IndexOf('@') + 1)..].Contains('.', StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string BuildAcknowledgementBody(CustomForm form, CustomFormResponseDocument response)
    {
        var builder = new StringBuilder("<p>Thank you. We received the following response:</p><dl>");
        var questions = form.Questions.ToDictionary(question => question.QuestionId);
        foreach (var answer in response.Answers)
        {
            var label = questions.TryGetValue(answer.QuestionId, out var question)
                ? question.QuestionText
                : answer.QuestionId;
            builder.Append("<dt>").Append(HtmlEncoder.Default.Encode(label)).Append("</dt><dd>")
                .Append(HtmlEncoder.Default.Encode(FormatAnswer(answer, question)))
                .Append("</dd>");
        }

        return builder.Append("</dl>").ToString();
    }

    private static string FormatAnswer(CustomFormAnswer answer, CustomFormQuestion? question)
        => answer switch
        {
            OpenAnswer open => open.TextResponse ?? string.Empty,
            EmailAnswer email => email.Email ?? string.Empty,
            BooleanAnswer boolean => boolean.Value ? "True" : "False",
            SingleChoiceAnswer single => GetOptionText(question, single.SelectedOptionId),
            MultipleChoiceAnswer multiple => string.Join(", ", multiple.SelectedOptionIds.Select(id => GetOptionText(question, id))),
            _ => string.Empty
        };

    private static string GetOptionText(CustomFormQuestion? question, string optionId)
    {
        var options = question switch
        {
            MultipleChoiceQuestion multiple => multiple.Options,
            SingleChoiceQuestion single => single.Options,
            _ => []
        };
        return options.FirstOrDefault(option => option.OptionId == optionId)?.OptionText ?? optionId;
    }
}
