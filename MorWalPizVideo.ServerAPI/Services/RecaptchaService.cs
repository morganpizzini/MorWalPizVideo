using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Contracts;

namespace MorWalPizVideo.ServerAPI.Services;

public sealed class RecaptchaServiceMock : IRecaptchaService
{
    public Task<bool> VerifyAsync(string token, string remoteIp, string expectedAction, CancellationToken ct = default)
        => Task.FromResult(true);
}

public sealed class RecaptchaService : IRecaptchaService
{
    private readonly IHttpClientFactory clientFactory;
    private readonly IConfiguration configuration;

    public RecaptchaService(IHttpClientFactory _clientFactory, IConfiguration _configuration)
    {
        clientFactory = _clientFactory;
        configuration = _configuration;
    }

    public async Task<bool> VerifyAsync(string token, string remoteIp, string expectedAction, CancellationToken ct = default)
    {
        using var client = clientFactory.CreateClient(HttpClientNames.Recaptcha);
        var parameters = new Dictionary<string, string>
        {
            { "secret", configuration["RecaptchaSecretKey"] ?? string.Empty },
            { "response", token ?? string.Empty },
            { "remoteip", remoteIp ?? string.Empty }
        };

        var response = await client.PostAsync("", new FormUrlEncodedContent(parameters), ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var result = System.Text.Json.JsonSerializer.Deserialize<RecaptchaResponse>(responseContent);

        return result != null && result.success && result.action == expectedAction;
    }
}
