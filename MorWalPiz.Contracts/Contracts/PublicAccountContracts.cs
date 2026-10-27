using System.ComponentModel.DataAnnotations;

namespace MorWalPiz.Contracts.Contracts;

public sealed record PublicAccountRegistrationRequest
{
    [Required, StringLength(40, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [StringLength(120)]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed record PublicAccountLoginRequest
{
    [Required, StringLength(256)]
    public string Identifier { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

public sealed record PublicAccountProfileUpdateRequest
{
    [Required, StringLength(120)]
    public string DisplayName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;
}

public sealed record PublicAccountSummaryContract
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed record PublicAccountSessionContract
{
    public bool Authenticated { get; init; }
    public PublicAccountSummaryContract? Account { get; init; }
}

public sealed record PublicAccountCsrfContract
{
    public string Token { get; init; } = string.Empty;
}
