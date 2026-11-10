using System.Linq.Expressions;
using System.Security.Claims;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Services;

[Trait("Category", "TestGroup:BackOffice")]
public sealed class VideoAuthorizationServiceTests
{
    private const string ChannelId = "selected-channel";

    [Theory]
    [InlineData("owner", true, 1, 1)]
    [InlineData("admin-group", true, 1, 0)]
    [InlineData("admin-permission", true, 1, 0)]
    [InlineData("admin-claim", true, 1, 0)]
    [InlineData("api-key", true, 1, 0)]
    [InlineData("missing-id", false, 0, 0)]
    [InlineData("inactive-owner", false, 1, 1)]
    [InlineData("other-channel-owner", false, 1, 1)]
    [InlineData("missing-profile", false, 1, 1)]
    [InlineData("impersonated-owner", true, 1, 1)]
    [InlineData("impersonated-non-owner", false, 1, 1)]
    public async Task Bulk_and_scalar_reads_preserve_channel_policy(
        string scenario, bool allowed, int resolutions, int ownershipReads)
    {
        var claims = new List<Claim>();
        if (scenario != "missing-id")
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "actor"));
        if (scenario.StartsWith("impersonated-", StringComparison.Ordinal))
        {
            claims.Add(new Claim("target_user_id", "target"));
            claims.Add(new Claim("impersonation", "true"));
            claims.Add(new Claim("permission", AuthorizationPermissionKeys.BackofficeManageAll));
        }
        if (scenario == "admin-claim")
            claims.Add(new Claim("permission", AuthorizationPermissionKeys.BackofficeManageAll));
        if (scenario == "api-key")
            claims.Add(new Claim("ApiKeyId", "key"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var effectiveId = scenario.StartsWith("impersonated-", StringComparison.Ordinal) ? "target" : "actor";
        var profile = new UserAccessProfile(
            new User { Id = effectiveId },
            new HashSet<string>(scenario == "admin-group" ? [AuthorizationGroupCodes.Admin] : []),
            new HashSet<string>(),
            new HashSet<string>(scenario == "admin-permission" ? [AuthorizationPermissionKeys.BackofficeManageAll] : []));
        var resolver = new CountingAccessResolver(scenario == "missing-profile" ? null : profile);
        var owners = new CountingOwnerRepository(
        [
            new UserChannelOwner
            {
                UserId = scenario == "impersonated-non-owner" ? "actor" : effectiveId,
                ChannelId = scenario == "other-channel-owner" ? "another-channel" : ChannelId,
                IsActive = scenario != "inactive-owner" && scenario != "missing-profile"
            }
        ]);
        var service = new VideoAuthorizationService(resolver, owners);
        var owned = YouTubeContent.CreateSingleVideo("owned", []) with { OwnerChannelId = ChannelId };
        var referenced = YouTubeContent.CreateSingleVideo("referenced", []) with
        {
            VideoRefs = [new VideoRef("video", channelIds: [ChannelId])]
        };
        var unrelatedCreator = YouTubeContent.CreateSingleVideo("unrelated", []) with { CreatorUserId = effectiveId };
        var caseMismatch = owned with { Id = "case-mismatch", OwnerChannelId = ChannelId.ToUpperInvariant() };
        YouTubeContent[] matches = [owned, unrelatedCreator, referenced, caseMismatch, owned];

        var result = await service.CanReadInChannelAsync(principal, matches, ChannelId);

        Assert.Equal(allowed ? new[] { owned, referenced, owned } : [], result);
        Assert.Equal(resolutions, resolver.UserIds.Count);
        Assert.Equal(ownershipReads, owners.UserIds.Count);
        Assert.All(resolver.UserIds, id => Assert.Equal(effectiveId, id));
        Assert.All(owners.UserIds, id => Assert.Equal(effectiveId, id));
        foreach (var match in matches)
            Assert.Equal(allowed && (match == owned || match == referenced),
                await service.CanReadInChannelAsync(principal, match, ChannelId));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(50, false)]
    [InlineData(200, false)]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(200, true)]
    public async Task Bulk_dependency_calls_do_not_grow_with_match_count(int count, bool admin)
    {
        var resolver = new CountingAccessResolver(new UserAccessProfile(
            new User { Id = "user" },
            new HashSet<string>(admin ? [AuthorizationGroupCodes.Admin] : []),
            new HashSet<string>(),
            new HashSet<string>()));
        var owners = new CountingOwnerRepository(
            [new UserChannelOwner { UserId = "user", ChannelId = ChannelId }]);
        var service = new VideoAuthorizationService(resolver, owners);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "Test"));
        var matches = Enumerable.Range(0, count).Select(index =>
            YouTubeContent.CreateSingleVideo($"video-{index}", []) with { OwnerChannelId = ChannelId }).ToArray();

        var result = await service.CanReadInChannelAsync(principal, matches, ChannelId);

        Assert.Equal(matches, result);
        Assert.Single(resolver.UserIds);
        Assert.Equal(admin ? 0 : 1, owners.UserIds.Count);
    }

    [Fact]
    public async Task Empty_bulk_read_does_not_resolve_access_or_ownership()
    {
        var service = new VideoAuthorizationService(null!, null!);

        Assert.Empty(await service.CanReadInChannelAsync(new ClaimsPrincipal(), Array.Empty<YouTubeContent>(), ChannelId));
    }

    [Fact]
    public async Task Bulk_read_propagates_access_resolution_failure()
    {
        var resolver = new CountingAccessResolver(null) { Failure = new InvalidOperationException("Access unavailable") };
        var service = new VideoAuthorizationService(resolver, null!);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "Test"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CanReadInChannelAsync(principal, new[] { YouTubeContent.CreateSingleVideo("video", []) }, ChannelId));

        Assert.Same(resolver.Failure, exception);
    }

    private sealed class CountingAccessResolver(UserAccessProfile? profile) : IUserAccessResolver
    {
        public List<string> UserIds { get; } = [];
        public Exception? Failure { get; init; }

        public Task<UserAccessProfile?> ResolveAsync(string userId)
        {
            UserIds.Add(userId);
            if (Failure is not null)
                throw Failure;
            return Task.FromResult(profile);
        }
    }

    private sealed class CountingOwnerRepository(IList<UserChannelOwner> owners) : IUserChannelOwnerRepository
    {
        public List<string> UserIds { get; } = [];

        public Task<IList<UserChannelOwner>> GetByUserIdAsync(string userId)
        {
            UserIds.Add(userId);
            return Task.FromResult<IList<UserChannelOwner>>(owners.Where(owner => owner.UserId == userId).ToList());
        }

        public Task<IList<UserChannelOwner>> GetByChannelIdAsync(string channelId) => throw new NotSupportedException();
        public Task<UserChannelOwner> GetItemAsync(string id) => throw new NotSupportedException();
        public Task<IList<UserChannelOwner>> GetItemsAsync() => throw new NotSupportedException();
        public Task<IList<UserChannelOwner>> GetItemsAsync(Expression<Func<UserChannelOwner, bool>> predicate) => throw new NotSupportedException();
        public Task<UserChannelOwner> AddItemAsync(UserChannelOwner item) => throw new NotSupportedException();
        public Task UpdateItemAsync(UserChannelOwner item) => throw new NotSupportedException();
        public Task DeleteItemAsync(string id) => throw new NotSupportedException();
    }
}
