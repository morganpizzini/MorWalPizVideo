using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Server.Services.Interfaces;
using FeedbackModel = MorWalPizVideo.Models.Models.Feedback;
using FeedbackStatus = MorWalPizVideo.Models.Models.FeedbackStatus;

namespace MorWalPizVideo.Domain.Feedback;

public sealed class FeedbackService(
    IFeedbackRepository repository,
    TimeProvider timeProvider,
    ILogger<FeedbackService> logger)
{
    public async Task<(FeedbackModel Feedback, bool Existing)> SubmitAsync(
        FeedbackSubmissionCommand request,
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken = default)
    {
        var description = request.Description.Trim();
        if (description.Length is < 1 or > 4000) throw new FeedbackValidationException("Description must contain 1 to 4000 characters.");
        if (!Guid.TryParse(request.RequestId, out var requestGuid)) throw new FeedbackValidationException("RequestId must be a UUID.");
        if (!string.Equals(request.Application, "Workshop", StringComparison.OrdinalIgnoreCase))
            throw new FeedbackValidationException("Application is not supported.");
        var pagePath = request.PagePath.Trim();
        if (!Uri.TryCreate(pagePath, UriKind.Absolute, out var pageUri) ||
            (pageUri.Scheme != Uri.UriSchemeHttps && pageUri.Scheme != Uri.UriSchemeHttp))
            throw new FeedbackValidationException("PagePath must be an absolute HTTP(S) URL.");

        var accountId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = principal?.Identity?.IsAuthenticated == true ? principal.FindFirst(ClaimTypes.Name)?.Value : null;
        ValidateMetadata(request);
        var normalizedRequestId = requestGuid.ToString("D");
        var normalizedPagePath = $"{pageUri.Scheme}://{pageUri.Authority}{pageUri.AbsolutePath}";
        var values = new[] { normalizedRequestId, "Workshop", normalizedPagePath, description,
            Normalize(request.ComponentKey), Normalize(request.ClientRelease), Normalize(request.UserAgent),
            Normalize(request.Language), Normalize(request.Timezone), request.ViewportWidth?.ToString() ?? "",
            request.ViewportHeight?.ToString() ?? "", request.Online?.ToString() ?? "", accountId ?? "" };
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\u001f", values))));
        var existing = await repository.FindByRequestIdAsync(normalizedRequestId, cancellationToken);
        if (existing is not null)
        {
            if (string.Equals(ExistingFingerprint(existing), fingerprint, StringComparison.Ordinal))
                return (existing, true);
            logger.LogWarning("Feedback replay conflict for request {RequestId}", normalizedRequestId);
            throw new FeedbackConflictException();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var feedback = new FeedbackModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            RequestId = normalizedRequestId,
            Fingerprint = fingerprint,
            Application = "Workshop",
            Description = description,
            PagePath = normalizedPagePath,
            ComponentKey = Normalize(request.ComponentKey),
            ClientRelease = Normalize(request.ClientRelease),
            UserAgent = Normalize(request.UserAgent),
            Language = Normalize(request.Language),
            Timezone = Normalize(request.Timezone),
            ViewportWidth = request.ViewportWidth,
            ViewportHeight = request.ViewportHeight,
            Online = request.Online,
            ReporterAccountId = accountId,
            ReporterUsername = username,
            CreationDateTime = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(90)
        };
        try { logger.LogInformation("Feedback submission accepted for request {RequestId}", normalizedRequestId); return (await repository.AddAsync(feedback, cancellationToken), false); }
        catch (FeedbackDuplicateRequestException)
        {
            existing = await repository.FindByRequestIdAsync(normalizedRequestId, cancellationToken);
            if (existing is not null && ExistingFingerprint(existing) == fingerprint) return (existing, true);
            throw new FeedbackConflictException();
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            existing = await repository.FindByRequestIdAsync(normalizedRequestId, cancellationToken);
            if (existing is not null && ExistingFingerprint(existing) == fingerprint)
                return (existing, true);
            throw new FeedbackConflictException();
        }
    }

    public Task<FeedbackModel?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        repository.GetAsync(id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    public Task<IReadOnlyList<FeedbackModel>> ListAsync(FeedbackStatus? status, string? application, int page, int pageSize, CancellationToken cancellationToken = default) =>
        repository.ListAsync(status, application, page, pageSize, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    public Task<long> CountAsync(FeedbackStatus? status, string? application, CancellationToken cancellationToken = default) =>
        repository.CountAsync(status, application, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    public Task<bool> UpdateStatusAsync(string id, FeedbackStatus status, long revision, ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return repository.UpdateStatusAsync(id, status, revision, now,
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            principal.Identity?.IsAuthenticated == true ? principal.FindFirst(ClaimTypes.Name)?.Value : null, cancellationToken);
    }
    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string ExistingFingerprint(FeedbackModel item) =>
        string.IsNullOrEmpty(item.Fingerprint) ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\u001f",
            item.RequestId, item.Application, item.PagePath, item.Description, item.ComponentKey ?? "", item.ClientRelease ?? "",
            item.UserAgent ?? "", item.Language ?? "", item.Timezone ?? "", item.ViewportWidth?.ToString() ?? "",
            item.ViewportHeight?.ToString() ?? "", item.Online?.ToString() ?? "", item.ReporterAccountId ?? "")))) : item.Fingerprint;
    private static void ValidateMetadata(FeedbackSubmissionCommand request)
    {
        if (request.Application.Length > 20 || request.PagePath.Length > 2048 ||
            request.ComponentKey?.Length is > 120 || request.ClientRelease?.Length is > 100 ||
            request.UserAgent?.Length is > 512 || request.Language?.Length is > 35 || request.Timezone?.Length is > 100)
            throw new FeedbackValidationException("Feedback metadata exceeds its allowed size.");
    }
}

public sealed class FeedbackValidationException(string message) : Exception(message);
public sealed class FeedbackConflictException : Exception;
public sealed class FeedbackDuplicateRequestException : Exception;
public sealed record FeedbackSubmissionCommand(
    string RequestId,
    string Description,
    string Application,
    string PagePath,
    string? ComponentKey,
    string? ClientRelease,
    string? UserAgent,
    string? Language,
    string? Timezone,
    int? ViewportWidth,
    int? ViewportHeight,
    bool? Online);
