using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;
using MorWalPizVideo.ShootingRange.Security;

namespace MorWalPizVideo.ShootingRange.Tests.Services;

[Trait("Category", "TestGroup:ShootingRange")]
public sealed class ShootingRangeSecurityFoundationTests
{
    [Theory]
    [InlineData("Development", true, false)]
    [InlineData("Test", true, false)]
    [InlineData("Production", true, true)]
    [InlineData("Staging", true, true)]
    [InlineData("Production", false, false)]
    public void Mock_mode_is_closed_outside_explicit_local_environments(string name, bool mock, bool rejected)
    {
        Action validate = () => global::StartupConfigurationValidation.ValidateMockMode(mock, new EnvironmentStub(name));
        if (rejected) validate.Should().Throw<InvalidOperationException>();
        else validate.Should().NotThrow();
    }

    [Fact]
    public async Task Generic_writes_enforce_normalized_username_uniqueness_and_rollback()
    {
        var repositories = new MockShootingRangeRepositorySet();
        var first = await repositories.Users.InsertAsync(new() { Id = "one", Username = " Alice " });
        first.NormalizedUsername.Should().Be("alice");
        (await repositories.Users.FindByUsernameAsync("ALICE"))!.Id.Should().Be("one");
        var insert = () => repositories.Users.InsertAsync(new() { Id = "two", Username = "alice" });
        await insert.Should().ThrowAsync<InvalidOperationException>();
        var second = await repositories.Users.InsertAsync(new() { Id = "two", Username = "bob" });
        var replace = () => repositories.Users.ReplaceAsync(second with { Username = "ALICE" });
        await replace.Should().ThrowAsync<InvalidOperationException>();
        (await repositories.Users.GetAsync("two"))!.Username.Should().Be("bob");
    }

    [Fact]
    public void Duplicate_preflight_is_read_only_and_legacy_fields_require_reviewed_backfill()
    {
        var users = new[] { new ShootingRangeUser { Id = "one", Username = " Alice " }, new ShootingRangeUser { Id = "two", Username = "ALICE" } };
        Action duplicate = () => ShootingRangeWriteValidation.ValidateUsernamePreflight(users);
        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*duplicate*");
        Action missing = () => ShootingRangeWriteValidation.ValidateUsernamePreflight(users.Take(1).ToArray());
        missing.Should().Throw<InvalidOperationException>().WithMessage("*backfill*");
        users[0].NormalizedUsername.Should().BeEmpty();
        ShootingRangeWriteValidation.ValidateUsernamePreflight([users[0] with { NormalizedUsername = "alice" }]);
    }

    [Fact]
    public void Legacy_bson_defaults_and_session_fields_remain_additive()
    {
        var user = BsonSerializer.Deserialize<ShootingRangeUser>(new BsonDocument { ["_id"] = "legacy", ["username"] = "legacy", ["status"] = 1 });
        user.SecurityVersion.Should().BeEmpty();
        user.NormalizedUsername.Should().BeEmpty();
        var session = new ShootingRangeLocalSession { Id = new string('A', 64), UserId = "legacy", CreationDateTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), ExpiresUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc) };
        BsonSerializer.Deserialize<ShootingRangeLocalSession>(session.ToBsonDocument()).Should().Be(session);
    }

    [Fact]
    public void Readiness_requires_writable_transaction_topology_and_logical_sessions()
    {
        ShootingRangeMongoReadiness.IsTransactionTopology(new BsonDocument { ["isWritablePrimary"] = true }).Should().BeFalse();
        ShootingRangeMongoReadiness.IsTransactionTopology(new BsonDocument { ["isWritablePrimary"] = true, ["setName"] = "rs0", ["logicalSessionTimeoutMinutes"] = 30 }).Should().BeTrue();
        ShootingRangeMongoReadiness.IsTransactionTopology(new BsonDocument { ["isWritablePrimary"] = false, ["setName"] = "rs0", ["logicalSessionTimeoutMinutes"] = 30 }).Should().BeFalse();
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "SecurityTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}