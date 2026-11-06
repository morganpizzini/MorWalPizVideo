using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using MongoDB.Bson;
using MongoDB.Driver;
using MorWalPizVideo.BackOffice.Authentication;
using MorWalPizVideo.BackOffice.Controllers;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.BlogInteractions;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:Shared")]
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
        var entry = MongoIndexOperationsService.RemovalManifest.Single(item => item.Key == "pages_url");

        Assert.Equal("pages_url", entry.Key);
        Assert.Equal("pages", entry.Collection);
        Assert.Equal("ix_pages_url", entry.Name);
        Assert.Equal("pages_url.unique", entry.ReplacementKey);
    }

    [Fact]
    public void Blog_interaction_startup_manifest_contains_only_feature_owned_indexes()
    {
        Assert.Equal([
            "blogcomments.channel_account_request.unique",
            "blogcomments.channel_post_status_created",
            "blogcomments.channel_status_created",
            "blogcomments.channel_account_created",
            "blogreactions.channel_post_account.unique",
            "blogreactions.channel_account_created",
            "blogportalaccess.channel_account.unique"
        ], BlogInteractionMongoIndexes.Manifest.Select(index => index.Key));
        Assert.All(BlogInteractionMongoIndexes.Manifest, index =>
            Assert.Contains(MongoIndexOperationsService.Manifest, manifestEntry =>
                manifestEntry.Key == index.Key &&
                manifestEntry.Name == index.Name &&
                manifestEntry.Collection == index.Collection &&
                manifestEntry.Keys.Equals(index.Keys) &&
                manifestEntry.Unique == index.Unique));
    }

    [Theory]
    [InlineData(26, "NamespaceNotFound", true)]
    [InlineData(26, null, true)]
    [InlineData(48, "NamespaceExists", false)]
    [InlineData(2, "BadValue", false)]
    public void Missing_collection_detection_is_limited_to_namespace_not_found(
        int code,
        string? codeName,
        bool expected)
    {
        Assert.Equal(expected, BlogInteractionMongoIndexes.IsNamespaceNotFound(code, codeName));
    }

    [Fact]
    public void Public_account_manifest_contains_required_unique_and_ttl_indexes()
    {
        var username = MongoIndexOperationsService.Manifest.Single(item => item.Key == "publicaccounts.normalizedusername.unique");
        var email = MongoIndexOperationsService.Manifest.Single(item => item.Key == "publicaccounts.normalizedemail.unique");
        var sessions = MongoIndexOperationsService.Manifest.Single(item => item.Key == "publicaccountsessions.expiresat.ttl");

        Assert.True(username.Unique);
        Assert.Equal("normalizedUsername", username.Keys.GetElement(0).Name);
        Assert.True(email.Unique);
        Assert.Equal("normalizedEmail", email.Keys.GetElement(0).Name);
        Assert.Equal("expiresAt", sessions.Keys.GetElement(0).Name);
        Assert.Equal(TimeSpan.Zero, sessions.ExpireAfter);
    }

    [Fact]
    public void Video_importer_settings_manifest_requires_a_unique_channel_index()
    {
        var index = MongoIndexOperationsService.Manifest
            .Single(item => item.Key == "videoimportersettings.channelid.unique");

        Assert.True(index.Unique);
        Assert.Equal("videoImporterChannelSettings", index.Collection);
        Assert.Equal("channelId", index.Keys.GetElement(0).Name);
        Assert.Equal("ux_videoimportersettings_channelid", index.Name);
    }

    [Fact]
    public void Shortlink_manifest_requires_unique_normalized_code_index_without_collation()
    {
        var entry = MongoIndexOperationsService.Manifest.Single(item => item.Key == "shortlinks.code.unique");

        Assert.True(entry.Unique);
        Assert.Equal("code", entry.Keys.GetElement(0).Name);
        Assert.Null(entry.Collation);
        Assert.Equal("ux_shortlinks_code", entry.Name);
    }

    [Fact]
    public void Shortlink_collation_index_is_registered_for_removal_after_replacement()
    {
        var removal = MongoIndexOperationsService.RemovalManifest
            .Single(item => item.Key == "shortlinks_code_collation");

        Assert.Equal("ux_shortlinks_code_ci", removal.Name);
        Assert.Equal("shortlinks.code.unique", removal.ReplacementKey);
    }

    [Fact]
    public void Collated_shortlink_index_does_not_match_the_binary_definition()
    {
        var expected = MongoIndexOperationsService.Manifest
            .Single(item => item.Key == "shortlinks.code.unique");
        var collated = new BsonDocument
        {
            { "name", "ux_shortlinks_code" },
            { "key", new BsonDocument("code", 1) },
            { "unique", true },
            { "collation", new BsonDocument { { "locale", "en" }, { "strength", 2 } } }
        };

        Assert.False(MongoIndexOperationsService.HasExpectedDefinition(collated, expected));
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
    public void Partial_filter_definition_is_part_of_index_audit()
    {
        var expected = MongoIndexOperationsService.Manifest
            .Single(item => item.Key == "customforms_channelid_url.unique");
        var matching = new BsonDocument
        {
            { "name", expected.Name },
            { "key", expected.Keys },
            { "unique", true },
            { "partialFilterExpression", expected.PartialFilter }
        };
        var wrongFilter = matching.DeepClone().AsBsonDocument;
        wrongFilter["partialFilterExpression"] = new BsonDocument("channelId", new BsonDocument("$exists", false));
        var missingFilter = matching.DeepClone().AsBsonDocument;
        missingFilter.Remove("partialFilterExpression");

        Assert.True(MongoIndexOperationsService.HasExpectedDefinition(matching, expected));
        Assert.False(MongoIndexOperationsService.HasExpectedDefinition(wrongFilter, expected));
        Assert.False(MongoIndexOperationsService.HasExpectedDefinition(missingFilter, expected));
    }

    [Fact]
    public void Unexpected_partial_filter_is_not_treated_as_an_exact_definition()
    {
        var expected = MongoIndexOperationsService.Manifest.Single(item => item.Key == "pages_url.unique");
        var withPartialFilter = new BsonDocument
        {
            { "name", expected.Name },
            { "key", expected.Keys },
            { "unique", true },
            { "partialFilterExpression", new BsonDocument("url", new BsonDocument("$exists", true)) }
        };

        Assert.False(MongoIndexOperationsService.HasExpectedDefinition(withPartialFilter, expected));
    }

    [Fact]
    public void Removal_decision_targets_only_the_legacy_index_name()
    {
        var removal = MongoIndexOperationsService.RemovalManifest.Single(item => item.Key == "pages_url");
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