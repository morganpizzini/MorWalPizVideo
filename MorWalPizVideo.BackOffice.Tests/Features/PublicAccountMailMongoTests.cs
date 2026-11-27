using MongoDB.Driver;
using MorWalPizVideo.Domain.Accounts;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class AccountMailMongoFactAttribute : FactAttribute
{
    public AccountMailMongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ACCOUNT_MAIL_TEST_MONGO_URI")))
            Skip = "Set ACCOUNT_MAIL_TEST_MONGO_URI to an explicitly disposable MongoDB instance.";
    }
}

public sealed class PublicAccountMailMongoTests
{
    [AccountMailMongoFact]
    public async Task Mongo_profile_pipeline_preserves_same_email_tokens_and_consumes_reset_once()
    {
        var client = new MongoClient(Environment.GetEnvironmentVariable("ACCOUNT_MAIL_TEST_MONGO_URI"));
        var databaseName = $"account_mail_test_{Guid.NewGuid():N}";
        try
        {
            var repository = new PublicAccountRepository(client.GetDatabase(databaseName));
            var accounts = new PublicAccountService(repository);
            var account = (await accounts.RegisterPendingAsync(new("mongomail", "mongo@example.test", "original-password", "", true, true)))!;
            var mail = new PublicAccountMailService(repository, TimeProvider.System);
            var confirmation = (await mail.IssueAsync(account.Email, PublicAccountMailPurpose.EmailConfirmation))!;
            await repository.UpdateProfileAsync(account.Id, account.Email, account.NormalizedEmail, "$literal display name");
            Assert.True(await mail.ConsumeAsync(account.Id, confirmation.Token, PublicAccountMailPurpose.EmailConfirmation));
            var login = (await accounts.LoginAsync(new(account.Email, "original-password")))!;
            var reset = (await mail.IssueAsync(account.Email, PublicAccountMailPurpose.PasswordReset))!;
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
                mail.ConsumeAsync(account.Id, reset.Token, PublicAccountMailPurpose.PasswordReset, "replacement-password")));
            Assert.Equal(1, results.Count(success => success));
            Assert.Null(await accounts.ValidateSessionAsync(login.SessionToken));
            var updated = (await repository.GetAccountAsync(account.Id))!;
            Assert.Equal("$literal display name", updated.DisplayName);
            Assert.Equal(1, updated.SecurityVersion);
            Assert.True(updated.EmailConfirmed);
            var oldReset = (await mail.IssueAsync(account.Email, PublicAccountMailPurpose.PasswordReset))!;
            await repository.UpdateProfileAsync(account.Id, "new@example.test", "NEW@EXAMPLE.TEST", "Changed");
            Assert.False(await mail.ConsumeAsync(account.Id, oldReset.Token, PublicAccountMailPurpose.PasswordReset, "other-password"));
            Assert.False((await repository.GetAccountAsync(account.Id))!.EmailConfirmed);
            Assert.Null(await accounts.LoginAsync(new("new@example.test", "replacement-password")));
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }
}
