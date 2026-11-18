using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MorWalPizVideo.ShootingRange.Models;
using MorWalPizVideo.ShootingRange.Repositories;

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
    public void Legacy_entity_defaults_and_opaque_ids_remain_compatible()
    {
        var user = new ShootingRangeUser { Id = "legacy", Username = "legacy", Status = ShootingRangeAccountStatus.Approved };
        user.SecurityVersion.Should().BeEmpty();
        user.NormalizedUsername.Should().BeEmpty();
        var session = new ShootingRangeLocalSession { Id = new string('A', 64), UserId = "legacy", CreationDateTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), ExpiresUtc = new DateTime(2030, 1, 1, 8, 0, 0, DateTimeKind.Utc) };
        session.Id.Should().HaveLength(64);
        ShootingRangeIds.NewId().Should().MatchRegex("^[0-9a-f]{24}$");
    }

    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "SecurityTests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}