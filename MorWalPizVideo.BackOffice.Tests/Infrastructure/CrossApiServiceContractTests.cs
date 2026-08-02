using System.Net;
using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Configuration;
using MorWalPizVideo.Models.Constraints;

namespace MorWalPizVideo.BackOffice.Tests.Infrastructure;

public class CrossApiServiceContractTests
{
    private static (CrossApiService sut, StubHttpMessageHandler handler) BuildSut()
    {
        var handler = new StubHttpMessageHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/api/") };
        var factory = HttpClientFactoryStub.WithClient(HttpClientNames.MorWalPiz, client);
        return (new CrossApiService(factory, Options.Create(new InternalServiceSettings())), handler);
    }

    [Fact]
    public async Task PurgeCache_uses_query_string_contract()
    {
        var (sut, handler) = BuildSut();

        await sut.PurgeCache("tag-biolinks");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://example.test/api/cache/purge?k=tag-biolinks", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task ResetCache_uses_query_string_contract()
    {
        var (sut, handler) = BuildSut();

        await sut.ResetCache("matches");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://example.test/api/cache/reset?k=matches", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task PurgeCache_url_encodes_key()
    {
        var (sut, handler) = BuildSut();

        await sut.PurgeCache("a/b c");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://example.test/api/cache/purge?k=a%2Fb%20c", handler.LastRequest!.RequestUri!.OriginalString);
    }
}
