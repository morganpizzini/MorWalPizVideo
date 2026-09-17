using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MorWalPiz.VideoImporter.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MorWalPiz.VideoImporter.Services;

public interface IHashtagHistoryService
{
    IReadOnlyList<string> Suggest(string input, int tenantId, string channelId, int limit = 8);
    void Record(IEnumerable<string> hashtags, int tenantId, string channelId);
}

public sealed class HashtagHistoryService(DatabaseService databaseService) : IHashtagHistoryService
{
    public IReadOnlyList<string> Suggest(string input, int tenantId, string channelId, int limit = 8)
    {
        var normalizedInput = HashtagNormalizer.Normalize(input);
        using var context = databaseService.CreateContext();
        return context.HashtagHistory
            .Where(item => item.TenantId == tenantId && item.ChannelId == channelId && item.Value.StartsWith(normalizedInput))
            .OrderByDescending(item => item.LastUsedAtUtc)
            .Select(item => item.Value)
            .Take(limit)
            .ToList();
    }

    public void Record(IEnumerable<string> hashtags, int tenantId, string channelId)
    {
        using var context = databaseService.CreateContext();
        foreach (var hashtag in hashtags.Select(HashtagNormalizer.Normalize).Where(value => value.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = context.HashtagHistory.FirstOrDefault(item => item.TenantId == tenantId && item.ChannelId == channelId && item.Value.ToLower() == hashtag.ToLower());
            if (existing is null)
            {
                context.HashtagHistory.Add(new HashtagHistory { Value = hashtag, TenantId = tenantId, ChannelId = channelId });
            }
            else
            {
                existing.LastUsedAtUtc = DateTime.UtcNow;
            }
        }
        context.SaveChanges();
    }
}

public static class HashtagNormalizer
{
    public static string Normalize(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith('#') ? trimmed : $"#{trimmed}";
    }

    public static IReadOnlyList<string> Extract(string caption) => caption
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        .Where(token => token.StartsWith('#'))
        .Select(Normalize)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

public interface ISocialProvider
{
    SocialProviderKind Provider { get; }
    SocialProviderCapability GetCapability(SocialChannelConfiguration? configuration);
    Task<SocialPublishResult> PublishAsync(SocialPostDraft draft, SocialChannelConfiguration? configuration, CancellationToken cancellationToken = default);
}

public sealed class OfficialGraphSocialProvider(IHttpClientFactory httpClientFactory) : ISocialProvider
{
    public SocialProviderKind Provider { get; init; }

    public SocialProviderCapability GetCapability(SocialChannelConfiguration? configuration)
    {
        var configured = configuration is not null && !string.IsNullOrWhiteSpace(configuration.AccountId) && !string.IsNullOrWhiteSpace(configuration.AccessToken);
        var ratios = Provider switch
        {
            SocialProviderKind.InstagramBusiness => new[] { "portrait", "square", "landscape" },
            SocialProviderKind.Threads => new[] { "portrait", "square", "landscape" },
            SocialProviderKind.FacebookPage => new[] { "portrait", "square", "landscape" },
            _ => Array.Empty<string>()
        };
        var scheduling = Provider == SocialProviderKind.FacebookPage;
        var localMedia = Provider == SocialProviderKind.FacebookPage ||
            Provider == SocialProviderKind.InstagramBusiness || Provider == SocialProviderKind.Threads;
        var status = configured ? "Configurato" : "Non configurato per questo canale";
        return new SocialProviderCapability(Provider, configured, localMedia, scheduling, ratios, status);
    }

    public async Task<SocialPublishResult> PublishAsync(SocialPostDraft draft, SocialChannelConfiguration? configuration, CancellationToken cancellationToken = default)
    {
        var capability = GetCapability(configuration);
        if (!capability.IsConfigured)
            return new(false, capability.StatusMessage);
        if (!capability.SupportedAspectRatios.Contains(draft.AspectRatio, StringComparer.OrdinalIgnoreCase))
            return new(false, $"Formato {draft.AspectRatio} non supportato da {Provider}.");
        if (draft.ScheduledAt.HasValue && !capability.SupportsScheduling)
            return new(false, $"Scheduling non supportato da {Provider}.");
        var client = httpClientFactory.CreateClient("SocialGraph");
        var media = draft.Media.FirstOrDefault();
        if (Provider is SocialProviderKind.InstagramBusiness or SocialProviderKind.Threads && string.IsNullOrWhiteSpace(media?.RemoteUrl))
            return new(false, $"{Provider}: media URL non disponibile.");

        var endpoint = Provider switch
        {
            SocialProviderKind.InstagramBusiness => $"{configuration!.AccountId}/media",
            SocialProviderKind.Threads => $"{configuration!.AccountId}/threads",
            _ => $"{configuration!.AccountId}/feed"
        };
        object payload;
        if (Provider == SocialProviderKind.InstagramBusiness)
        {
            payload = new
            {
                image_url = media?.MediaType == SocialMediaType.Image ? media.RemoteUrl : null,
                video_url = media?.MediaType == SocialMediaType.Video ? media.RemoteUrl : null,
                media_type = media?.MediaType == SocialMediaType.Video ? "VIDEO" : "IMAGE",
                caption = draft.Caption
            };
        }
        else if (Provider == SocialProviderKind.Threads)
        {
            payload = new
            {
                image_url = media?.MediaType == SocialMediaType.Image ? media.RemoteUrl : null,
                video_url = media?.MediaType == SocialMediaType.Video ? media.RemoteUrl : null,
                media_type = media?.MediaType == SocialMediaType.Video ? "VIDEO" : "IMAGE",
                text = draft.Caption
            };
        }
        else
        {
            payload = new { message = draft.Caption, scheduled_publish_time = draft.ScheduledAt?.ToUnixTimeSeconds() };
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.AccessToken);
        request.Content = JsonContent.Create(payload);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new(false, $"{Provider}: API ha restituito {(int)response.StatusCode}.");
        return new(true, "Pubblicazione inviata alla piattaforma.");
    }
}

public sealed class UnavailableSocialProvider(SocialProviderKind provider) : ISocialProvider
{
    public SocialProviderKind Provider => provider;
    public SocialProviderCapability GetCapability(SocialChannelConfiguration? configuration) =>
        new(provider, false, false, false, [], "Non supportato dalle API ufficiali disponibili per questo account.");
    public Task<SocialPublishResult> PublishAsync(SocialPostDraft draft, SocialChannelConfiguration? configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SocialPublishResult(false, GetCapability(configuration).StatusMessage));
}

public sealed class SocialPublishingService(DatabaseService databaseService, IEnumerable<ISocialProvider> providers, IApiServiceFactory apiServiceFactory)
{
    public IReadOnlyList<SocialProviderCapability> GetCapabilities(string channelId, int tenantId) => providers.Select(provider =>
    {
        using var context = databaseService.CreateContext();
        var configuration = context.SocialChannelConfigurations.FirstOrDefault(item => item.TenantId == tenantId && item.ChannelId == channelId && item.Provider == provider.Provider);
        return provider.GetCapability(configuration);
    }).ToList();

