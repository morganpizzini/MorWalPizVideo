using System.Net;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HttpRequestMessage? LastRequest { get; private set; }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        _responder = responder ?? (_ => new HttpResponseMessage(HttpStatusCode.NoContent) { Content = new StringContent(string.Empty) });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_responder(request));
    }
}

public class HttpClientFactoryStub : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new(StringComparer.Ordinal);

    public HttpClient CreateClient(string name) =>
        _clients.TryGetValue(name, out var client)
            ? client
            : throw new InvalidOperationException($"No HttpClient registered for name '{name}'.");

    public static HttpClientFactoryStub WithClient(string name, HttpClient client)
    {
        var stub = new HttpClientFactoryStub();
        stub._clients[name] = client;
        return stub;
    }

    public HttpClientFactoryStub Add(string name, HttpClient client)
    {
        _clients[name] = client;
        return this;
    }
}
