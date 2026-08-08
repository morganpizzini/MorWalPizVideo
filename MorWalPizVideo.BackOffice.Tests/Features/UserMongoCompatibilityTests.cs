using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MorWalPiz.Contracts.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class UserMongoCompatibilityTests : IClassFixture<LegacyUserBackOfficeWebApplicationFactory>
{
    private readonly LegacyUserBackOfficeWebApplicationFactory _factory;

    public UserMongoCompatibilityTests(LegacyUserBackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Profile_returns_ok_for_legacy_user_with_object_id_document_id()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", LegacyUserRepository.UserId);

        var response = await client.GetAsync("/api/User/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserContract>();
        Assert.NotNull(profile);
        Assert.Equal(LegacyUserRepository.UserId, profile!.Id);
    }
}

public sealed class LegacyUserBackOfficeWebApplicationFactory : BackOfficeWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository, LegacyUserRepository>();
        });
    }
}

internal sealed class LegacyUserRepository : IUserRepository
{
    public static readonly string UserId = ObjectId.GenerateNewId().ToString();

    private readonly User user = BsonSerializer.Deserialize<User>(new BsonDocument
    {
        { "_id", ObjectId.Parse(UserId) },
        { "creationDateTime", DateTime.UtcNow },
        { "username", "legacy-profile-user" },
        { "email", "legacy-profile-user@example.test" },
        { "passwordHash", "legacy-hash" },
        { "salt", "legacy-salt" },
        { "isActive", true },
        { "directPermissions", new BsonArray { AuthorizationPermissionKeys.BackofficeAccess } },
        { "groupIds", new BsonArray() },
        { "canAccessBackoffice", false }
    });

    public Task<User> AddItemAsync(User item) => throw new NotSupportedException();

    public Task DeleteItemAsync(string id) => throw new NotSupportedException();

    public Task<User> GetItemAsync(string id) =>
        Task.FromResult(string.Equals(user.Id, id, StringComparison.OrdinalIgnoreCase) ? user : null!);

    public Task<IList<User>> GetItemsAsync() => Task.FromResult<IList<User>>([user]);

    public Task<IList<User>> GetItemsAsync(Expression<Func<User, bool>> predicate) =>
        throw new InvalidOperationException("User lookups must use the ObjectId-aware repository path.");

    public Task UpdateItemAsync(User item) => throw new NotSupportedException();

    public Task<User?> AuthenticateAsync(string username, string password) => throw new NotSupportedException();
}