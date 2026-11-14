using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:Feedback")]
public sealed class FeedbackEndpointTests : IClassFixture<ServerApiWebApplicationFactory>
{
    private readonly ServerApiWebApplicationFactory factory;

    public FeedbackEndpointTests(ServerApiWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Intake_requires_csrf_and_accepts_anonymous_submission_without_caching()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var request = NewSubmission();

        var withoutCsrf = await client.PostAsJsonAsync("/api/v1/feedback", request);
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        using var submission = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feedback")
        {
            Content = JsonContent.Create(request)
        };
        submission.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var accepted = await client.SendAsync(submission);

        Assert.Equal(HttpStatusCode.Forbidden, withoutCsrf.StatusCode);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        Assert.Contains("no-store", accepted.Headers.CacheControl?.ToString());
        var receipt = await accepted.Content.ReadFromJsonAsync<FeedbackReceiptContract>();
        Assert.NotNull(receipt);
        Assert.False(receipt.Authenticated);
    }

    [Fact]
    public async Task Authenticated_intake_records_attribution_and_replays_only_the_complete_payload()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await client.GetFromJsonAsync<PublicAccountCsrfContract>("/api/v1/account/csrf");
        var registration = new PublicAccountRegistrationRequest
        {
            Username = $"feedback{Guid.NewGuid():N}"[..16],
            Email = $"{Guid.NewGuid():N}@example.test",
            Password = "A-strong-password-123",
            DisplayName = "Feedback Reporter",
            TermsAccepted = true,
            PrivacyNoticeAcknowledged = true
        };
        using var accountRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts")
        {
            Content = JsonContent.Create(registration)
        };
        accountRequest.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(accountRequest)).StatusCode);

        var request = NewSubmission();
        using var firstRequest = FeedbackRequest(request, csrf.Token);
        var first = await client.SendAsync(firstRequest);
        using var replayRequest = FeedbackRequest(request, csrf.Token);
        var replay = await client.SendAsync(replayRequest);
        var changed = request with
        {
            Context = request.Context with { ComponentKey = "different-component" }
        };
        using var conflictRequest = FeedbackRequest(changed, csrf.Token);
        var conflict = await client.SendAsync(conflictRequest);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var firstReceipt = await first.Content.ReadFromJsonAsync<FeedbackReceiptContract>();
        var replayReceipt = await replay.Content.ReadFromJsonAsync<FeedbackReceiptContract>();
        Assert.True(firstReceipt!.Authenticated);
        Assert.Equal(firstReceipt.Id, replayReceipt!.Id);
    }

    private static HttpRequestMessage FeedbackRequest(SubmitFeedbackRequest request, string csrf)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feedback")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-CSRF-TOKEN", csrf);
        return message;
    }

    private static SubmitFeedbackRequest NewSubmission() => new()
    {
        RequestId = Guid.NewGuid().ToString(),
        Description = "The preview toolbar is difficult to discover.",
        Context = new FeedbackContextRequest
        {
            Application = "Workshop",
            PagePath = "https://example.test/workshop/preview",
            ComponentKey = "preview-toolbar",
            ClientRelease = "test",
            Language = "en",
            Timezone = "UTC",
            ViewportWidth = 1280,
            ViewportHeight = 720,
            Online = true
        }
    };
}

[Trait("Category", "TestGroup:Feedback")]
public sealed class BackOfficeFeedbackEndpointTests : IClassFixture<BackOfficeWebApplicationFactory>
{
    private readonly BackOfficeWebApplicationFactory factory;

    public BackOfficeFeedbackEndpointTests(BackOfficeWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task Feedback_view_can_read_but_manage_is_required_for_status_and_delete()
    {
        var feedback = await AddFeedbackAsync();
        using var viewer = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.FeedbackView);
        using var manager = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.FeedbackManage);

        var detail = await viewer.GetAsync($"/api/v1/feedback/{feedback.Id}");
        using var statusRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/feedback/{feedback.Id}/status")
        {
            Content = JsonContent.Create(new UpdateFeedbackStatusRequest
            {
                Status = nameof(FeedbackStatus.Reviewing),
                ExpectedRevision = feedback.Revision
            })
        };
        var viewerStatus = await viewer.SendAsync(statusRequest);

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("no-store", detail.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.Forbidden, viewerStatus.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/v1/feedback/{feedback.Id}")).StatusCode);
    }

    [Fact]
    public async Task Status_is_revision_checked_and_feedback_is_not_channel_scoped()
    {
        var feedback = await AddFeedbackAsync();
        using var manager = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.FeedbackManage);
        manager.DefaultRequestHeaders.Remove("X-Channel-Id");
        manager.DefaultRequestHeaders.Add("X-Channel-Id", $"unrelated-{Guid.NewGuid():N}");

        var list = await manager.GetFromJsonAsync<FeedbackPageContract>("/api/v1/feedback");
        using var staleRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/feedback/{feedback.Id}/status")
        {
            Content = JsonContent.Create(new UpdateFeedbackStatusRequest
            {
                Status = nameof(FeedbackStatus.Resolved),
                ExpectedRevision = feedback.Revision + 1
            })
        };
        var stale = await manager.SendAsync(staleRequest);
        using var currentRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/feedback/{feedback.Id}/status")
        {
            Content = JsonContent.Create(new UpdateFeedbackStatusRequest
            {
                Status = nameof(FeedbackStatus.Resolved),
                ExpectedRevision = feedback.Revision
            })
        };
        var current = await manager.SendAsync(currentRequest);

        Assert.Contains(list!.Items, item => item.Id == feedback.Id);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Contains("no-store", current.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Expired_feedback_is_hidden_from_detail_status_and_delete()
    {
        var feedback = await AddFeedbackAsync(DateTime.UtcNow.AddMinutes(-1));
        using var manager = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.FeedbackManage);

        var detail = await manager.GetAsync($"/api/v1/feedback/{feedback.Id}");
        using var statusRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/feedback/{feedback.Id}/status")
        {
            Content = JsonContent.Create(new UpdateFeedbackStatusRequest
            {
                Status = nameof(FeedbackStatus.Dismissed),
                ExpectedRevision = feedback.Revision
            })
        };
        var status = await manager.SendAsync(statusRequest);
        var delete = await manager.DeleteAsync($"/api/v1/feedback/{feedback.Id}");

        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    private async Task<Feedback> AddFeedbackAsync(DateTime? expiresAtUtc = null)
    {
        var now = DateTime.UtcNow;
        var feedback = new Feedback
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            RequestId = Guid.NewGuid().ToString(),
            Fingerprint = Guid.NewGuid().ToString("N"),
            Application = "Workshop",
            Description = "Seeded feedback",
            PagePath = "https://example.test/workshop",
            CreationDateTime = now,
            UpdatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc ?? now.AddDays(90)
        };
        await factory.Services.GetRequiredService<IFeedbackRepository>().AddAsync(feedback);
        return feedback;
    }
}
