using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain;

public sealed record AskPublicResponse(string Content, string Author, DateTime CreatedAt);
public sealed record AskPublicQuestion(string Id, string Text, int ReactionCount, AskPublicResponse? Response);
public sealed record AskPublicCampaign(string CampaignId, string ChannelName, string Title, string Description, string Slug, AskPolicy Policy, IReadOnlyList<AskPublicResponse> Responses, IReadOnlyList<AskPublicQuestion> Questions);
public enum AskSubmissionResult { Created, Duplicate, Conflict, NotFound, Closed, RateLimited, Invalid }
public sealed record AskAnalytics(int TotalSubmissions, int PendingSubmissions, int ApprovedSubmissions, int Reactions, int Responses);
public sealed record AskReactionResult(bool Accepted, int Count);

public interface IAskService
{
    Task<IList<AskCampaign>> GetForChannelAsync(string channelId);
    Task<AskCampaign?> GetByIdAsync(string id, string channelId);
    Task<string?> GetChannelNameAsync(string channelId);
    Task<AskPublicCampaign?> GetPublicAsync(string channelName, string slug);
    Task<AskCampaign> CreateAsync(AskCampaign campaign);
    Task<AskCampaign?> UpdateAsync(AskCampaign campaign, string channelId);
    Task<bool> DeleteAsync(string id, string channelId);
    Task<AskSubmissionResult> SubmitAsync(string channelName, string slug, string text, string? idempotencyKey, string? name = null, CancellationToken cancellationToken = default);
    Task<IList<AskSubmission>> GetSubmissionsAsync(string campaignId, string channelId, int limit = 500);
    Task<AskSubmission?> ModerateAsync(string id, string channelId, AskModerationStatus status, string? note);
    Task<AskSubmission?> RespondAsync(string id, string channelId, string content, string author, AskResponseVisibility visibility);
    Task<AskReactionResult> ReactAsync(string channelName, string slug, string submissionId, string fingerprint);
    Task<AskAnalytics?> GetAnalyticsAsync(string campaignId, string channelId);
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<string> Validate(AskCampaign campaign);
    static string NormalizeSlug(string value) => Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}

