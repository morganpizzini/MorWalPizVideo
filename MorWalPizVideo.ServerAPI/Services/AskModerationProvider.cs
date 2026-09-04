using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.ServerAPI.Services;

public sealed class AskModerationProvider(
    IChatCompletionService chatCompletionService,
    ILogger<AskModerationProvider> logger) : IAskModerationProvider
{
    public async Task<AskModerationResult?> AssessAsync(string content, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage("Assess the user message for moderation. Return only JSON with score (0..1, higher is safer), categories (array of short labels), decision (approve, reject, review), and reason (short, generic, no quoted user text). Do not repeat or transform the user message.");
        history.AddUserMessage(content);

        var response = await chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
        var json = response.Content?.Trim();
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            var parsed = JsonSerializer.Deserialize<ModerationPayload>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (parsed is null || parsed.Score is < 0 or > 1 || string.IsNullOrWhiteSpace(parsed.Decision)) return null;
            var decision = parsed.Decision.Trim().ToLowerInvariant() switch
            {
                "approve" => AskModerationDecision.Approve,
                "reject" => AskModerationDecision.Reject,
                "review" => AskModerationDecision.Review,
                _ => AskModerationDecision.Review
            };
            return new AskModerationResult(
                parsed.Score,
                (parsed.Categories ?? []).Where(category => !string.IsNullOrWhiteSpace(category)).Select(category => category.Trim()).Take(20).ToArray(),
                decision,
                string.IsNullOrWhiteSpace(parsed.Reason) ? "AI assessment completed." : parsed.Reason.Trim()[..Math.Min(parsed.Reason.Trim().Length, 240)]);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ask AI moderation returned an invalid assessment");
            return null;
        }
    }

    private sealed record ModerationPayload(double Score, string[]? Categories, string Decision, string? Reason);
}