using Hangfire;
using MongoDB.Bson;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Services;

public interface IInsightCommentAnalysisService
{
    Task<AnalyzeInsightCommentsResponse> EnqueueOrRunAsync(InsightTopic topic, AnalyzeInsightCommentsRequest request, string selectedChannelId);
    Task<InsightCommentAnalysisRun?> GetRunAsync(string id, string selectedChannelId);
    Task<InsightCommentAnalysisRun?> RescheduleAsync(string id, string selectedChannelId);
    Task ExecuteAsync(string id);
}

public interface IInsightCommentAnalysisScheduler
{
    bool TryEnqueue(string runId);
}

public sealed class InsightCommentAnalysisScheduler(IBackgroundJobClient? backgroundJobClient) : IInsightCommentAnalysisScheduler
{
    public bool TryEnqueue(string runId)
    {
        if (backgroundJobClient == null)
            return false;

        try
        {
            backgroundJobClient.Enqueue<InsightCommentAnalysisJob>(job => job.ExecuteAsync(runId));
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class InsightCommentAnalysisJob(IInsightCommentAnalysisService analysisService)
{
    public Task ExecuteAsync(string runId) => analysisService.ExecuteAsync(runId);
}

public sealed class InsightCommentAnalysisService(
    IInsightCommentAnalysisRunRepository runRepository,
    IInsightsService insightsService,
    IInsightIngestionService ingestionService,
    IInsightCommentAnalysisScheduler scheduler,
    ILogger<InsightCommentAnalysisService> logger) : IInsightCommentAnalysisService
{
    public async Task<AnalyzeInsightCommentsResponse> EnqueueOrRunAsync(
        InsightTopic topic,
        AnalyzeInsightCommentsRequest request,
        string selectedChannelId)
    {
        var normalizedRequest = Normalize(request, selectedChannelId);
        var activeRun = (await runRepository.GetItemsAsync(run =>
            run.TopicId == topic.Id &&
            run.ChannelId == selectedChannelId &&
            (run.Status == InsightCommentAnalysisRunStatus.Pending || run.Status == InsightCommentAnalysisRunStatus.Running) &&
            run.SourceType == normalizedRequest.SourceType &&
            run.SourceChannelId == normalizedRequest.SourceChannelId &&
            run.VideoId == normalizedRequest.VideoId &&
            run.CommentsNumber == normalizedRequest.CommentsNumber &&
            run.ExcludeUploaderComments == normalizedRequest.ExcludeUploaderComments &&
            run.SourceKind == normalizedRequest.SourceKind)).FirstOrDefault();

        if (activeRun != null)
            return ToResponse(activeRun, queued: activeRun.Status == InsightCommentAnalysisRunStatus.Pending);

        var run = normalizedRequest with
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TopicId = topic.Id,
            ChannelId = selectedChannelId,
            CreationMode = topic.CreationMode,
            Status = InsightCommentAnalysisRunStatus.Pending,
            QueuedAtUtc = DateTime.UtcNow
        };
        await runRepository.AddItemAsync(run);

        if (scheduler.TryEnqueue(run.Id))
        {
            logger.LogInformation("Comment analysis {RunId} queued for topic {TopicId} and channel {ChannelId}", run.Id, run.TopicId, run.ChannelId);
            return ToResponse(run, queued: true);
        }

        logger.LogInformation("Comment analysis {RunId} uses synchronous fallback because Hangfire is unavailable", run.Id);
        await ExecuteAsync(run.Id);
        var completed = await runRepository.GetItemAsync(run.Id);
        return ToResponse(completed, queued: false);
    }

    public async Task<InsightCommentAnalysisRun?> GetRunAsync(string id, string selectedChannelId) =>
        (await runRepository.GetItemsAsync(run => run.Id == id && run.ChannelId == selectedChannelId)).FirstOrDefault();

    public async Task<InsightCommentAnalysisRun?> RescheduleAsync(string id, string selectedChannelId)
    {
        var existing = await GetRunAsync(id, selectedChannelId);
        if (existing == null || existing.Status != InsightCommentAnalysisRunStatus.Rejected)
            return null;

        var rescheduled = existing with
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Status = InsightCommentAnalysisRunStatus.Pending,
            QueuedAtUtc = DateTime.UtcNow,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            RejectionReason = string.Empty,
            VideosProcessed = 0,
            CommentsAnalyzed = 0,
            CreatedNewsItemCount = 0
        };
        await runRepository.AddItemAsync(rescheduled);

        if (scheduler.TryEnqueue(rescheduled.Id))
            return rescheduled;

        await ExecuteAsync(rescheduled.Id);
        return await runRepository.GetItemAsync(rescheduled.Id);
    }

    public async Task ExecuteAsync(string id)
    {
        var current = await runRepository.GetItemAsync(id);
        if (current == null || current.Status is InsightCommentAnalysisRunStatus.Completed or InsightCommentAnalysisRunStatus.Rejected)
            return;

        var running = current with { Status = InsightCommentAnalysisRunStatus.Running, StartedAtUtc = DateTime.UtcNow };
        await runRepository.UpdateItemAsync(running);

        try
        {
            var topic = await insightsService.GetTopicByIdAsync(running.TopicId, running.ChannelId);
            if (topic == null)
                throw new InvalidOperationException("The insight topic is no longer available.");

            var result = await ingestionService.AnalyzeCommentsAsync(topic, ToRequest(running), running.ChannelId);
            var completed = running with
            {
                Status = InsightCommentAnalysisRunStatus.Completed,
                CompletedAtUtc = DateTime.UtcNow,
                VideosProcessed = result.VideosProcessed,
                CommentsAnalyzed = result.CommentsAnalyzed,
                CreatedNewsItemCount = result.CreatedNewsItemIds.Count
            };
            await runRepository.UpdateItemAsync(completed);
        }
        catch (Exception exception)
        {
            var rejected = running with
            {
                Status = InsightCommentAnalysisRunStatus.Rejected,
                CompletedAtUtc = DateTime.UtcNow,
                RejectionReason = "Comment analysis could not be completed."
            };
            await runRepository.UpdateItemAsync(rejected);
            logger.LogError(exception, "Comment analysis {RunId} rejected for topic {TopicId} and channel {ChannelId}", id, running.TopicId, running.ChannelId);
        }
    }

    private static InsightCommentAnalysisRun Normalize(AnalyzeInsightCommentsRequest request, string selectedChannelId) => new()
    {
        SourceType = request.SourceType,
        SourceKind = request.SourceKind ?? InsightSourceKind.ShortContent,
        SourceChannelId = string.IsNullOrWhiteSpace(request.ChannelId) ? selectedChannelId : request.ChannelId.Trim(),
        VideoId = request.SourceType == InsightCommentSourceType.StoredChannel ? string.Empty : request.VideoId.Trim(),
        CommentsNumber = request.CommentsNumber,
        ExcludeUploaderComments = request.ExcludeUploaderComments
    };

    private static AnalyzeInsightCommentsRequest ToRequest(InsightCommentAnalysisRun run) => new()
    {
        SourceType = run.SourceType,
        SourceKind = run.SourceKind,
        ChannelId = run.SourceChannelId,
        VideoId = run.VideoId,
        CommentsNumber = run.CommentsNumber,
        ExcludeUploaderComments = run.ExcludeUploaderComments
    };

    private static AnalyzeInsightCommentsResponse ToResponse(InsightCommentAnalysisRun run, bool queued) => new()
    {
        RunId = run.Id,
        Status = run.Status,
        Queued = queued,
        VideosProcessed = run.VideosProcessed,
        CommentsAnalyzed = run.CommentsAnalyzed,
        CreatedNewsItemCount = run.CreatedNewsItemCount,
        RejectionReason = run.RejectionReason
    };
}
