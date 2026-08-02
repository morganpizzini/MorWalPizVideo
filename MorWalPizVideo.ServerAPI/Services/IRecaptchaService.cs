namespace MorWalPizVideo.ServerAPI.Services;

/// <summary>
/// Verifies Google reCAPTCHA v3 tokens submitted by public-facing forms.
/// </summary>
public interface IRecaptchaService
{
    Task<bool> VerifyAsync(string token, string remoteIp, string expectedAction, CancellationToken ct = default);
}
