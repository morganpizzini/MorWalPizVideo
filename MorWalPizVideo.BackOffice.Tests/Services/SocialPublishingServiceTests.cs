using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.BackOffice.Services.Configuration;
using MorWalPizVideo.BackOffice.Services.Factories;

namespace MorWalPizVideo.BackOffice.Tests.Services;

public sealed class SocialPublishingServiceTests
{
    [Fact]
    public void Telegram_configuration_service_reads_named_hierarchical_options()
    {
        var service = TelegramConfigurationService(new Dictionary<string, string?>
        {
            ["TelegramSettings:Token"] = "telegram-token",
            ["TelegramSettings:ChannelName"] = "telegram-channel"
        });

        var settings = service.GetTelegramSettings();

        Assert.Equal("telegram-token", settings.Token);
        Assert.Equal("telegram-channel", settings.ChannelName);
    }

    [Fact]
    public void Telegram_configuration_service_rejects_missing_token()
    {
        var service = TelegramConfigurationService(new Dictionary<string, string?>
        {
            ["TelegramSettings:ChannelName"] = "telegram-channel"
        });

        var exception = Assert.Throws<InvalidOperationException>(service.GetTelegramSettings);

        Assert.Contains("Telegram configuration is not properly set", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Discord_configuration_service_reads_named_hierarchical_options()
    {
        var service = DiscordConfigurationService(new Dictionary<string, string?>
        {
            ["DiscordSettings:Token"] = "discord-token",
            ["DiscordSettings:ChannelName"] = "discord-channel"
        });

        var settings = service.GetDiscordSettings();

        Assert.Equal("discord-token", settings.Token);
        Assert.Equal("discord-channel", settings.ChannelName);
    }

    [Fact]
    public void Discord_configuration_service_rejects_missing_token()
    {
        var service = DiscordConfigurationService(new Dictionary<string, string?>
        {
            ["DiscordSettings:ChannelName"] = "discord-channel"
        });

        var exception = Assert.Throws<InvalidOperationException>(service.GetDiscordSettings);

        Assert.Contains("Discord configuration is not properly set", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Telegram_service_uses_configured_factory_client()
    {
        var handler = new CapturingHandler();
        var client = new HttpClient(handler);
        var service = new TelegramService(
            new TelegramClientFactory(client),
            new TelegramConfiguration("telegram-channel"),
            Configuration("https://site.example/"));

        var result = await service.CreatePost("abc12", "New video");

        Assert.Empty(result);
        Assert.Equal("https://api.example/telegram/sendMessage", handler.Request!.RequestUri!.ToString());
        Assert.Contains("New video https://site.example/sl/abc12", await handler.Request.Content!.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Discord_service_uses_configured_factory_client()
    {
        var handler = new CapturingHandler();
        var client = new HttpClient(handler);
        var service = new DiscordService(
            new DiscordClientFactory(client),
            new DiscordConfiguration("discord-channel"),
            Configuration("https://site.example/"));

        var result = await service.CreatePost("abc12", string.Empty);

        Assert.Empty(result);
        Assert.Equal("https://api.example/discord/channels/discord-channel/messages", handler.Request!.RequestUri!.ToString());
        Assert.Contains("Guarda il mio ultimo video: https://site.example/sl/abc12", await handler.Request.Content!.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private static IConfiguration Configuration(string siteUrl)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SiteUrl"] = siteUrl })
            .Build();

    private static ITelegramConfigurationService TelegramConfigurationService(
        Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddOptions<global::TelegramSettings>("TelegramSettings")
            .Bind(configuration.GetSection("TelegramSettings"));
        var provider = services.BuildServiceProvider();

        return new TelegramConfigurationService(
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<global::TelegramSettings>>());
    }

    private static IDiscordConfigurationService DiscordConfigurationService(
        Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddOptions<global::TelegramSettings>("DiscordSettings")
            .Bind(configuration.GetSection("DiscordSettings"));
        var provider = services.BuildServiceProvider();

        return new DiscordConfigurationService(
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<global::TelegramSettings>>());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { ok = true })
            });
        }
    }

    private sealed class TelegramClientFactory(HttpClient client) : ITelegramHttpClientFactory
    {
        public HttpClient CreateClient() => Configure(client, "https://api.example/telegram/sendMessage");
    }

    private sealed class DiscordClientFactory(HttpClient client) : IDiscordHttpClientFactory
    {
        public HttpClient CreateClient() => Configure(client, "https://api.example/discord/");
    }

    private sealed class TelegramConfiguration(string channelName) : ITelegramConfigurationService
    {
        public TelegramSettings GetTelegramSettings() => new() { Token = "token", ChannelName = channelName };
    }

    private sealed class DiscordConfiguration(string channelName) : IDiscordConfigurationService
    {
        public TelegramSettings GetDiscordSettings() => new() { Token = "token", ChannelName = channelName };
    }

    private static HttpClient Configure(HttpClient client, string baseAddress)
    {
        client.BaseAddress = new Uri(baseAddress);
        return client;
    }
}
