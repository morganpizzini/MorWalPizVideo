using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class SocialPublishingServiceTests
{
    private const string ChannelId = "channel-one";
    private static readonly string EncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Secret_protector_round_trips_and_binds_ciphertext_to_channel_and_provider()
    {
        var protector = CreateProtector();

        var first = protector.Protect("secret-token", ChannelId, "telegram");
        var second = protector.Protect("secret-token", ChannelId, "telegram");

        Assert.NotEqual(first, second);
        Assert.Equal("secret-token", protector.Unprotect(first, ChannelId, "telegram"));
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(first, "channel-two", "telegram"));
        Assert.ThrowsAny<CryptographicException>(() => protector.Unprotect(first, ChannelId, "discord"));
    }

    [Fact]
    public void Selected_channel_accessor_reads_only_the_resolved_channel_configuration()
    {
        var protector = CreateProtector();
        var channel = new YTChannel(ChannelId, "Channel One")
        {
            SocialPublishing = new SocialPublishingConfiguration
            {
                Telegram = new SocialPublishingProviderConfiguration
                {
                    DestinationId = "telegram-chat",
                    CredentialCiphertext = protector.Protect("telegram-token", ChannelId, "telegram")
                }
            }
        };
        var context = new DefaultHttpContext();
        context.Items[ChannelContextConstants.ItemKey] = new ChannelContext(ChannelId, channel, true, false, true);
        var accessor = new SelectedChannelPublishingConfigurationAccessor(
            new HttpContextAccessor { HttpContext = context },
            protector);

        var credentials = accessor.Get("telegram");

        Assert.Equal("telegram-chat", credentials.DestinationId);
        Assert.Equal("telegram-token", credentials.Credential);
        Assert.Throws<SocialProviderNotConfiguredException>(() => accessor.Get("discord"));
    }

    [Fact]
    public async Task Telegram_service_uses_selected_channel_credentials()
    {
        var handler = new CapturingHandler();
        var service = new TelegramService(
            new TestHttpClientFactory(handler),
            new StaticConfigurationAccessor("telegram-chat", "telegram-token"),
            Configuration());

        var result = await service.CreatePost("abc12", "New video");

        Assert.Empty(result);
        Assert.Equal("https://api.telegram.org/bottelegram-token/sendMessage", handler.RequestUri);
        Assert.Contains("telegram-chat", handler.Content, StringComparison.Ordinal);
        Assert.Contains("New video https://site.example/sl/abc12", handler.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Discord_service_uses_selected_channel_destination_and_bot_token()
    {
        var handler = new CapturingHandler();
        var service = new DiscordService(
            new TestHttpClientFactory(handler),
            new StaticConfigurationAccessor("discord-channel", "discord-token"),
            Configuration());

        var result = await service.CreatePost("abc12", string.Empty);

        Assert.Empty(result);
        Assert.Equal("https://discord.com/api/channels/discord-channel/messages", handler.RequestUri);
        Assert.Equal("Bot", handler.AuthorizationScheme);
        Assert.Equal("discord-token", handler.AuthorizationParameter);
        Assert.Contains("Guarda il mio ultimo video: https://site.example/sl/abc12", handler.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Facebook_service_uses_selected_channel_page_and_access_token()
    {
        var handler = new CapturingHandler();
        var service = new FacebookService(
            new TestHttpClientFactory(handler),
            new StaticConfigurationAccessor("facebook-page", "facebook-token"),
            Configuration());

        var result = await service.CreatePost("abc12", "New video");

        Assert.Empty(result);
        Assert.Equal("https://graph.facebook.com/v23.0/facebook-page/feed", handler.RequestUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("facebook-token", handler.AuthorizationParameter);
    }

    [Fact]
    public async Task Unconfigured_provider_does_not_create_an_http_client()
    {
        var factory = new TestHttpClientFactory(new CapturingHandler());
        var service = new TelegramService(factory, new MissingConfigurationAccessor(), Configuration());

        await Assert.ThrowsAsync<SocialProviderNotConfiguredException>(() => service.CreatePost("abc12", "New video"));

        Assert.Equal(0, factory.CreatedClients);
    }

    private static SocialPublishingSecretProtector CreateProtector() =>
        new(Options.Create(new SocialPublishingOptions { EncryptionKey = EncryptionKey }));

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SiteUrl"] = "https://site.example/" })
            .Build();

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string RequestUri { get; private set; } = string.Empty;
        public string AuthorizationScheme { get; private set; } = string.Empty;
        public string AuthorizationParameter { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString() ?? string.Empty;
            AuthorizationScheme = request.Headers.Authorization?.Scheme ?? string.Empty;
            AuthorizationParameter = request.Headers.Authorization?.Parameter ?? string.Empty;
            Content = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { ok = true })
            };
        }
    }

    private sealed class TestHttpClientFactory(CapturingHandler handler) : IHttpClientFactory
    {
        public int CreatedClients { get; private set; }

        public HttpClient CreateClient(string name)
        {
            CreatedClients++;
            var client = new HttpClient(handler);
            if (name == HttpClientNames.Discord)
            {
                client.BaseAddress = new Uri("https://discord.com/api/");
            }
            else if (name == HttpClientNames.Facebook)
            {
                client.BaseAddress = new Uri("https://graph.facebook.com/v23.0/");
            }
            return client;
        }
    }

    private sealed class StaticConfigurationAccessor(string destinationId, string credential)
        : ISelectedChannelPublishingConfigurationAccessor
    {
        public SocialPublishingCredentials Get(string provider) => new(destinationId, credential);
    }

    private sealed class MissingConfigurationAccessor : ISelectedChannelPublishingConfigurationAccessor
    {
        public SocialPublishingCredentials Get(string provider) => throw new SocialProviderNotConfiguredException(provider);
    }
}