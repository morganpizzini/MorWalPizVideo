using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MorWalPiz.Contracts.DTOs;
using MorWalPizVideo.BackOffice.DTOs;
using MorWalPizVideo.BackOffice.Services.Interfaces;
using MorWalPizVideo.BackOffice.Services.Ai;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class AiOrchestrationTests(BackOfficeWebApplicationFactory factory)
    : IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task MockHostExecutesEveryLegacyChatRouteWithoutExternalAi()
    {
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var apiKeys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var (_, apiKey) = await apiKeys.CreateApiKeyAsync(
            "ai-orchestration-test",
            "Test-only key",
            PrimaryScenario.ChannelId);
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

        using var reviewResponse = await client.PostAsJsonAsync("/api/chat", new ReviewRequest
        {
            Names = ["IPSC stage run"],
            Context = "Indoor match",
            Languages = ["en", "es"]
        });

        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        reviewResponse.Headers.Contains("X-AI-Execution-Id").Should().BeTrue();
        var review = await reviewResponse.Content.ReadFromJsonAsync<List<ReviewApiVideoResponse>>();
        review.Should().ContainSingle();
        review![0].Name.Should().Be("IPSC stage run");
        review[0].Translations.Select(item => item.Language).Should().BeEquivalentTo(["en", "es", "IT-it"]);

        using var translationResponse = await client.PostAsJsonAsync("/api/chat/translate", new VideoTranslationRequest
        {
            Title = "Dynamic shooting",
            Description = "A description",
            Languages = ["en", "fr"]
        });
        translationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        translationResponse.Headers.Contains("X-AI-Execution-Id").Should().BeTrue();
        (await translationResponse.Content.ReadFromJsonAsync<List<VideoTranslationResponse>>())
            .Should().HaveCount(2);

        using var transcriptResponse = await client.PostAsJsonAsync("/api/chat/transcript-analysis", new TranscriptAnalysisRequest
        {
            Transcript = "(00:01.200) IPSC stage briefing\n(00:02.400) Shooter begins the run",
            Context = "Competition highlights"
        });
        transcriptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        transcriptResponse.Headers.Contains("X-AI-Execution-Id").Should().BeTrue();
        var transcript = await transcriptResponse.Content.ReadFromJsonAsync<TranscriptAnalysisResponse>();
        transcript.Should().NotBeNull();
        transcript!.Titles.Should().HaveCount(3);
        transcript.Descriptions.Should().HaveCount(3);
        transcript.Hashtags.Should().Contain("IPSC");
    }

    [Fact]
    public async Task LegacyFallbackRemainsAvailableUntilOrchestrationIsEnabled()
    {
        IAiWorkflowService legacy = new LegacyAiWorkflowService(new DeterministicAiModelGateway());

        var review = await legacy.ReviewAsync(new ReviewRequest
        {
            Names = ["IPSC stage run"],
            Languages = ["en"]
        }, PrimaryScenario.ChannelId, null, CancellationToken.None);
        var translation = await legacy.TranslateAsync(new VideoTranslationRequest
        {
            Title = "Dynamic shooting",
            Description = "A description",
            Languages = ["fr"]
        }, PrimaryScenario.ChannelId, CancellationToken.None);
        var transcript = await legacy.AnalyzeTranscriptAsync(new TranscriptAnalysisRequest
        {
            Transcript = "(00:01.200) IPSC stage briefing\n(00:02.400) Shooter begins the run"
        }, PrimaryScenario.ChannelId, CancellationToken.None);

        review.Value.Should().ContainSingle();
        review.Value[0].Name.Should().Be("IPSC stage run");
        translation.Value.Should().ContainSingle(item => item.LanguageCode == "fr");
        transcript.Value.Titles.Should().HaveCount(3);
    }

    [Fact]
    public async Task ChatRequiresTheApiKeyAuthenticationScheme()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        using var response = await client.PostAsJsonAsync("/api/chat", new ReviewRequest
        {
            Names = ["IPSC stage run"]
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PromptManagementRequiresItsOwnPermissionAndMockHostHasBaselines()
    {
        using var unauthorizedClient = factory.CreateClient();
        using var forbidden = await unauthorizedClient.GetAsync("/api/v1/ai/workflows");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var viewerClient = factory.CreateClientWithPermissions(AuthorizationPermissionKeys.AiPromptsView);
        using var response = await viewerClient.GetAsync("/api/v1/ai/workflows");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var definitions = await response.Content.ReadFromJsonAsync<List<AiPromptDefinitionDto>>();
        definitions.Should().NotBeNull();
        definitions.Should().HaveCount(AiPromptStageCatalog.All.Count);
        definitions!.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.ActiveRevisionId));
    }

    [Fact]
    public async Task PromptRevisionsAreEvaluatedCanariedPromotedAndRolledBackByPointer()
    {
        var scenario = new PrimaryScenario();
        var repository = new AiOrchestrationMockRepository(scenario);
        var options = Options.Create(new AiOrchestrationOptions
        {
            ActivationEnabled = true,
            MinimumReviewedCases = 2,
            MaximumLatencyRegressionPercent = 1000,
            MinimumCanaryExecutions = 2,
            MaximumCanaryFailurePercent = 0,
            MaximumCanaryLatencyRegressionPercent = 1000
        });
        var gateway = new DeterministicAiModelGateway();
        var tools = new AiToolRegistry();
        var catalog = new AiPromptCatalog(repository, tools, options);
        var service = new AiPromptOperationsService(repository, catalog, tools, gateway, options);

        await service.BootstrapAsync("operator", CancellationToken.None);
        var workflow = AiPromptStageCatalog.TranscriptAnalysisWorkflow;
        var stage = AiPromptStageKeys.TranscriptMetadata;
        var baseline = (await repository.GetDefinitionAsync(workflow, stage))!;
        var activeRevision = (await repository.GetRevisionAsync(baseline.ActiveRevisionId!))!;
        var draftRequest = new CreateAiPromptRevisionRequest
        {
            Messages = activeRevision.Messages.Select(message => new AiPromptMessageDto
            {
                Role = message.Role,
                Template = message.Template
            }).ToList(),
            Variables = activeRevision.Variables,
            ResponseSchema = activeRevision.ResponseSchema,
            Rationale = "Strengthen fidelity to the source transcript."
        };
        draftRequest.Messages[0].Template += Environment.NewLine + "Keep every output grounded in the supplied transcript.";
        var draft = await service.CreateDraftAsync(workflow, stage, draftRequest,
            "operator",
            CancellationToken.None);

        for (var index = 0; index < 2; index++)
        {
            await service.AddEvaluationCaseAsync(new CreateAiEvaluationCaseRequest
            {
                WorkflowKey = workflow,
                StageKey = stage,
                DatasetVersion = "reviewed-v1",
                InputJson = """{"transcript":"IPSC stage briefing","context":"competition"}""",
                ExpectedContains = ["IPSC"],
                SourceClassification = "curated",
                ContentConsentGranted = true,
                ContentRedacted = true
            }, "operator", CancellationToken.None);
        }

        var experiment = await service.EvaluateAsync(workflow, stage, new EvaluateAiPromptRequest
        {
            CandidateRevisionId = draft.Id,
            DatasetVersion = "reviewed-v1"
        }, "operator", CancellationToken.None);

        experiment.Status.Should().Be("Passed");
        experiment.CaseResults.Should().HaveCount(2);
        (await service.StartCanaryAsync(workflow, stage, new StartAiPromptCanaryRequest
        {
            CandidateRevisionId = draft.Id,
            ExpectedActiveRevisionId = baseline.ActiveRevisionId!,
            ExperimentId = experiment.Id,
            Percentage = 50
        }, "operator", CancellationToken.None)).CanaryRevisionId.Should().Be(draft.Id);

        var canarySelections = 0;
        for (var index = 0; index < 100; index++)
        {
            var prompt = await catalog.ResolveAsync(workflow, stage, executionId: $"execution-{index}");
            if (prompt.Revision.Id == draft.Id)
                canarySelections++;
        }
        canarySelections.Should().BeInRange(1, 99);

        var promotionRequest = new PromoteAiPromptCanaryRequest
        {
            ExpectedActiveRevisionId = baseline.ActiveRevisionId!,
            ExpectedCanaryRevisionId = draft.Id,
            ExperimentId = experiment.Id
        };
        var earlyPromotion = () => service.PromoteCanaryAsync(
            workflow, stage, promotionRequest, "operator", CancellationToken.None);
        await earlyPromotion.Should().ThrowAsync<AiPromptCanaryEvidenceException>();

        foreach (var revisionId in new[] { baseline.ActiveRevisionId!, draft.Id })
        {
            for (var index = 0; index < 2; index++)
            {
                var now = DateTime.UtcNow;
                await repository.AddExecutionAsync(new AiExecutionRecord
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ExecutionId = Guid.NewGuid().ToString("N"),
                    WorkflowKey = workflow,
                    StageKey = stage,
                    RevisionId = revisionId,
                    Status = "Succeeded",
                    DurationMilliseconds = 10,
                    InputCount = 1,
                    OutputCount = 1,
                    CreatedAtUtc = now,
                    ExpiresAtUtc = now.AddDays(30),
                    CreationDateTime = now
                });
            }
        }

        var promoted = await service.PromoteCanaryAsync(workflow, stage, new PromoteAiPromptCanaryRequest
        {
            ExpectedActiveRevisionId = promotionRequest.ExpectedActiveRevisionId,
            ExpectedCanaryRevisionId = promotionRequest.ExpectedCanaryRevisionId,
            ExperimentId = promotionRequest.ExperimentId
        }, "operator", CancellationToken.None);
        promoted.ActiveRevisionId.Should().Be(draft.Id);
        promoted.CanaryRevisionId.Should().BeNull();

        var rolledBack = await service.RollbackAsync(workflow, stage, new RollbackAiPromptRequest
        {
            ExpectedActiveRevisionId = draft.Id
        }, "operator", CancellationToken.None);
        rolledBack.ActiveRevisionId.Should().Be(baseline.ActiveRevisionId);
    }

    [Fact]
    public async Task PromptPublicationRejectsUndeclaredVariablesAndUnregisteredTools()
    {
        var scenario = new PrimaryScenario();
        var repository = new AiOrchestrationMockRepository(scenario);
        var options = Options.Create(new AiOrchestrationOptions());
        var tools = new AiToolRegistry();
        var catalog = new AiPromptCatalog(repository, tools, options);
        var service = new AiPromptOperationsService(repository, catalog, tools, new DeterministicAiModelGateway(), options);
        await service.BootstrapAsync("operator", CancellationToken.None);

        var stage = AiPromptStageCatalog.Get(AiPromptStageCatalog.TranscriptAnalysisWorkflow, AiPromptStageKeys.TranscriptMetadata);
        var baseline = (await repository.GetRevisionAsync(
            (await repository.GetDefinitionAsync(stage.WorkflowKey, stage.StageKey))!.ActiveRevisionId!))!;
        var request = new CreateAiPromptRevisionRequest
        {
            Messages = baseline.Messages.Select(message => new AiPromptMessageDto
            {
                Role = message.Role,
                Template = message.Role == "user" ? "{{payload}} {{untrusted}}" : message.Template
            }).ToList(),
            Variables = ["payload"],
            ResponseSchema = stage.ResponseSchema,
            Rationale = "Invalid revision"
        };

        var action = () => service.CreateDraftAsync(stage.WorkflowKey, stage.StageKey, request, "operator", CancellationToken.None);
        await action.Should().ThrowAsync<ArgumentException>();

        request.Messages = baseline.Messages.Select(message => new AiPromptMessageDto { Role = message.Role, Template = message.Template }).ToList();
        request.AllowedToolNames = ["arbitrary.mongo.query"];
        var toolAction = () => service.CreateDraftAsync(stage.WorkflowKey, stage.StageKey, request, "operator", CancellationToken.None);
        await toolAction.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EvaluationCasesRequireConsentAndRedaction()
    {
        var scenario = new PrimaryScenario();
        var repository = new AiOrchestrationMockRepository(scenario);
        var options = Options.Create(new AiOrchestrationOptions());
        var tools = new AiToolRegistry();
        var catalog = new AiPromptCatalog(repository, tools, options);
        var service = new AiPromptOperationsService(repository, catalog, tools, new DeterministicAiModelGateway(), options);
        await service.BootstrapAsync("operator", CancellationToken.None);

        var action = () => service.AddEvaluationCaseAsync(new CreateAiEvaluationCaseRequest
        {
            WorkflowKey = AiPromptStageCatalog.TranscriptAnalysisWorkflow,
            StageKey = AiPromptStageKeys.TranscriptMetadata,
            DatasetVersion = "unreviewed",
            InputJson = """{"transcript":"sensitive source"}""",
            SourceClassification = "curated"
        }, "operator", CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>().WithMessage("*consent and redaction*");
    }

    [Fact]
    public async Task EvaluationFailuresCreateOnlyAnUnactivatedDraftCandidate()
    {
        var scenario = new PrimaryScenario();
        var repository = new AiOrchestrationMockRepository(scenario);
        var options = Options.Create(new AiOrchestrationOptions { MinimumReviewedCases = 1 });
        var tools = new AiToolRegistry();
        var gateway = new DeterministicAiModelGateway();
        var catalog = new AiPromptCatalog(repository, tools, options);
        var operations = new AiPromptOperationsService(repository, catalog, tools, gateway, options);
        var optimizer = new AiPromptOptimizationService(repository, gateway, tools, options);
        await operations.BootstrapAsync("operator", CancellationToken.None);

        var workflow = AiPromptStageCatalog.TranscriptAnalysisWorkflow;
        var stage = AiPromptStageKeys.TranscriptMetadata;
        var definition = (await repository.GetDefinitionAsync(workflow, stage))!;
        var baseline = (await repository.GetRevisionAsync(definition.ActiveRevisionId!))!;
        var draftRequest = new CreateAiPromptRevisionRequest
        {
            Messages = baseline.Messages.Select(message => new AiPromptMessageDto
            {
                Role = message.Role,
                Template = message.Template
            }).ToList(),
            Variables = baseline.Variables,
            ResponseSchema = baseline.ResponseSchema,
            Rationale = "Candidate intended to satisfy a reviewed case."
        };
        draftRequest.Messages[0].Template += Environment.NewLine + "Use precise and verifiable wording.";
        var draft = await operations.CreateDraftAsync(workflow, stage, draftRequest,
            "operator",
            CancellationToken.None);

        await operations.AddEvaluationCaseAsync(new CreateAiEvaluationCaseRequest
        {
            WorkflowKey = workflow,
            StageKey = stage,
            DatasetVersion = "failure-v1",
            InputJson = """{"transcript":"IPSC stage briefing","context":"competition"}""",
            ExpectedContains = ["This phrase is intentionally absent."],
            SourceClassification = "curated",
            ContentConsentGranted = true,
            ContentRedacted = true
        }, "operator", CancellationToken.None);
        var experiment = await operations.EvaluateAsync(workflow, stage, new EvaluateAiPromptRequest
        {
            CandidateRevisionId = draft.Id,
            DatasetVersion = "failure-v1"
        }, "operator", CancellationToken.None);
        experiment.Status.Should().Be("Rejected");

        var optimized = await optimizer.CreateCandidateFromExperimentAsync(experiment.Id, "operator", CancellationToken.None);

        optimized.Status.Should().Be(AiPromptRevisionStatus.Draft);
        optimized.ParentRevisionId.Should().Be(draft.Id);
        optimized.SourceExperimentId.Should().Be(experiment.Id);
        (await repository.GetDefinitionAsync(workflow, stage))!.ActiveRevisionId.Should().Be(definition.ActiveRevisionId);
    }
}
