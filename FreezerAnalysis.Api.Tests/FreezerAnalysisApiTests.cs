using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using FreezerAnalysis.Api.Services;
using MorWalPiz.Contracts.Contracts.FreezerAnalysis;
using MorWalPizVideo.Domain.FreezerAnalysis;
using MorWalPizVideo.Models.FreezerAnalysis;
using MorWalPizVideo.Server.Services.Interfaces;

namespace FreezerAnalysis.Api.Tests;

public sealed class FreezerAnalysisApiTests
{
    [Fact]
    public void Mongo_registration_uses_a_distinct_feature_database_without_connecting_to_server()
    {
        var settings = new Dictionary<string, string?>
        {
            ["MorWalPizDatabase:ConnectionString"] = "mongodb://127.0.0.1:27017",
            ["MorWalPizDatabase:DatabaseName"] = "platform_accounts_test",
            ["FreezerAnalysis:ApplicationDatabaseName"] = "freezer_analysis_test"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection();

        FreezerAnalysisMongoRegistration.AddRepositories(services, configuration);
        using var provider = services.BuildServiceProvider();
        var applicationDatabase = provider.GetRequiredService<FreezerAnalysisDatabase>();
        var accountRepository = provider.GetRequiredService<IPublicAccountRepository>();
        var analysisRepository = provider.GetRequiredService<IFreezerAnalysisRepository>();

        Assert.Equal("freezer_analysis_test", applicationDatabase.Database.DatabaseNamespace.DatabaseName);
        Assert.IsType<PublicAccountRepository>(accountRepository);
        Assert.IsType<FreezerAnalysisMongoRepository>(analysisRepository);
    }

    [Fact]
    public void Mongo_registration_rejects_shared_platform_and_feature_database_names()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MorWalPizDatabase:ConnectionString"] = "mongodb://127.0.0.1:27017",
                ["MorWalPizDatabase:DatabaseName"] = "shared_database",
                ["FreezerAnalysis:ApplicationDatabaseName"] = "SHARED_DATABASE"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            FreezerAnalysisMongoRegistration.AddRepositories(new ServiceCollection(), configuration));

        Assert.Contains("distinct", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_verification_report_deserializes_with_defaults_for_additive_metrics()
    {
        var legacyDocument = new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "accountId", "legacy-account" },
            { "snapshotId", "legacy-snapshot" },
            { "finalRevisionId", "legacy-revision" },
            { "resultHash", "legacy-hash" },
            { "stageCount", 1 },
            { "missingStageCount", 0 },
            { "disqualifiedStageCount", 0 },
            { "zeroedStageCount", 0 },
            { "meanRelativeError", 0.05 },
            { "errorSampleStandardDeviation", BsonNull.Value },
            { "intervalCoverageCount", 1 },
            { "observedBeta", 1.0 },
            { "stages", new BsonArray() },
            { "diagnostics", new BsonArray() },
            { "createdAtUtc", DateTime.UtcNow }
        };

        var report = BsonSerializer.Deserialize<FreezerVerificationReport>(legacyDocument);

