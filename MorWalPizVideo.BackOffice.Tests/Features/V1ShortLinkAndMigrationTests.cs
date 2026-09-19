using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class V1ShortLinkAndMigrationTests
{
    [Fact]
    public async Task V1_short_links_require_management_permission_and_return_stable_dto_shape()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        using var anonymous = factory.CreateClient();
        anonymous.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/short-links")).StatusCode);

        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.ShortLinksView, PrimaryScenario.ChannelId);
        var response = await client.GetAsync("/api/v1/short-links");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var links = await response.Content.ReadFromJsonAsync<List<V1ShortLinkResponse>>();
        Assert.NotNull(links);
        Assert.Contains(links!, link => link.Code == PrimaryScenario.ChannelShortLinkCode);
    }

    [Fact]
    public async Task Migration_is_idempotent_and_removes_only_migrated_embedded_links()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var matches = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matches.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        var legacy = new ShortLink("MiXeD-Legacy", PrimaryScenario.VideoId, [])
        {
            Id = "legacy-v1",
            LinkType = LinkType.YouTubeVideo,
            ContentId = source.Id,
            ManagementChannelId = PrimaryScenario.ChannelId,
            ClicksCount = 4
        };
        await matches.UpdateItemAsync(source with { ShortLinks = [legacy] });

        var migration = factory.Services.GetRequiredService<IShortLinkMigrationService>();
        var first = await migration.RunAsync(new ShortLinkMigrationOptions(DryRun: false));
        Assert.True(first.Completed);
        Assert.Equal(1, first.CanonicalLinksCreated);
        Assert.Equal(1, first.EmbeddedLinksRemoved);

        var links = factory.Services.GetRequiredService<IShortLinkRepository>();
        var canonical = (await links.GetItemsAsync()).Single(item => item.Id == legacy.Id);
        Assert.Equal("mixed-legacy", canonical.Code);
        Assert.Equal(4, canonical.ClicksCount);
        Assert.Empty((await matches.GetItemAsync(source.Id))!.ShortLinks);

        var rerun = await migration.RunAsync(new ShortLinkMigrationOptions(DryRun: false));
        Assert.True(rerun.Completed);
        Assert.Equal(0, rerun.CanonicalLinksCreated);
    }

    [Fact]
    public async Task Migration_overwrites_code_conflicts_and_preserves_canonical_metadata()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var matches = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var links = factory.Services.GetRequiredService<IShortLinkRepository>();
        var source = (await matches.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        var existing = new ShortLink("CONFLICT", "old-target", [new QueryLink("campaign", "utm_source=existing")])
        {
            Id = "canonical-conflict",
            LinkType = LinkType.CustomUrl,
            ClicksCount = 9,
            ChannelId = "existing-channel",
            CampaignId = "existing-campaign",
            SponsorId = "existing-sponsor",
            ManagementChannelId = "existing-owner"
        };
        await links.AddItemAsync(existing);

        var legacy = new ShortLink(" conflict ", PrimaryScenario.VideoId, [])
        {
            Id = "legacy-conflict",
            LinkType = LinkType.YouTubeVideo,
            ContentId = source.Id,
            ClicksCount = 4
        };
        await matches.UpdateItemAsync(source with { ShortLinks = [legacy] });

        var report = await factory.Services.GetRequiredService<IShortLinkMigrationService>()
            .RunAsync(new ShortLinkMigrationOptions(DryRun: false));

        Assert.True(report.Completed);
        Assert.Equal(1, report.CanonicalLinksReplaced);
        Assert.Single(report.Conflicts);

        var canonical = (await links.GetItemsAsync()).Single(link => link.Code == "conflict");
        Assert.Equal(existing.Id, canonical.Id);
        Assert.Equal(LinkType.YouTubeVideo, canonical.LinkType);
        Assert.Equal(source.Id, canonical.ContentId);
        Assert.Equal(PrimaryScenario.VideoId, canonical.Target);
        Assert.Equal(existing.ClicksCount, canonical.ClicksCount);
        Assert.Equal(existing.QueryString, canonical.QueryString);
        Assert.Equal(existing.ChannelId, canonical.ChannelId);
        Assert.Equal(existing.CampaignId, canonical.CampaignId);
        Assert.Equal(existing.SponsorId, canonical.SponsorId);
        Assert.Equal(existing.ManagementChannelId, canonical.ManagementChannelId);
        Assert.Empty((await matches.GetItemAsync(source.Id))!.ShortLinks);
    }

    [Fact]
    public async Task Migration_dry_run_reports_missing_references_without_mutating_data()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var matches = factory.Services.GetRequiredService<IYouTubeContentRepository>();
        var source = (await matches.GetItemsAsync()).Single(item => item.Id == PrimaryScenario.MatchId);
        var legacy = new ShortLink("missing-ref", "not-in-video-refs", [])
        {
            Id = "legacy-missing-ref",
            LinkType = LinkType.YouTubeVideo,
            ContentId = source.Id
        };
        await matches.UpdateItemAsync(source with { ShortLinks = [legacy] });

        var report = await factory.Services.GetRequiredService<IShortLinkMigrationService>()
            .RunAsync(new ShortLinkMigrationOptions(DryRun: true));

        Assert.False(report.Completed);
        Assert.Single(report.MissingReferences);
        Assert.Contains((await matches.GetItemAsync(source.Id))!.ShortLinks, link => link.Id == legacy.Id);
    }

    [Fact]
    public async Task Migration_audit_reports_duplicate_and_malformed_canonical_codes()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        var links = factory.Services.GetRequiredService<IShortLinkRepository>();
        await links.AddItemAsync(new ShortLink("DUPLICATE", "target-a", []));
        await links.AddItemAsync(new ShortLink("duplicate", "target-b", []));
        await links.AddItemAsync(new ShortLink("", "target-c", []));

        var report = await factory.Services.GetRequiredService<IShortLinkMigrationService>()
            .RunAsync(new ShortLinkMigrationOptions(DryRun: true, Resume: false));

        Assert.False(report.Completed);
        Assert.NotEmpty(report.DuplicateCodes);
        Assert.NotEmpty(report.MalformedCodes);
        Assert.False(report.ArchivalReady);
    }

    [Fact]
    public async Task Migration_validation_does_not_claim_index_evidence_in_mock_mode()
    {
        await using var factory = new BackOfficeWebApplicationFactory();
        using var client = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.BackofficeManageAll);

        var response = await client.GetAsync("/api/v1/maintenance/short-links/validate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var validation = await response.Content.ReadFromJsonAsync<ShortLinkMigrationValidationResponse>();
        Assert.NotNull(validation);
        Assert.Equal("unavailable", validation!.IndexStatus);
    }
}