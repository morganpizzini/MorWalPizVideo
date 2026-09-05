namespace MorWalPiz.Contracts;

public sealed record NewsletterSubscribeRequest(string ChannelId, string Email, string Language, string RecaptchaToken);
public sealed record NewsletterSubscribeResponse(string Message);
public sealed record NewsletterTokenRequest(string ChannelId, string Token);
public sealed record NewsletterConfirmationResponse(string UnsubscribeToken, string UnsubscribeLink);
public sealed record NewsletterCreateRequest(
    string Name,
    string SubjectIt,
    string SubjectEng,
    string TemplateId,
    int TemplateVersion,
    IReadOnlyList<NewsletterSectionContract> Sections);
public sealed record NewsletterSectionContract(string Type, string? Title, string? Body, string? ImageUrl, string? ShortLinkCode);
public sealed record NewsletterStateRequest(string State);
public sealed record NewsletterContract(string Id, string ChannelId, string Name, string SubjectIt, string SubjectEng, string TemplateId, int TemplateVersion, string State);