        Assert.Equal("legacy-account", report.AccountId);
        Assert.Equal("legacy-snapshot", report.SnapshotId);
        Assert.Equal(string.Empty, report.StageGroupingConvention);
        Assert.Empty(report.Groups);
        Assert.Equal(0, report.InformantDriftComparisonCount);
        Assert.Equal(0, report.InformantDriftMissingStageCount);
        Assert.Equal(0, report.InformantDriftDisqualifiedStageCount);
        Assert.Equal(0, report.InformantDriftZeroedStageCount);
        Assert.Equal(0, report.InformantDriftUnverifiableStageCount);
        Assert.Null(report.InformantDriftMeanRelativeError);
        Assert.Null(report.InformantDriftErrorSampleStandardDeviation);
    }

    [Fact]
    public async Task Supported_formats_reports_pdf_as_diagnostic_only()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/supported-formats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var pdf = document.RootElement.GetProperty("pdf");
        Assert.Equal("paired-score-preview-no-publication", pdf.GetProperty("status").GetString());
        Assert.True(pdf.GetProperty("pairPreview").GetBoolean());
        Assert.True(pdf.GetProperty("structuralCounts").GetBoolean());
        Assert.True(pdf.GetProperty("numericScorePreview").GetBoolean());
        Assert.False(pdf.GetProperty("scorePublication").GetBoolean());
    }

    [Fact]
    public async Task Development_session_can_read_and_update_its_private_profile_with_csrf_protection()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = await CreateAuthenticatedClientAsync(factory);
        var identityResponse = await client.PostAsJsonAsync(
            "/api/v1/identities",
            new FreezerIdentityCreateRequest { DisplayName = "Runner One" });
        Assert.Equal(HttpStatusCode.Created, identityResponse.StatusCode);
        var identity = await identityResponse.Content.ReadFromJsonAsync<FreezerIdentityResponse>();
        Assert.NotNull(identity);

        var update = await client.PutAsJsonAsync("/api/v1/profile", new FreezerProfileRequest
        {
            IdentityId = identity.Id,
            Division = "prod",
            PowerFactor = "Minor",
            Performance = 0.94
        });

        Assert.True(update.StatusCode == HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
        var profile = await update.Content.ReadFromJsonAsync<FreezerProfileResponse>();
        Assert.NotNull(profile);
        Assert.Equal("development-account", profile.AccountId);
        Assert.Equal("PROD", profile.Division);
        Assert.Equal(0.94, profile.Performance);
    }

    [Fact]
    public async Task Identity_review_requires_explicit_account_owned_mapping_and_revision()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = await CreateAuthenticatedClientAsync(factory);

        var overLimitAliases = await client.PostAsJsonAsync(
            "/api/v1/identities",
            new FreezerIdentityCreateRequest
            {
                DisplayName = "Runner One",
                Aliases = Enumerable.Range(1, 21).Select(index => $"Runner {index}").ToArray()
            });
        Assert.Equal(HttpStatusCode.BadRequest, overLimitAliases.StatusCode);

        var createIdentity = await client.PostAsJsonAsync(
            "/api/v1/identities",
            new FreezerIdentityCreateRequest
            {
                DisplayName = "Runner One",
                Aliases = ["Runner Uno"]
            });
        Assert.Equal(HttpStatusCode.Created, createIdentity.StatusCode);
        var identity = await createIdentity.Content.ReadFromJsonAsync<FreezerIdentityResponse>();
        Assert.NotNull(identity);

        using var import = new MultipartFormDataContent();
        import.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(SingleCompetitorCsv)), "File", "scores.csv");
        import.Add(new StringContent("Identity test"), "MatchName");
        import.Add(new StringContent("2026-10-29"), "MatchDate");
        import.Add(new StringContent("0"), "expectedRevision");
        var publish = await client.PostAsync("/api/v1/imports/scores/publish", import);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);

        var matches = await client.GetFromJsonAsync<FreezerMatchResponse[]>("/api/v1/matches");
        var match = Assert.Single(matches!);
        var review = await client.GetFromJsonAsync<FreezerIdentityReviewEntryResponse[]>(
            $"/api/v1/matches/{match.Id}/identities/review");
        var competitor = Assert.Single(review!);
        Assert.Null(competitor.IdentityId);
        var candidate = Assert.Single(competitor.Candidates);
        Assert.Equal(identity.Id, candidate.Id);

        var resolution = await client.PutAsJsonAsync(
            $"/api/v1/matches/{match.Id}/identities/{competitor.ResultId}",
            new FreezerIdentityResolutionRequest
            {
                IdentityId = identity.Id,
                ExpectedRevision = 0
            });
        Assert.Equal(HttpStatusCode.OK, resolution.StatusCode);
        var mapped = await resolution.Content.ReadFromJsonAsync<FreezerIdentityResolutionResponse>();
        Assert.NotNull(mapped);
        Assert.Equal(identity.Id, mapped.IdentityId);
        Assert.Equal(1, mapped.Revision);

        var staleResolution = await client.PutAsJsonAsync(
            $"/api/v1/matches/{match.Id}/identities/{competitor.ResultId}",
            new FreezerIdentityResolutionRequest
            {
                IdentityId = identity.Id,
                ExpectedRevision = 0
            });
        Assert.Equal(HttpStatusCode.Conflict, staleResolution.StatusCode);

        var updatedReview = await client.GetFromJsonAsync<FreezerIdentityReviewEntryResponse[]>(
            $"/api/v1/matches/{match.Id}/identities/review");
        var updatedCompetitor = Assert.Single(updatedReview!);
        Assert.Equal(identity.Id, updatedCompetitor.IdentityId);
        Assert.Equal(1, updatedCompetitor.ResolutionRevision);

        var remove = await client.DeleteAsync(
            $"/api/v1/matches/{match.Id}/identities/{competitor.ResultId}?expectedRevision=1");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
    }

    [Fact]
    public async Task Identity_records_are_not_visible_across_account_boundaries()
    {
        var repository = new InMemoryFreezerAnalysisRepository();
        await repository.CreateIdentityAsync(new FreezerShooterIdentity
        {
            Id = "identity-owner",
            CreatedByAccountId = "account-one",
            NormalizedName = "RUNNERONE",
            Aliases = ["Runner One"],
            NormalizedAliases = ["RUNNERUNO"]
        }, CancellationToken.None);

        Assert.Null(await repository.GetIdentityAsync("account-two", "identity-owner", CancellationToken.None));
        Assert.Empty(await repository.FindIdentityCandidatesAsync(
            "account-two",
            ["RUNNERONE", "RUNNERUNO"],
            CancellationToken.None));
    }

    [Fact]
    public async Task Profile_rejects_unknown_identity()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = await CreateAuthenticatedClientAsync(factory);

        var update = await client.PutAsJsonAsync("/api/v1/profile", new FreezerProfileRequest
        {
            IdentityId = "not-owned",
            Division = "prod"
        });

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task Estimate_composes_only_prior_history_with_explicit_availability_and_identity_mappings()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = await CreateAuthenticatedClientAsync(factory);
        var historicalDate = new DateOnly(2026, 10, 28);
        var baseHits = new[] { 1.0, 1.3, 1.6, 2.1, 2.4, 2.9 };
        var historicalCsv = BuildScoreCsv(
            "history-estimate-test",
            historicalDate,
            [
                ("self", "Runner One", baseHits),
                ("rival-a", "Rival Two", baseHits.Select((value, index) => value * new[] { 1.01, 1.04, 0.98, 1.03, 0.97, 1.02 }[index]).ToArray()),
                ("rival-b", "Rival Three", baseHits.Select((value, index) => value * new[] { 0.99, 0.96, 1.02, 0.97, 1.03, 0.98 }[index]).ToArray()),
                ("rival-c", "Rival Four", baseHits.Select((value, index) => value * new[] { 1.02, 0.98, 1.04, 0.96, 1.03, 0.97 }[index]).ToArray()),
                ("rival-d", "Rival Five", baseHits.Select((value, index) => value * new[] { 0.98, 1.02, 0.96, 1.04, 0.97, 1.03 }[index]).ToArray())
            ]);
        var historicalMatch = await PublishMatchAsync(
            client,
            "Historical synthetic match",
            historicalDate,
            historicalCsv);
        var identityIds = await CreateAndResolveIdentitiesAsync(client, historicalMatch.Id);

        var targetDate = historicalDate.AddDays(1);
        var targetCsv = BuildScoreCsv(
            "target-estimate-test",
            targetDate,
            [("target-self", "Runner One", [1.1, 1.5, 1.9])]);
        var targetMatch = await PublishMatchAsync(client, "Target synthetic match", targetDate, targetCsv);
        var draft = await client.GetFromJsonAsync<FreezerDraftResponse>(
            $"/api/v1/matches/{targetMatch.Id}/draft");
        Assert.NotNull(draft);

        var saveDraft = await client.PutAsJsonAsync(
            $"/api/v1/matches/{targetMatch.Id}/draft",
            new FreezerDraftRequest
            {
                ExpectedRevision = draft.Revision,
                IdentityId = identityIds["self"],
                Division = "PRODUCTION",
                Performance = 0.935
            });
        Assert.Equal(HttpStatusCode.OK, saveDraft.StatusCode);
        var savedDraft = await saveDraft.Content.ReadFromJsonAsync<FreezerDraftResponse>();
        Assert.NotNull(savedDraft);

        var estimate = await client.GetFromJsonAsync<FreezerEstimateResponse>(
            $"/api/v1/matches/{targetMatch.Id}/estimate");

        Assert.NotNull(estimate);
        Assert.True(estimate.IsAvailable, string.Join(Environment.NewLine, estimate.Diagnostics));
        Assert.Equal(3, estimate.Stages.Count);
        Assert.Equal(
            estimate.Stages.OrderBy(stage => stage.StageNumber).Select(stage => stage.StageNumber),
            estimate.Stages.Select(stage => stage.StageNumber));
        Assert.All(estimate.Stages, stage =>
        {
            Assert.NotNull(stage.Center);
            Assert.True(stage.Center > 0);
            Assert.NotNull(stage.Lower);
            Assert.NotNull(stage.Upper);
            Assert.True(stage.Lower <= stage.Center);
            Assert.True(stage.Center <= stage.Upper);
        });

        var replay = await client.GetFromJsonAsync<FreezerEstimateResponse>(
            $"/api/v1/matches/{targetMatch.Id}/estimate");
        Assert.NotNull(replay);
        Assert.Equal(
            estimate.Stages.Select(stage => (stage.StageId, stage.StageNumber, stage.Center, stage.Lower, stage.Upper)),
            replay.Stages.Select(stage => (stage.StageId, stage.StageNumber, stage.Center, stage.Lower, stage.Upper)));

        var freeze = await client.PostAsJsonAsync(
            $"/api/v1/matches/{targetMatch.Id}/freeze",
            new FreezerFreezeRequest { ExpectedRevision = savedDraft.Revision });
        Assert.Equal(HttpStatusCode.OK, freeze.StatusCode);
        var snapshot = await freeze.Content.ReadFromJsonAsync<FreezerSnapshotResponse>();
        Assert.NotNull(snapshot);
        var firstReplayResponse = await client.GetAsync($"/api/v1/snapshots/{snapshot.Id}/replay");
        Assert.Equal(HttpStatusCode.OK, firstReplayResponse.StatusCode);
        var firstReplay = await firstReplayResponse.Content.ReadFromJsonAsync<FreezerSnapshotReplayResponse>();
        Assert.NotNull(firstReplay);
        Assert.True(firstReplay.IsReplayAvailable, string.Join(Environment.NewLine, firstReplay.Diagnostics));
        Assert.True(firstReplay.MatchesFrozenForecast, string.Join(Environment.NewLine, firstReplay.Diagnostics));
        Assert.All(firstReplay.Stages, stage => Assert.True(stage.MatchesFrozenForecast));

        var fieldEventRequest = new FreezerFieldEventRequest
        {
            ClientOperationId = "offline-reconciliation-operation",
            Kind = "reconnaissance",
            Content = "Synthetic field note",
            ObservedAtLocal = new DateTime(2026, 10, 29, 12, 0, 0),
            ObservedOffsetMinutes = 120
        };
        var appendedEventResponse = await client.PostAsJsonAsync(
            $"/api/v1/snapshots/{snapshot.Id}/field-events",
            fieldEventRequest);
        Assert.Equal(HttpStatusCode.OK, appendedEventResponse.StatusCode);
        var appendedEvent = await appendedEventResponse.Content.ReadFromJsonAsync<FreezerFieldEventResponse>();
        Assert.NotNull(appendedEvent);
        Assert.Equal(1, appendedEvent.Sequence);
        var retriedEventResponse = await client.PostAsJsonAsync(
            $"/api/v1/snapshots/{snapshot.Id}/field-events",
            fieldEventRequest);
        var retriedEvent = await retriedEventResponse.Content.ReadFromJsonAsync<FreezerFieldEventResponse>();
        Assert.Equal(appendedEvent.Id, retriedEvent!.Id);
        Assert.Equal(appendedEvent.Sequence, retriedEvent.Sequence);
        var persistedEvents = await client.GetFromJsonAsync<FreezerFieldEventResponse[]>(
            $"/api/v1/snapshots/{snapshot.Id}/field-events");
        Assert.Single(persistedEvents!);
        var unknownSnapshotEvent = await client.PostAsJsonAsync(
            "/api/v1/snapshots/not-owned/field-events",
            fieldEventRequest with { ClientOperationId = "unknown-snapshot-operation" });
        Assert.Equal(HttpStatusCode.Conflict, unknownSnapshotEvent.StatusCode);

        var repository = factory.Services.GetRequiredService<IFreezerAnalysisRepository>();
        var frozenRecord = await repository.GetSnapshotAsync(
            "development-account",
            snapshot.Id,
            CancellationToken.None);
        Assert.NotNull(frozenRecord);
        var storage = factory.Services.GetRequiredService<IFreezerPrivateObjectStorage>();
        var compressedPackage = await storage.GetAsync(
            new PrivateObjectReference(
                frozenRecord.InputPackageObjectKey,
                frozenRecord.InputPackageHash,
                frozenRecord.InputPackageLength,
                "application/gzip"),
            CancellationToken.None);
        var legacyPackage = await storage.GetAsync(
            new PrivateObjectReference(
                frozenRecord.InputPackageObjectKey,
                frozenRecord.InputPackageHash,
                0,
                "application/gzip"),
            CancellationToken.None);
        Assert.Equal(compressedPackage, legacyPackage);
        using var packageStream = new MemoryStream(compressedPackage);
        using var gzip = new GZipStream(packageStream, CompressionMode.Decompress);
        using var package = await JsonDocument.ParseAsync(gzip);
        var root = package.RootElement;
        var historicalInput = Assert.Single(root.GetProperty("historicalInputs").EnumerateArray());
        Assert.Equal(historicalMatch.Id, historicalInput.GetProperty("MatchId").GetString());
        Assert.True(historicalInput.GetProperty("IsIncluded").GetBoolean());
        Assert.Equal(5, historicalInput.GetProperty("IdentityByResultId").EnumerateObject().Count());
        Assert.Equal(30, root.GetProperty("historicalStageScores").GetArrayLength());
        Assert.DoesNotContain("Runner One", root.GetRawText(), StringComparison.Ordinal);

        var finalRevisionMatch = await PublishMatchAsync(
            client,
            "Target synthetic match",
            targetDate,
            BuildScoreCsv(
                "target-estimate-test",
                targetDate,
                [
                    ("target-self", "Runner One", [1.2, 1.6, 2.1, 2.4]),
                    ("rival-a", "Rival Two", [1.25, 1.55, 2.05, 2.3]),
                    ("rival-b", "Rival Three", [1.15, 1.45, 1.95, 2.2]),
                    ("rival-c", "Rival Four", [1.3, 1.5, 2.1, 2.4]),
                    ("rival-d", "Rival Five", [1.1, 1.4, 1.9, 2.2])
                ],
                disqualifiedCompetitorIds: new HashSet<string>(StringComparer.Ordinal) { "rival-c" },
                zeroedCompetitorIds: new HashSet<string>(StringComparer.Ordinal) { "rival-d" },
                missingStageResults: new HashSet<(string CompetitorId, int StageNumber)>
                {
                    ("target-self", 4)
                },
                minimumRoundsByStage: [12, 13, 25, 26]),
            expectedRevision: 1);
        Assert.Equal(targetMatch.Id, finalRevisionMatch.Id);
        var finalReview = await client.GetFromJsonAsync<FreezerIdentityReviewEntryResponse[]>(
            $"/api/v1/matches/{targetMatch.Id}/identities/review");
        foreach (var (competitorId, identityKey) in new[]
                 {
                     ("target-self", "self"),
                     ("rival-a", "rival-a"),
                     ("rival-c", "rival-c"),
                     ("rival-d", "rival-d")
                 })
        {
            var finalCompetitor = Assert.Single(finalReview!, entry => entry.CompetitorId == competitorId);
            var finalResolution = await client.PutAsJsonAsync(
                $"/api/v1/matches/{targetMatch.Id}/identities/{finalCompetitor.ResultId}",
                new FreezerIdentityResolutionRequest
                {
                    IdentityId = identityIds[identityKey],
                    ExpectedRevision = finalCompetitor.ResolutionRevision
                });
            Assert.Equal(HttpStatusCode.OK, finalResolution.StatusCode);
        }

        var replayAfterPublication = await client.GetFromJsonAsync<FreezerSnapshotReplayResponse>(
            $"/api/v1/snapshots/{snapshot.Id}/replay");
        Assert.NotNull(replayAfterPublication);
        Assert.Equal(firstReplay.Stages, replayAfterPublication.Stages);
        Assert.Equal(firstReplay.MatchesFrozenForecast, replayAfterPublication.MatchesFrozenForecast);

        var currentMatch = await repository.GetMatchAsync(targetMatch.Id, CancellationToken.None);
        Assert.NotNull(currentMatch?.ActiveRevisionId);
        var currentMatchSummary = await client.GetFromJsonAsync<FreezerMatchResponse>(
            $"/api/v1/matches/{targetMatch.Id}");
        Assert.NotNull(currentMatchSummary);
        Assert.Equal(currentMatch.ActiveRevisionId, currentMatchSummary.ActiveRevisionId);
        Assert.True(currentMatchSummary.CanVerify);
        var verifyRequest = new FreezerVerificationRequest { FinalRevisionId = currentMatch.ActiveRevisionId };
        var verification = await client.PostAsJsonAsync(
            $"/api/v1/snapshots/{snapshot.Id}/verification",
            verifyRequest);
        Assert.Equal(HttpStatusCode.OK, verification.StatusCode);
        var verified = await verification.Content.ReadFromJsonAsync<FreezerVerificationResponse>();
        Assert.NotNull(verified);
        Assert.Equal(3, verified.VerifiableStageCount);
        Assert.Equal(1, verified.MissingStageCount);
        Assert.Equal(4, verified.Stages.Count);
        Assert.Equal(FreezerAnalysisEngine.StageGroupingConvention, verified.StageGroupingConvention);
        Assert.Equal(4, verified.InformantDriftComparisonCount);
        Assert.Equal(4, verified.InformantDriftMissingStageCount);
        Assert.Equal(4, verified.InformantDriftDisqualifiedStageCount);
        Assert.Equal(4, verified.InformantDriftZeroedStageCount);
        Assert.Equal(0, verified.InformantDriftUnverifiableStageCount);
        Assert.NotNull(verified.InformantDriftMeanRelativeError);
        Assert.NotNull(verified.InformantDriftErrorSampleStandardDeviation);
        Assert.Contains(verified.Diagnostics, diagnostic => diagnostic.StartsWith(
            "informant_drift_method:",
            StringComparison.Ordinal));
        Assert.Contains(verified.Diagnostics, diagnostic => diagnostic.StartsWith(
            "informant_drift_exclusions: missing=4, disqualified=4, zeroed=4, unverifiable=0",
            StringComparison.Ordinal));
        Assert.Contains(verified.Diagnostics, diagnostic => diagnostic.Contains(
            $"stage_grouping_convention: {FreezerAnalysisEngine.StageGroupingConvention}",
            StringComparison.Ordinal));
        Assert.Equal(3, verified.Groups.Count);
        var shortStageGroup = Assert.Single(verified.Groups, group => group.StageGroup == "short");
        Assert.Equal("short", shortStageGroup.StageGroup);
        Assert.Equal(1, shortStageGroup.StageCount);
        Assert.Equal(1, shortStageGroup.VerifiableStageCount);
        Assert.Equal(0, shortStageGroup.MissingStageCount);
        var mediumStageGroup = Assert.Single(verified.Groups, group => group.StageGroup == "medium");
        Assert.Equal(1, mediumStageGroup.StageCount);
        Assert.Equal(1, mediumStageGroup.VerifiableStageCount);
        Assert.Equal(0, mediumStageGroup.MissingStageCount);
        var longStageGroup = Assert.Single(verified.Groups, group => group.StageGroup == "long");
        Assert.Equal(2, longStageGroup.StageCount);
        Assert.Equal(1, longStageGroup.VerifiableStageCount);
        Assert.Equal(1, longStageGroup.MissingStageCount);
        Assert.Equal(
            verified.Stages.Count(stage => stage.IsWithinPredictionInterval == true),
            shortStageGroup.IntervalCoverageCount
                + mediumStageGroup.IntervalCoverageCount
                + longStageGroup.IntervalCoverageCount);
        Assert.Equal(
            longStageGroup.IntervalCoverageCount / (double)longStageGroup.VerifiableStageCount,
            longStageGroup.IntervalCoverageRate);
        Assert.All(verified.Stages.Where(stage => stage.Status == "scored"), stage =>
        {
            Assert.Equal(
                stage.StageNumber switch
                {
                    1 => "short",
                    2 => "medium",
                    _ => "long"
                },
                stage.StageGroup);
            Assert.NotNull(stage.RelativeError);
            Assert.NotNull(stage.IsWithinPredictionInterval);
        });
        var missingStage = Assert.Single(verified.Stages, stage => stage.Status == "missing");
        Assert.Equal("long", missingStage.StageGroup);
        Assert.Null(missingStage.RelativeError);
        Assert.Null(missingStage.IsWithinPredictionInterval);
        var repeatedVerification = await client.PostAsJsonAsync(
            $"/api/v1/snapshots/{snapshot.Id}/verification",
            verifyRequest);
        var repeatedReport = await repeatedVerification.Content.ReadFromJsonAsync<FreezerVerificationResponse>();
        Assert.Equal(verified.Id, repeatedReport!.Id);

        var candidateParameters = new FreezerParameterValuesRequest
        {
            Beta = 1.25,
            DriftPercent = 0.05,
            OwnVariability = 0.11,
            DistancePenalty = 0.3
        };
        var backtestRequest = new FreezerParameterBacktestRequest
        {
            Parameters = candidateParameters,
            Evaluations =
            [
                new FreezerParameterBacktestEvaluationRequest
                {
                    SnapshotId = snapshot.Id,
                    FinalRevisionId = currentMatch.ActiveRevisionId
                }
            ]
        };
        var backtestResponse = await client.PostAsJsonAsync(
            "/api/v1/parameter-backtests",
            backtestRequest);
        Assert.Equal(HttpStatusCode.OK, backtestResponse.StatusCode);
        var backtest = await backtestResponse.Content.ReadFromJsonAsync<FreezerParameterBacktestResponse>();
        Assert.NotNull(backtest);
        Assert.Equal(3, backtest.VerifiableStageCount);
        Assert.Equal("evaluated", Assert.Single(backtest.Evaluations).Status);
        Assert.Equal(candidateParameters.Beta, backtest.Parameters.Beta);
        Assert.Equal(backtest.Id, (await client.GetFromJsonAsync<FreezerParameterBacktestResponse>(
            $"/api/v1/parameter-backtests/{backtest.Id}"))!.Id);

        var unconfirmedAdoption = await client.PostAsJsonAsync(
            "/api/v1/parameters/adoption",
            new FreezerParameterAdoptionRequest
            {
                BacktestId = backtest.Id,
                ExpectedRevision = 0,
                Confirm = false
            });
        Assert.Equal(HttpStatusCode.BadRequest, unconfirmedAdoption.StatusCode);
        var adoptedResponse = await client.PostAsJsonAsync(
            "/api/v1/parameters/adoption",
            new FreezerParameterAdoptionRequest
            {
                BacktestId = backtest.Id,
                ExpectedRevision = 0,
                Confirm = true
            });
        Assert.Equal(HttpStatusCode.OK, adoptedResponse.StatusCode);
        var adoption = await adoptedResponse.Content.ReadFromJsonAsync<FreezerParameterAdoptionResponse>();
        Assert.NotNull(adoption);
        Assert.Equal(1, adoption.Revision);
        Assert.Equal(backtest.ParameterVersion, adoption.ParameterVersion);
        Assert.Equal(candidateParameters.Beta, adoption.Parameters.Beta);
        var staleAdoption = await client.PostAsJsonAsync(
            "/api/v1/parameters/adoption",
            new FreezerParameterAdoptionRequest
            {
                BacktestId = backtest.Id,
                ExpectedRevision = 0,
                Confirm = true
            });
        Assert.Equal(HttpStatusCode.Conflict, staleAdoption.StatusCode);
        var savedAdoption = await client.GetFromJsonAsync<FreezerParameterAdoptionResponse>(
            "/api/v1/parameters/adoption");
        Assert.Equal(adoption.Revision, savedAdoption!.Revision);

        var replayAfterAdoption = await client.GetFromJsonAsync<FreezerSnapshotReplayResponse>(
            $"/api/v1/snapshots/{snapshot.Id}/replay");
        Assert.NotNull(replayAfterAdoption);
        Assert.Equal(firstReplay.Stages, replayAfterAdoption.Stages);
        var accountSnapshots = await client.GetFromJsonAsync<FreezerSnapshotSelectionResponse[]>("/api/v1/snapshots");
        Assert.Contains(accountSnapshots!, item => item.Id == snapshot.Id);

        var adoptedDraftMatch = await PublishMatchAsync(
            client,
            "Adopted parameter target",
            targetDate.AddDays(1),
            BuildScoreCsv(
                $"adoption-target-{Guid.NewGuid():N}",
                targetDate.AddDays(1),
                [("target-self", "Runner One", [1.1])]));
        var adoptedDraft = await client.GetFromJsonAsync<FreezerDraftResponse>(
            $"/api/v1/matches/{adoptedDraftMatch.Id}/draft");
        Assert.NotNull(adoptedDraft);
        Assert.Equal(candidateParameters.Beta, adoptedDraft.Beta);
        Assert.Equal(candidateParameters.DriftPercent, adoptedDraft.DriftPercent);
        Assert.Equal(candidateParameters.OwnVariability, adoptedDraft.OwnVariability);
        Assert.Equal(candidateParameters.DistancePenalty, adoptedDraft.DistancePenalty);
        Assert.Equal(backtest.ParameterVersion, adoptedDraft.ParameterVersion);
        var backtestAfterNewPublicationResponse = await client.PostAsJsonAsync(
            "/api/v1/parameter-backtests",
            backtestRequest);
        var backtestAfterNewPublication =
            await backtestAfterNewPublicationResponse.Content.ReadFromJsonAsync<FreezerParameterBacktestResponse>();
        Assert.Equal(HttpStatusCode.OK, backtestAfterNewPublicationResponse.StatusCode);
        Assert.Equal(
            backtest.Evaluations.Single().Stages.Select(stage =>
                (stage.StageId, stage.Status, stage.ActualHitFactor, stage.ForecastCenter, stage.ForecastLower, stage.ForecastUpper)),
            backtestAfterNewPublication!.Evaluations.Single().Stages.Select(stage =>
                (stage.StageId, stage.Status, stage.ActualHitFactor, stage.ForecastCenter, stage.ForecastLower, stage.ForecastUpper)));
        var savedAdoptedDraftResponse = await client.PutAsJsonAsync(
            $"/api/v1/matches/{adoptedDraftMatch.Id}/draft",
            new FreezerDraftRequest
            {
                ExpectedRevision = adoptedDraft.Revision,
                IdentityId = identityIds["self"],
                Division = "PRODUCTION",
                Beta = candidateParameters.Beta,
                DriftPercent = candidateParameters.DriftPercent,
                OwnVariability = candidateParameters.OwnVariability,
                DistancePenalty = candidateParameters.DistancePenalty,
                Performance = 0.935
            });
        Assert.Equal(HttpStatusCode.OK, savedAdoptedDraftResponse.StatusCode);
        var savedAdoptedDraft = await savedAdoptedDraftResponse.Content.ReadFromJsonAsync<FreezerDraftResponse>();
        Assert.Equal(backtest.ParameterVersion, savedAdoptedDraft!.ParameterVersion);

        var preMatchRevision = await client.PostAsJsonAsync(
            $"/api/v1/snapshots/{snapshot.Id}/verification",
            new FreezerVerificationRequest { FinalRevisionId = frozenRecord.MatchRevisionId });
        Assert.Equal(HttpStatusCode.BadRequest, preMatchRevision.StatusCode);
        var unknownSnapshot = await client.PostAsJsonAsync(
            "/api/v1/snapshots/not-owned/verification",
            verifyRequest);
        Assert.Equal(HttpStatusCode.NotFound, unknownSnapshot.StatusCode);
        var unknownReplay = await client.GetAsync("/api/v1/snapshots/not-owned/replay");
        Assert.Equal(HttpStatusCode.NotFound, unknownReplay.StatusCode);

        var dqdRevision = await PublishMatchAsync(
            client,
            "Target synthetic match",
            targetDate,
            BuildScoreCsv(
                "target-estimate-test",
                targetDate,
                [("target-self", "Runner One", [1.2, 1.6, 2.1, 2.4])],
                disqualifiedCompetitorIds: new HashSet<string>(StringComparer.Ordinal) { "target-self" },
                minimumRoundsByStage: [12, 13, 25, 26]),
            expectedRevision: 2);
        var dqdReport = await VerifyTargetRevisionAsync(dqdRevision);
        Assert.All(dqdReport.Stages, stage => Assert.Equal("disqualified", stage.Status));
        Assert.Equal(4, dqdReport.Groups.Sum(group => group.DisqualifiedStageCount));
        Assert.All(dqdReport.Groups, group =>
        {
            Assert.Equal(0, group.VerifiableStageCount);
            Assert.Equal(0, group.UnverifiableStageCount);
            Assert.Null(group.IntervalCoverageRate);
        });

        var zeroedRevision = await PublishMatchAsync(
            client,
            "Target synthetic match",
            targetDate,
            BuildScoreCsv(
                "target-estimate-test",
                targetDate,
                [("target-self", "Runner One", [1.2, 1.6, 2.1, 2.4])],
                zeroedCompetitorIds: new HashSet<string>(StringComparer.Ordinal) { "target-self" },
                minimumRoundsByStage: [12, 13, 25, 26]),
            expectedRevision: 3);
        var zeroedReport = await VerifyTargetRevisionAsync(zeroedRevision);
        Assert.All(zeroedReport.Stages, stage => Assert.Equal("zeroed", stage.Status));
        Assert.Equal(4, zeroedReport.Groups.Sum(group => group.ZeroedStageCount));
        Assert.All(zeroedReport.Groups, group =>
        {
            Assert.Equal(0, group.VerifiableStageCount);
            Assert.Equal(0, group.UnverifiableStageCount);
            Assert.Null(group.IntervalCoverageRate);
        });

        var additionalStageRevision = await PublishMatchAsync(
            client,
            "Target synthetic match",
            targetDate,
            BuildScoreCsv(
                "target-estimate-test",
                targetDate,
                [("target-self", "Runner One", [1.2, 1.6, 2.1, 2.4, 1.7])],
                minimumRoundsByStage: [12, 13, 25, 26, 13]),
            expectedRevision: 4);
        var additionalStageReport = await VerifyTargetRevisionAsync(additionalStageRevision);
        Assert.Single(additionalStageReport.Stages, stage =>
            stage.StageNumber == 4 && stage.Status == "forecast_unavailable");
        Assert.Single(additionalStageReport.Stages, stage =>
            stage.StageNumber == 5 && stage.Status == "forecast_unavailable");
        Assert.Equal(2, additionalStageReport.Groups.Sum(group => group.UnverifiableStageCount));

        async Task<FreezerVerificationResponse> VerifyTargetRevisionAsync(FreezerMatchResponse revision)
        {
            var review = await client.GetFromJsonAsync<FreezerIdentityReviewEntryResponse[]>(
                $"/api/v1/matches/{revision.Id}/identities/review");
            var target = Assert.Single(review!, entry => entry.CompetitorId == "target-self");
            var resolution = await client.PutAsJsonAsync(
                $"/api/v1/matches/{revision.Id}/identities/{target.ResultId}",
                new FreezerIdentityResolutionRequest
                {
                    IdentityId = identityIds["self"],
                    ExpectedRevision = target.ResolutionRevision
                });
            Assert.Equal(HttpStatusCode.OK, resolution.StatusCode);

            var response = await client.PostAsJsonAsync(
                $"/api/v1/snapshots/{snapshot.Id}/verification",
                new FreezerVerificationRequest { FinalRevisionId = revision.ActiveRevisionId });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var report = await response.Content.ReadFromJsonAsync<FreezerVerificationResponse>();
            Assert.NotNull(report);
            return report;
        }
    }

    [Fact]
    public async Task Estimate_is_unavailable_without_prior_stage_history()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = await CreateAuthenticatedClientAsync(factory);
        var identity = await CreateIdentityAsync(client, "Runner One");
        var matchDate = new DateOnly(2026, 10, 29);
        var match = await CreateTargetMatchAsync(client, "No history target", matchDate, identity.Id);

        var estimate = await client.GetFromJsonAsync<FreezerEstimateResponse>(
            $"/api/v1/matches/{match.Id}/estimate");

        Assert.NotNull(estimate);
        Assert.False(estimate.IsAvailable);
        Assert.Single(estimate.Stages);
        Assert.Null(estimate.Stages[0].Center);
        Assert.Contains(estimate.Stages[0].Diagnostics, diagnostic =>
            diagnostic.StartsWith("stage_profile_unavailable:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Estimate_reports_missing_account_availability_and_excludes_same_day_history()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = await CreateAuthenticatedClientAsync(factory);
        var identity = await CreateIdentityAsync(client, "Runner One");
        var targetDate = new DateOnly(2026, 10, 29);
        var match = await CreateTargetMatchAsync(client, "Missing availability target", targetDate, identity.Id);
        var repository = factory.Services.GetRequiredService<IFreezerAnalysisRepository>();

        await SeedPublishedMatchAsync(repository, "other-account", targetDate.AddDays(-1), "prior-without-account-snapshot");
        await SeedPublishedMatchAsync(repository, "other-account", targetDate, "same-day-without-account-snapshot");

        var estimate = await client.GetFromJsonAsync<FreezerEstimateResponse>(
            $"/api/v1/matches/{match.Id}/estimate");

        Assert.NotNull(estimate);
        Assert.False(estimate.IsAvailable);
        Assert.Contains(estimate.Diagnostics, diagnostic =>
            diagnostic.Contains("history_availability_missing: 1 prior shared match", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Published_match_query_excludes_matches_on_the_target_date()
    {
        var repository = new InMemoryFreezerAnalysisRepository();
        var targetDate = new DateOnly(2026, 10, 29);
        var earlier = await SeedPublishedMatchAsync(repository, "owner", targetDate.AddDays(-1), "earlier");
        await SeedPublishedMatchAsync(repository, "owner", targetDate, "same-day");

        var matches = await repository.ListPublishedMatchesBeforeAsync(
            targetDate,
            1_000,
            CancellationToken.None);

        var onlyPriorMatch = Assert.Single(matches);
        Assert.Equal(earlier.Id, onlyPriorMatch.Id);
    }

    [Fact]
    public async Task Profile_mutation_without_csrf_is_rejected()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/profile", new FreezerProfileRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var csrfResponse = await client.GetAsync("/api/v1/account/csrf");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<MorWalPiz.Contracts.Contracts.PublicAccountCsrfContract>();
        Assert.NotNull(csrf);
        client.DefaultRequestHeaders.Add("Cookie", $"freezer_account_csrf={csrf.Token}");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf.Token);
        return client;
    }

    private static async Task<FreezerMatchResponse> PublishMatchAsync(
        HttpClient client,
        string matchName,
        DateOnly matchDate,
        string csv,
        int expectedRevision = 0)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "File", "scores.csv");
        form.Add(new StringContent(matchName), "MatchName");
        form.Add(new StringContent(matchDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), "MatchDate");
        form.Add(new StringContent(expectedRevision.ToString(CultureInfo.InvariantCulture)), "expectedRevision");

        var response = await client.PostAsync("/api/v1/imports/scores/publish", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matches = await client.GetFromJsonAsync<FreezerMatchResponse[]>("/api/v1/matches");
        return Assert.Single(matches!, item => item.Name == matchName);
    }

    private static async Task<FreezerIdentityResponse> CreateIdentityAsync(HttpClient client, string displayName)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/identities",
            new FreezerIdentityCreateRequest { DisplayName = displayName });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FreezerIdentityResponse>())!;
    }

    private static async Task<FreezerMatchResponse> CreateTargetMatchAsync(
        HttpClient client,
        string matchName,
        DateOnly matchDate,
        string identityId)
    {
        var match = await PublishMatchAsync(
            client,
            matchName,
            matchDate,
            BuildScoreCsv(
                $"target-{Guid.NewGuid():N}",
                matchDate,
                [("target-self", "Runner One", [1.1])]));
        var draft = await client.GetFromJsonAsync<FreezerDraftResponse>(
            $"/api/v1/matches/{match.Id}/draft");
        Assert.NotNull(draft);
        var saveDraft = await client.PutAsJsonAsync(
            $"/api/v1/matches/{match.Id}/draft",
            new FreezerDraftRequest
            {
                ExpectedRevision = draft.Revision,
                IdentityId = identityId,
                Division = "PRODUCTION",
                Performance = 0.935
            });
        Assert.Equal(HttpStatusCode.OK, saveDraft.StatusCode);
        return match;
    }

    private static async Task<FreezerMatch> SeedPublishedMatchAsync(
        IFreezerAnalysisRepository repository,
        string accountId,
        DateOnly matchDate,
        string key)
    {
        var matchId = $"match-{Guid.NewGuid():N}";
        var revisionId = $"revision-{Guid.NewGuid():N}";
        var match = new FreezerMatch
        {
            Id = matchId,
            MatchKey = key,
            Name = key,
            LocalMatchDate = matchDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            OwnerAccountId = accountId,
            CreatedAtUtc = DateTime.UtcNow
        };
        var revision = new FreezerMatchRevision
        {
            Id = revisionId,
            MatchId = matchId,
            RevisionNumber = 1,
            SourceHash = $"source-{key}",
            SourceFormat = "csv",
            ParserVersion = "test",
            Completeness = FreezerImportCompleteness.Final,
            CreatedAtUtc = DateTime.UtcNow
        };
        var manifest = new FreezerImportManifest
        {
            Id = $"import-{Guid.NewGuid():N}",
            AccountId = accountId,
            MatchId = matchId,
            SourceHash = $"source-{key}",
            Format = "csv",
            Completeness = FreezerImportCompleteness.Final,
            State = FreezerImportState.Published,
            CreatedAtUtc = DateTime.UtcNow
        };

        var publication = await repository.PublishImportAsync(
            match,
            revision,
            [],
            manifest,
            0,
            CancellationToken.None);
        Assert.False(publication.IsConflict);
        return publication.Match!;
    }

    private static async Task<IReadOnlyDictionary<string, string>> CreateAndResolveIdentitiesAsync(
        HttpClient client,
        string matchId)
    {
        var entries = await client.GetFromJsonAsync<FreezerIdentityReviewEntryResponse[]>(
            $"/api/v1/matches/{matchId}/identities/review");
        var identities = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries!)
        {
            var create = await client.PostAsJsonAsync(
                "/api/v1/identities",
                new FreezerIdentityCreateRequest { DisplayName = entry.DisplayName });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var identity = await create.Content.ReadFromJsonAsync<FreezerIdentityResponse>();
            Assert.NotNull(identity);
            var resolve = await client.PutAsJsonAsync(
                $"/api/v1/matches/{matchId}/identities/{entry.ResultId}",
                new FreezerIdentityResolutionRequest { IdentityId = identity.Id });
            Assert.Equal(HttpStatusCode.OK, resolve.StatusCode);
            identities.Add(entry.CompetitorId, identity.Id);
        }
        return identities;
    }

    private static string BuildScoreCsv(
        string matchKey,
        DateOnly matchDate,
        IReadOnlyList<(string CompetitorId, string Name, double[] HitFactors)> competitors,
        IReadOnlySet<string>? disqualifiedCompetitorIds = null,
        IReadOnlySet<string>? zeroedCompetitorIds = null,
        IReadOnlySet<(string CompetitorId, int StageNumber)>? missingStageResults = null,
        IReadOnlyList<int>? minimumRoundsByStage = null)
    {
        var rows = new List<string>
        {
            "gara,data,comp_id,nome,div,fattore,stage,colpi,hf,punti,tempo,totale,prima,escluso,azzerato,dq,A,C,D,M,NS,PE"
        };
        foreach (var competitor in competitors)
        {
            var isDisqualified = disqualifiedCompetitorIds?.Contains(competitor.CompetitorId) == true;
            var isZeroed = zeroedCompetitorIds?.Contains(competitor.CompetitorId) == true;
            var total = isDisqualified || isZeroed
                ? 0
                : competitor.HitFactors
                    .Select((_, index) => (Index: index, Rounds: minimumRoundsByStage?[index] ?? 1))
                    .Where(stage => missingStageResults?.Contains((competitor.CompetitorId, stage.Index + 1)) != true)
                    .Sum(stage => stage.Rounds * 5);
            for (var index = 0; index < competitor.HitFactors.Length; index++)
            {
                var rounds = minimumRoundsByStage?[index] ?? 1;
                var isMissing = missingStageResults?.Contains((competitor.CompetitorId, index + 1)) == true;
                var hitFactor = isDisqualified || isZeroed || isMissing ? 0 : competitor.HitFactors[index];
                var points = isDisqualified || isZeroed || isMissing ? 0 : rounds * 5;
                var time = hitFactor > 0 ? points / hitFactor : 0;
                rows.Add(string.Join(",", new[]
                {
                    matchKey,
                    matchDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    competitor.CompetitorId,
                    competitor.Name,
                    "Production",
                    "Minor",
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    rounds.ToString(CultureInfo.InvariantCulture),
                    hitFactor.ToString("0.######", CultureInfo.InvariantCulture),
                    points.ToString(CultureInfo.InvariantCulture),
                    time.ToString("0.######", CultureInfo.InvariantCulture),
                    total.ToString(CultureInfo.InvariantCulture),
                    "true",
                    "false",
                    isZeroed ? "true" : "false",
                    isDisqualified ? "true" : "false",
                    isDisqualified || isZeroed || isMissing ? "0" : rounds.ToString(CultureInfo.InvariantCulture),
                    "0",
                    "0",
                    "0",
                    "0",
                    "0"
                }));
            }
        }
        return string.Join(Environment.NewLine, rows);
    }

    private const string SingleCompetitorCsv = """
        gara,data,comp_id,nome,div,fattore,stage,colpi,hf,punti,tempo,totale,prima,escluso,azzerato,dq,A,C,D,M,NS,PE
        identity-test,29/10/2026,competitor-1,Runner One,Production,Minor,1,1,1,5,5,5,true,false,false,false,1,0,0,0,0,0
        """;
}