public sealed class AskService(
    IAskCampaignRepository campaignRepository,
    IAskSubmissionRepository submissionRepository,
    IAskReactionRepository reactionRepository,
    IYTChannelRepository channelRepository,
    IConfiguration configuration,
    IAskModerationProvider moderationProvider,
    ILogger<AskService> logger) : IAskService
{
    private readonly AskPolicy _baseline = new()
    {
        MaxSubmissionLength = configuration.GetValue("Ask:MaxSubmissionLength", 2000),
        RetentionDays = configuration.GetValue("Ask:RetentionDays", 90),
        RateLimitPerHour = configuration.GetValue("Ask:RateLimitPerHour", 5),
        DuplicateWindowMinutes = configuration.GetValue("Ask:DuplicateWindowMinutes", 60),
        RecaptchaRequired = configuration.GetValue("Ask:RecaptchaRequired", true)
    };

    private AskPolicy EffectivePolicy(AskPolicy policy)
    {
        var selected = policy == new AskPolicy() ? _baseline : policy;
        return selected with
        {
            MaxSubmissionLength = selected.MaxSubmissionLength > 0 ? selected.MaxSubmissionLength : _baseline.MaxSubmissionLength,
            RetentionDays = selected.RetentionDays > 0 ? selected.RetentionDays : _baseline.RetentionDays,
            RateLimitPerHour = selected.RateLimitPerHour > 0 ? selected.RateLimitPerHour : _baseline.RateLimitPerHour,
            DuplicateWindowMinutes = selected.DuplicateWindowMinutes > 0 ? selected.DuplicateWindowMinutes : _baseline.DuplicateWindowMinutes
        };
    }

    public Task<IList<AskCampaign>> GetForChannelAsync(string channelId) => campaignRepository.GetByChannelIdAsync(channelId);
    public async Task<AskCampaign?> GetByIdAsync(string id, string channelId)
        => (await campaignRepository.GetItemsAsync(x => x.Id == id && x.ChannelId == channelId)).FirstOrDefault();
    public async Task<string?> GetChannelNameAsync(string channelId)
        => (await channelRepository.GetItemsAsync(x => x.ChannelId == channelId)).FirstOrDefault()?.ChannelName;

    public async Task<AskPublicCampaign?> GetPublicAsync(string channelName, string slug)
    {
        var channel = (await channelRepository.GetItemsAsync(x => x.ChannelName.ToLower() == channelName.Trim().ToLower())).FirstOrDefault();
        if (channel is null) return null;
        var campaign = await campaignRepository.GetByChannelAndSlugAsync(channel.ChannelId, IAskService.NormalizeSlug(slug));
        var now = DateTime.UtcNow;
        if (campaign is not null && campaign.Status == AskCampaignStatus.Published && campaign.EndAt <= now)
        {
            campaign = campaign with { Status = AskCampaignStatus.Closed, ClosedAt = now, UpdatedAt = now };
            await campaignRepository.UpdateItemAsync(campaign);
        }
        return campaign is { Status: AskCampaignStatus.Published } &&
            (campaign.StartAt is null || campaign.StartAt <= now) &&
            (campaign.EndAt is null || campaign.EndAt > now)
            ? await BuildPublicCampaignAsync(campaign with { Policy = EffectivePolicy(campaign.Policy) }, channel.ChannelName)
            : null;
    }

    private async Task<AskPublicCampaign> BuildPublicCampaignAsync(AskCampaign campaign, string channelName)
    {
        var approved = (await submissionRepository.GetByCampaignIdAsync(campaign.Id, 5000))
            .Where(x => x.ModerationStatus == AskModerationStatus.Approved).ToArray();
        var questions = approved.Select(x => new AskPublicQuestion(x.Id, x.Text, x.ReactionCount,
            x.ResponseVisibility == AskResponseVisibility.Public && !string.IsNullOrWhiteSpace(x.ResponseContent)
                ? new AskPublicResponse(x.ResponseContent, x.ResponseAuthor, x.ResponseCreatedAt ?? x.SubmittedAt) : null)).ToArray();
        return new(campaign.Id, channelName, campaign.Title, campaign.Description, campaign.Slug, campaign.Policy,
            questions.Where(x => x.Response is not null).Select(x => x.Response!).ToArray(), questions);
    }

    public async Task<AskCampaign> CreateAsync(AskCampaign campaign)
    {
        var normalized = ApplyLifecycle(Normalize(campaign)) with { Id = ObjectId.GenerateNewId().ToString(), CreationDateTime = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await campaignRepository.AddItemAsync(normalized);
        return normalized;
    }

    public async Task<AskCampaign?> UpdateAsync(AskCampaign campaign, string channelId)
    {
        var existing = await GetByIdAsync(campaign.Id, channelId);
        if (existing is null) return null;
        var normalized = ApplyLifecycle(Normalize(campaign), existing) with { Id = existing.Id, ChannelId = existing.ChannelId, CreationDateTime = existing.CreationDateTime, UpdatedAt = DateTime.UtcNow };
        await campaignRepository.UpdateItemAsync(normalized);
        return normalized;
    }

    public async Task<bool> DeleteAsync(string id, string channelId)
    {
        var existing = await GetByIdAsync(id, channelId);
        if (existing is null) return false;
        await campaignRepository.DeleteItemAsync(existing.Id);
        return true;
    }

    public async Task<AskSubmissionResult> SubmitAsync(string channelName, string slug, string text, string? idempotencyKey, string? name = null, CancellationToken cancellationToken = default)
    {
        var channel = (await channelRepository.GetItemsAsync(x => x.ChannelName.ToLower() == channelName.Trim().ToLower())).FirstOrDefault();
        var campaign = channel is null ? null : await campaignRepository.GetByChannelAndSlugAsync(channel.ChannelId, IAskService.NormalizeSlug(slug));
        var now = DateTime.UtcNow;
        if (campaign is not null && campaign.Status == AskCampaignStatus.Published && campaign.EndAt <= now)
        {
            campaign = campaign with { Status = AskCampaignStatus.Closed, ClosedAt = now, UpdatedAt = now };
            await campaignRepository.UpdateItemAsync(campaign);
        }
        if (campaign is not { Status: AskCampaignStatus.Published } ||
            (campaign.StartAt is not null && campaign.StartAt > now) ||
            (campaign.EndAt is not null && campaign.EndAt <= now)) campaign = null;
        if (campaign is null) return AskSubmissionResult.NotFound;
        var policy = EffectivePolicy(campaign.Policy);
        var safeText = Regex.Replace(text ?? string.Empty, "<[^>]*>", string.Empty).Trim();
        if (safeText.Length == 0 || safeText.Length > policy.MaxSubmissionLength) return AskSubmissionResult.Invalid;
        var safeName = Regex.Replace(name ?? string.Empty, "<[^>]*>", string.Empty).Trim();
        if (!policy.AllowNamedSubmissions && safeName.Length > 0 || policy.NameRequired && safeName.Length == 0) return AskSubmissionResult.Invalid;
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && await submissionRepository.GetByIdempotencyKeyAsync(campaign.Id, idempotencyKey.Trim()) is not null)
            return AskSubmissionResult.Duplicate;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(safeText)));
        if (await submissionRepository.HasRecentDuplicateAsync(campaign.Id, hash, now.AddMinutes(-policy.DuplicateWindowMinutes))) return AskSubmissionResult.Duplicate;
        var recent = await submissionRepository.GetByCampaignIdAsync(campaign.Id, 5000);
        if (recent.Count(item => item.SubmittedAt >= now.AddHours(-1)) >= policy.RateLimitPerHour) return AskSubmissionResult.RateLimited;
        var submission = new AskSubmission { Id = ObjectId.GenerateNewId().ToString(), CampaignId = campaign.Id, ChannelId = campaign.ChannelId, Text = safeText, Name = safeName, ContentHash = hash, IdempotencyKey = idempotencyKey?.Trim() ?? string.Empty, SubmittedAt = now, RetentionUntil = now.AddDays(policy.RetentionDays) };
        if (policy.ModerationMode == AskModerationMode.AiAssisted)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                var assessment = await moderationProvider.AssessAsync(safeText, timeout.Token);
                if (assessment is not null)
                {
                    submission = submission with
                    {
                        ModerationStatus = assessment.Decision switch
                        {
                            AskModerationDecision.Approve => AskModerationStatus.Approved,
                            AskModerationDecision.Reject => AskModerationStatus.Rejected,
                            _ => AskModerationStatus.Pending
                        },
                        ModerationScore = assessment.Score,
                        ModerationCategories = assessment.Categories,
                        ModerationNote = assessment.Reason
                    };
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Ask AI moderation unavailable; submission remains pending");
            }
        }
        await submissionRepository.AddItemAsync(submission);
        return AskSubmissionResult.Created;
    }

    public async Task<IList<AskSubmission>> GetSubmissionsAsync(string campaignId, string channelId, int limit = 500)
    {
        var campaign = await GetByIdAsync(campaignId, channelId);
        return campaign is null ? [] : await submissionRepository.GetByCampaignIdAsync(campaignId, limit);
    }

    public async Task<AskSubmission?> ModerateAsync(string id, string channelId, AskModerationStatus status, string? note)
    {
        var item = (await submissionRepository.GetItemsAsync(x => x.Id == id && x.ChannelId == channelId)).FirstOrDefault();
        if (item is null) return null;
        var updated = item with { ModerationStatus = status, ModerationNote = note?.Trim() ?? string.Empty };
        await submissionRepository.UpdateItemAsync(updated);
        return updated;
    }

    public async Task<AskSubmission?> RespondAsync(string id, string channelId, string content, string author, AskResponseVisibility visibility)
    {
        var item = (await submissionRepository.GetItemsAsync(x => x.Id == id && x.ChannelId == channelId)).FirstOrDefault();
        if (item is null || string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(author)) return null;
        var updated = item with { ResponseContent = content.Trim(), ResponseAuthor = author.Trim(), ResponseCreatedAt = DateTime.UtcNow, ResponseVisibility = visibility };
        await submissionRepository.UpdateItemAsync(updated);
        return updated;
    }

    public async Task<AskReactionResult> ReactAsync(string channelName, string slug, string submissionId, string fingerprint)
    {
        var campaign = await GetPublicAsync(channelName, slug);
        if (campaign is null || string.IsNullOrWhiteSpace(fingerprint)) return new(false, 0);
        var item = (await submissionRepository.GetItemsAsync(x => x.Id == submissionId && x.CampaignId == campaign.CampaignId && x.ModerationStatus == AskModerationStatus.Approved)).FirstOrDefault();
        if (item is null) return new(false, 0);
        if (await reactionRepository.ExistsAsync(submissionId, fingerprint)) return new(false, item.ReactionCount);
        var recent = await reactionRepository.GetItemsAsync(x => x.Fingerprint == fingerprint && x.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));
        if (recent.Count >= 10) return new(false, item.ReactionCount);
        try
        {
            await reactionRepository.AddItemAsync(new AskReaction { Id = ObjectId.GenerateNewId().ToString(), SubmissionId = submissionId, Fingerprint = fingerprint, CreatedAt = DateTime.UtcNow });
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000) { return new(false, item.ReactionCount); }
        var count = await reactionRepository.CountBySubmissionIdAsync(submissionId);
        await submissionRepository.UpdateItemAsync(item with { ReactionCount = count });
        return new(true, count);
    }

    public async Task<AskAnalytics?> GetAnalyticsAsync(string campaignId, string channelId)
    {
        if (await GetByIdAsync(campaignId, channelId) is null) return null;
        var items = await submissionRepository.GetByCampaignIdAsync(campaignId, 5000);
        return new(items.Count, items.Count(x => x.ModerationStatus == AskModerationStatus.Pending), items.Count(x => x.ModerationStatus == AskModerationStatus.Approved), items.Sum(x => x.ReactionCount), items.Count(x => !string.IsNullOrWhiteSpace(x.ResponseContent)));
    }

    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
        => submissionRepository.DeleteExpiredAsync(DateTime.UtcNow);

    public IReadOnlyList<string> Validate(AskCampaign campaign)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(campaign.ChannelId)) errors.Add("Channel owner is required.");
        if (string.IsNullOrWhiteSpace(campaign.Title) || campaign.Title.Trim().Length > 200) errors.Add("Title is required and must be at most 200 characters.");
        if (IAskService.NormalizeSlug(campaign.Slug).Length is < 1 or > 120) errors.Add("Slug must be between 1 and 120 characters.");
        if (campaign.StartAt is not null && campaign.EndAt is not null && campaign.EndAt <= campaign.StartAt) errors.Add("EndAt must be after StartAt.");
        if (!Enum.IsDefined(campaign.Status)) errors.Add("Status is invalid.");
        return errors;
    }

    private static AskCampaign Normalize(AskCampaign campaign) => campaign with { ChannelId = campaign.ChannelId.Trim(), Title = campaign.Title.Trim(), Description = campaign.Description.Trim(), Slug = IAskService.NormalizeSlug(campaign.Slug) };

    private static AskCampaign ApplyLifecycle(AskCampaign campaign, AskCampaign? existing = null)
    {
        var now = DateTime.UtcNow;
        if (campaign.Status == AskCampaignStatus.Published && existing?.Status != AskCampaignStatus.Published)
            campaign = campaign with { PublishedAt = now, ClosedAt = null };
        else if (campaign.Status == AskCampaignStatus.Closed && existing?.Status != AskCampaignStatus.Closed)
            campaign = campaign with { ClosedAt = now };
        else if (campaign.Status == AskCampaignStatus.Draft)
            campaign = campaign with { PublishedAt = existing?.PublishedAt, ClosedAt = null };
        return campaign;
    }
}