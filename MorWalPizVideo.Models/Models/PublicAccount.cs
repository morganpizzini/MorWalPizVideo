using MongoDB.Bson.Serialization.Attributes;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.Models.Models;

public sealed record PublicAccount : BaseEntity
{
    [BsonElement("username")]
    public string Username { get; init; } = string.Empty;

    [BsonElement("normalizedUsername")]
    public string NormalizedUsername { get; init; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; init; } = string.Empty;

    [BsonElement("normalizedEmail")]
    public string NormalizedEmail { get; init; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; init; } = string.Empty;

    [BsonElement("salt")]
    public string Salt { get; init; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; init; } = true;

    [BsonElement("securityVersion")]
    public int SecurityVersion { get; init; }

    [BsonElement("emailConfirmationRequired")]
    public bool EmailConfirmationRequired { get; init; }

    [BsonElement("emailConfirmedAtUtc")]
    public DateTime? EmailConfirmedAtUtc { get; init; }

    [BsonElement("confirmedEmail")]
    public string? ConfirmedEmail { get; init; }

    [BsonIgnore]
    public bool EmailConfirmed => EmailConfirmedAtUtc is not null && ConfirmedEmail == NormalizedEmail;

    [BsonElement("confirmationTokenHash")]
    public string? ConfirmationTokenHash { get; init; }

    [BsonElement("confirmationExpiresAtUtc")]
    public DateTime? ConfirmationExpiresAtUtc { get; init; }

    [BsonElement("resetTokenHash")]
    public string? ResetTokenHash { get; init; }

    [BsonElement("resetExpiresAtUtc")]
    public DateTime? ResetExpiresAtUtc { get; init; }

    [BsonElement("resetSecurityVersion")]
    public int? ResetSecurityVersion { get; init; }

    [BsonElement("termsVersionAccepted")]
    public string TermsVersionAccepted { get; init; } = string.Empty;

    [BsonElement("termsAcceptedAtUtc")]
    public DateTime? TermsAcceptedAtUtc { get; init; }

    [BsonElement("privacyNoticeVersionAcknowledged")]
    public string PrivacyNoticeVersionAcknowledged { get; init; } = string.Empty;

    [BsonElement("privacyNoticeAcknowledgedAtUtc")]
    public DateTime? PrivacyNoticeAcknowledgedAtUtc { get; init; }

    [BsonElement("workshopCapabilityGuideSeenVersion")]
    public int WorkshopCapabilityGuideSeenVersion { get; init; } = 1;
}

public enum PublicAccountMailPurpose { PasswordReset, EmailConfirmation }

public sealed record PublicAccountSession : BaseEntity
{
    [BsonElement("accountId")]
    public string AccountId { get; init; } = string.Empty;

    [BsonElement("tokenHash")]
    public string TokenHash { get; init; } = string.Empty;

    [BsonElement("securityVersion")]
    public int SecurityVersion { get; init; }

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; init; }

    [BsonElement("revokedAt")]
    public DateTime? RevokedAt { get; init; }
}
