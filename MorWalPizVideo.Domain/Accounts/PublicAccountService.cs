using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MorWalPizVideo.Domain.Interfaces;
using MorWalPizVideo.Domain.Security;
using MorWalPizVideo.Models.Models;
using MorWalPizVideo.Server.Services.Interfaces;

namespace MorWalPizVideo.Domain.Accounts;

public sealed record PublicAccountAuthenticationResult(PublicAccount Account, string SessionToken);

public sealed record PublicAccountRegistrationCommand(
    string Username,
    string Email,
    string Password,
    string DisplayName,
    bool TermsAccepted,
    bool PrivacyNoticeAcknowledged);
public sealed record PublicAccountLoginCommand(string Identifier, string Password);
public sealed record PublicAccountProfileUpdateCommand(string DisplayName, string Email);

public sealed record PublicAccountSessionResult(PublicAccount Account, string TokenHash);

public interface IPublicAccountService
{
    Task<PublicAccountAuthenticationResult?> RegisterAsync(PublicAccountRegistrationCommand request, CancellationToken cancellationToken = default);
    Task<PublicAccountAuthenticationResult?> LoginAsync(PublicAccountLoginCommand request, CancellationToken cancellationToken = default);
    Task<PublicAccountSessionResult?> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<PublicAccount?> UpdateProfileAsync(string accountId, PublicAccountProfileUpdateCommand request, CancellationToken cancellationToken = default);
    Task<PublicAccount?> GetAccountAsync(string accountId, CancellationToken cancellationToken = default);
}

public sealed class PublicAccountService(IPublicAccountRepository repository) : IPublicAccountService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    public const string TermsVersion = "2026-10-27";
    public const string PrivacyNoticeVersion = "2026-10-27";

    public async Task<PublicAccountAuthenticationResult?> RegisterAsync(
        PublicAccountRegistrationCommand request,
        CancellationToken cancellationToken = default)
    {
        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);
        if (username is null || email is null || string.IsNullOrWhiteSpace(request.Password) ||
            !request.TermsAccepted || !request.PrivacyNoticeAcknowledged)
            return null;

        if (await repository.FindByUsernameAsync(username, cancellationToken) is not null ||
            await repository.FindByEmailAsync(email, cancellationToken) is not null)
            throw new PublicAccountConflictException();

        var passwordHash = PasswordHashing.HashPassword(request.Password, out var salt);
        var account = new PublicAccount
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = request.Username.Trim(),
            NormalizedUsername = username,
            Email = request.Email.Trim(),
            NormalizedEmail = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username.Trim() : request.DisplayName.Trim(),
            PasswordHash = passwordHash,
            Salt = salt,
            CreationDateTime = DateTime.UtcNow,
            TermsVersionAccepted = TermsVersion,
            TermsAcceptedAtUtc = DateTime.UtcNow,
            PrivacyNoticeVersionAcknowledged = PrivacyNoticeVersion,
            PrivacyNoticeAcknowledgedAtUtc = DateTime.UtcNow
        };
        await repository.AddAccountAsync(account, cancellationToken);
        return await CreateSessionAsync(account, cancellationToken);
    }

    public async Task<PublicAccountAuthenticationResult?> LoginAsync(
        PublicAccountLoginCommand request,
        CancellationToken cancellationToken = default)
    {
        var identifier = request.Identifier.Trim();
        var normalized = identifier.Contains('@', StringComparison.Ordinal)
            ? NormalizeEmail(identifier)
            : NormalizeUsername(identifier);
        if (normalized is null)
            return null;

        var account = identifier.Contains('@', StringComparison.Ordinal)
            ? await repository.FindByEmailAsync(normalized, cancellationToken)
            : await repository.FindByUsernameAsync(normalized, cancellationToken);
        if (account is null || !account.IsActive ||
            !PasswordHashing.VerifyPassword(request.Password, account.PasswordHash, account.Salt))
            return null;

        return await CreateSessionAsync(account, cancellationToken);
    }

    public async Task<PublicAccountSessionResult?> ValidateSessionAsync(
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return null;

        var tokenHash = HashToken(sessionToken);
        var now = DateTime.UtcNow;
        var session = await repository.FindActiveSessionAsync(tokenHash, now, cancellationToken);
        if (session is null)
            return null;

        var account = await GetAccountAsync(session.AccountId, cancellationToken);
        return account is { IsActive: true } activeAccount && activeAccount.SecurityVersion == session.SecurityVersion
            ? new PublicAccountSessionResult(activeAccount, tokenHash)
            : null;
    }

    public Task RevokeSessionAsync(string sessionToken, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(sessionToken)
            ? Task.CompletedTask
            : repository.RevokeSessionAsync(HashToken(sessionToken), DateTime.UtcNow, cancellationToken);

    public async Task<PublicAccount?> UpdateProfileAsync(
        string accountId,
        PublicAccountProfileUpdateCommand request,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountAsync(accountId, cancellationToken);
        if (account is null)
            return null;

        var email = NormalizeEmail(request.Email);
        if (email is null)
            return null;
        var existing = await repository.FindByEmailAsync(email, cancellationToken);
        if (existing is not null && existing.Id != account.Id)
            throw new PublicAccountConflictException();

        var updated = account with
        {
            Email = request.Email.Trim(),
            NormalizedEmail = email,
            DisplayName = request.DisplayName.Trim()
        };
        await repository.UpdateAccountAsync(updated, cancellationToken);
        return updated;
    }

    public async Task<PublicAccount?> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
        => await repository.GetAccountAsync(accountId, cancellationToken);

    private async Task<PublicAccountAuthenticationResult> CreateSessionAsync(
        PublicAccount account,
        CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await repository.AddSessionAsync(new PublicAccountSession
        {
            Id = ObjectId.GenerateNewId().ToString(),
            AccountId = account.Id,
            TokenHash = HashToken(token),
            SecurityVersion = account.SecurityVersion,
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime),
            CreationDateTime = DateTime.UtcNow
        }, cancellationToken);
        return new PublicAccountAuthenticationResult(account, token);
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string? NormalizeUsername(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized.Length is >= 3 and <= 40 && normalized.All(char.IsLetterOrDigit) ? normalized : null;
    }

    public static string? NormalizeEmail(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized.Length is > 3 and <= 256 && normalized.Contains('@', StringComparison.Ordinal) ? normalized : null;
    }
}

public sealed class PublicAccountConflictException : Exception { }
