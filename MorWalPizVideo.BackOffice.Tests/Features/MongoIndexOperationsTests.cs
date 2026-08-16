using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using MongoDB.Bson;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.BackOffice.Services;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class MongoIndexOperationsTests
{
    [Fact]
    public async Task Removal_rejects_unknown_keys_before_accessing_database()
    {
        var service = new MongoIndexOperationsService(null!);

        var exception = await Assert.ThrowsAsync<MongoIndexOperationValidationException>(() =>
            service.RemoveAsync(["pages_url", "not-allowlisted"]));

        Assert.Contains("Unknown Mongo index removal key(s): not-allowlisted.", exception.Message);
    }

    [Fact]
    public void Removal_manifest_maps_only_the_legacy_page_index()
    {
        var entry = Assert.Single(MongoIndexOperationsService.RemovalManifest);

        Assert.Equal("pages_url", entry.Key);
        Assert.Equal("pages", entry.Collection);
        Assert.Equal("ix_pages_url", entry.Name);
        Assert.Equal("pages_url.unique", entry.ReplacementKey);
    }

    [Fact]
    public void Replacement_definition_requires_unique_url_index()
    {
        var desired = MongoIndexOperationsService.Manifest.Single(item => item.Key == "pages_url.unique");
        var correct = new BsonDocument
        {
            { "name", "ux_pages_url_ci" },
            { "key", new BsonDocument("url", 1) },
            { "unique", true }
        };
        var nonUnique = correct.DeepClone().AsBsonDocument;
        nonUnique["unique"] = false;
        var wrongKeys = correct.DeepClone().AsBsonDocument;
        wrongKeys["key"] = new BsonDocument("url", -1);

        Assert.True(MongoIndexOperationsService.HasExpectedDefinition(correct, desired));
        Assert.False(MongoIndexOperationsService.HasExpectedDefinition(nonUnique, desired));
        Assert.False(MongoIndexOperationsService.HasExpectedDefinition(wrongKeys, desired));
    }

    [Fact]
    public void Removal_decision_targets_only_the_legacy_index_name()
    {
        var removal = Assert.Single(MongoIndexOperationsService.RemovalManifest);
        var replacementOnly = new[]
        {
            new BsonDocument
            {
                { "name", "ux_pages_url_ci" },
                { "key", new BsonDocument("url", 1) },
                { "unique", true }
            }
        };
        var legacyPresent = replacementOnly.Append(new BsonDocument
        {
            { "name", "ix_pages_url" },
            { "key", new BsonDocument("url", 1) }
        });

        Assert.Equal("skipped_absent", MongoIndexOperationsService.GetRemovalAction(replacementOnly, removal));
        Assert.Equal("removed", MongoIndexOperationsService.GetRemovalAction(legacyPresent, removal));
    }

    [Fact]
    public void Remove_route_is_api_key_protected_and_has_explicit_route()
    {
        var controller = typeof(MongoIndexesController);
        var action = controller.GetMethod(nameof(MongoIndexesController.Remove));

        Assert.NotNull(controller.GetCustomAttributes(typeof(ApiKeyAuthAttribute), inherit: true).SingleOrDefault());
        Assert.Equal("remove", action?.GetCustomAttributes<HttpPostAttribute>().Single().Template);
    }

    [Fact]
    public async Task Remove_route_requires_approval_token_and_non_empty_removal_keys()
    {
        var service = new RecordingMongoIndexOperationsService();
        var controller = new MongoIndexesController(service);

        var invalidToken = await controller.Remove(
            new MongoIndexesController.RemoveMongoIndexesRequest
            {
                ApprovalToken = "wrong-token",
                ApprovedRemovalKeys = ["pages_url"]
            },
            CancellationToken.None);
        var emptyKeys = await controller.Remove(
            new MongoIndexesController.RemoveMongoIndexesRequest
            {
                ApprovalToken = "apply-approved-indexes",
                ApprovedRemovalKeys = []
            },
            CancellationToken.None);

        Assert.Equal(400, ((BadRequestObjectResult)invalidToken).StatusCode);
        Assert.Equal(400, ((BadRequestObjectResult)emptyKeys).StatusCode);
        Assert.Empty(service.RemovalKeys);
    }

    [Fact]
    public async Task Remove_route_forwards_allowlisted_key_and_returns_removal_result()
    {
        var service = new RecordingMongoIndexOperationsService
        {
            RemovalResults =
            [
                new MongoIndexRemovalResult("pages_url", "pages", "ix_pages_url", "removed")
            ]
        };
        var controller = new MongoIndexesController(service);

        var result = await controller.Remove(
            new MongoIndexesController.RemoveMongoIndexesRequest
            {
                ApprovalToken = "apply-approved-indexes",
                ApprovedRemovalKeys = ["pages_url"]
            },
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(["pages_url"], service.RemovalKeys);
        Assert.Same(service.RemovalResults, response.Value);
    }

    private sealed class RecordingMongoIndexOperationsService : IMongoIndexOperationsService
    {
        public IList<string> RemovalKeys { get; private set; } = [];
        public IList<MongoIndexRemovalResult> RemovalResults { get; set; } = [];

        public Task<IList<MongoIndexAuditItem>> AuditAsync(IList<string>? keys = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IList<MongoIndexAuditItem>>([]);

        public Task<IList<MongoIndexApplyResult>> ApplyAsync(IList<string> approvedKeys, CancellationToken cancellationToken = default) =>
            Task.FromResult<IList<MongoIndexApplyResult>>([]);

        public Task<IList<MongoIndexRemovalResult>> RemoveAsync(IList<string> approvedRemovalKeys, CancellationToken cancellationToken = default)
        {
            RemovalKeys = approvedRemovalKeys;
            return Task.FromResult(RemovalResults);
        }
    }
}