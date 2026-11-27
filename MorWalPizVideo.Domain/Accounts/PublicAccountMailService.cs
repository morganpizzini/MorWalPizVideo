using System.Security.Cryptography;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Accounts;

public sealed record PublicAccountMailLink(PublicAccount Account, string Token);

public sealed class PublicAccountMailService(IPublicAccountRepository repository, TimeProvider clock)
{
    public async Task<PublicAccountMailLink?> IssueAsync(string email, PublicAccountMailPurpose purpose,
        bool onlyIfExpired = false, CancellationToken cancellationToken = default)
    {
        var normalized = PublicAccountService.NormalizeEmail(email);
        if (normalized is null) return null;
        var account = await repository.FindByEmailAsync(normalized, cancellationToken);
        if (account is null || !account.IsActive || (purpose == PublicAccountMailPurpose.EmailConfirmation && account.EmailConfirmed))
            return null;
        var now = clock.GetUtcNow().UtcDateTime;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expires = now.Add(purpose == PublicAccountMailPurpose.PasswordReset ? TimeSpan.FromMinutes(30) : TimeSpan.FromHours(24));
        return await repository.IssueMailTokenAsync(account.Id, normalized, account.SecurityVersion, purpose,
            PublicAccountService.HashToken(token), expires, now, onlyIfExpired, cancellationToken)
            ? new PublicAccountMailLink(account, token) : null;
    }

    public async Task<bool> ConsumeAsync(string accountId, string token, PublicAccountMailPurpose purpose,
        string? password = null, CancellationToken cancellationToken = default)
    {
        if (token.Length != 64 || !token.All(char.IsAsciiHexDigit) ||
            (purpose == PublicAccountMailPurpose.PasswordReset &&
             (string.IsNullOrWhiteSpace(password) || password.Length is < 8 or > 128)))
            return false;
        var account = await repository.GetAccountAsync(accountId, cancellationToken);
        if (account is null || !account.IsActive) return false;
        string? hash = null;
        string? salt = null;
        if (purpose == PublicAccountMailPurpose.PasswordReset)
            hash = PasswordHashing.HashPassword(password!, out salt);
        return await repository.ConsumeMailTokenAsync(accountId, account.NormalizedEmail, account.SecurityVersion,
            purpose, PublicAccountService.HashToken(token), clock.GetUtcNow().UtcDateTime, hash, salt, cancellationToken);
    }
}
