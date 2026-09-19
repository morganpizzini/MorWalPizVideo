using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MorWalPiz.VideoImporter.Services;

public sealed class FakeApiServiceFactory(string scenario) : IApiServiceFactory
{
    public ApiService Create(string apiEndpoint, string? apiKey = null, string? channelId = null)
    {
        var client = new HttpClient(new FakeApiMessageHandler(scenario))
        {
            BaseAddress = new Uri(apiEndpoint, UriKind.Absolute)
        };
        return new ApiService(client);
    }
}

internal sealed class FakeApiMessageHandler(string scenario) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var normalizedScenario = scenario.Trim().ToLowerInvariant();
        var status = normalizedScenario switch
        {
            "auth" => HttpStatusCode.Unauthorized,
            "transient" => HttpStatusCode.ServiceUnavailable,
            "permanent" => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.OK
        };
        var body = normalizedScenario == "success" && request.RequestUri?.AbsolutePath.EndsWith("accessible", StringComparison.OrdinalIgnoreCase) == true
            ? "[]"
            : normalizedScenario == "success" && request.RequestUri?.AbsolutePath.EndsWith("translate", StringComparison.OrdinalIgnoreCase) == true
                ? "[]"
                : "{}";
        return Task.FromResult(new HttpResponseMessage(status)
        {
            RequestMessage = request,
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}