    public async Task<IReadOnlyList<SocialPublishResult>> PublishAsync(SocialPostDraft draft, CancellationToken cancellationToken = default)
    {
        using var context = databaseService.CreateContext();
        var results = new List<SocialPublishResult>();
        foreach (var provider in providers.Where(item => draft.Providers.Contains(item.Provider)))
        {
            var configuration = context.SocialChannelConfigurations.FirstOrDefault(item => item.TenantId == draft.TenantId && item.ChannelId == draft.ChannelId && item.Provider == provider.Provider);
            var preparedDraft = draft;
            if (provider.Provider is SocialProviderKind.InstagramBusiness or SocialProviderKind.Threads)
            {
                try
                {
                    preparedDraft = await UploadMediaAsync(draft, provider.Provider, cancellationToken);
                }
                catch (Exception exception)
                {
                    results.Add(new(false, $"Upload media per {provider.Provider} non riuscito: {exception.Message}"));
                    continue;
                }
            }

            var result = await provider.PublishAsync(preparedDraft, configuration, cancellationToken);
            if (!result.Succeeded && provider.Provider is SocialProviderKind.InstagramBusiness or SocialProviderKind.Threads)
            {
                // A Meta request can outlive the SAS. Re-uploading issues a fresh short-lived URL once.
                try
                {
                    var retryDraft = await UploadMediaAsync(draft, provider.Provider, cancellationToken);
                    result = await provider.PublishAsync(retryDraft, configuration, cancellationToken);
                }
                catch (Exception exception)
                {
                    result = new(false, $"Retry media per {provider.Provider} non riuscito: {exception.Message}");
                }
            }
            results.Add(result);
        }
        return results;
    }

    private async Task<SocialPostDraft> UploadMediaAsync(SocialPostDraft draft, SocialProviderKind provider, CancellationToken cancellationToken)
    {
        var api = apiServiceFactory.Create(App.ApiSettings.ApiEndpoint, App.ApiSettings.ApiKey, draft.ChannelId);
        var media = new List<SocialMediaDescriptor>();
        foreach (var item in draft.Media)
        {
            await using var stream = File.OpenRead(item.FilePath);
            var key = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{draft.ChannelId}|{provider}|{item.FilePath}|{new FileInfo(item.FilePath).Length}"))).ToLowerInvariant();
            var upload = await api.UploadSocialAssetAsync(stream, item.FilePath, GetContentType(item.FilePath), key, cancellationToken);
            media.Add(new SocialMediaDescriptor
            {
                FilePath = item.FilePath, MediaType = item.MediaType, ThumbnailPath = item.ThumbnailPath,
                Width = item.Width, Height = item.Height, RemoteUrl = upload.ReadUrl
            });
        }
        return new SocialPostDraft
        {
            TenantId = draft.TenantId, ChannelId = draft.ChannelId, Caption = draft.Caption,
            Media = media, Providers = draft.Providers, AspectRatio = draft.AspectRatio, ScheduledAt = draft.ScheduledAt
        };
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp",
        ".mov" => "video/quicktime", _ => "video/mp4"
    };
}

public interface IVideoThumbnailService
{
    Task<string?> ExtractAsync(string videoPath, CancellationToken cancellationToken = default);
}

public sealed class WpfVideoThumbnailService : IVideoThumbnailService
{
    public async Task<string?> ExtractAsync(string videoPath, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"morwalpiz-thumbnail-{Guid.NewGuid():N}.png");
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var player = new MediaPlayer();
        player.MediaOpened += (_, _) =>
        {
            try
            {
                var width = Math.Max(player.NaturalVideoWidth, 1);
                var height = Math.Max(player.NaturalVideoHeight, 1);
                var drawing = new DrawingVisual();
                using (var context = drawing.RenderOpen())
                    context.DrawVideo(player, new System.Windows.Rect(0, 0, width, height));
                var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(drawing);
                using var stream = File.Create(outputPath);
                new PngBitmapEncoder { Frames = { BitmapFrame.Create(bitmap) } }.Save(stream);
                completion.TrySetResult(outputPath);
            }
            catch
            {
                completion.TrySetResult(null);
            }
            finally
            {
                player.Close();
            }
        };
        player.MediaFailed += (_, _) => completion.TrySetResult(null);
        player.Open(new Uri(videoPath, UriKind.Absolute));
        using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
            return await completion.Task;
    }
}