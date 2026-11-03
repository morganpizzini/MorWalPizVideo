using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MorWalPiz.Contracts.Contracts;
using MorWalPiz.VideoImporter.Models;

namespace MorWalPiz.VideoImporter.Services;

public sealed class FakeApiServiceFactory(string scenario) : IApiServiceFactory
{
    private readonly FakeApiScenarioState _state = new(scenario);

    public ApiService Create(string apiEndpoint, string? apiKey = null, string? channelId = null)
    {
        var client = new HttpClient(new FakeApiMessageHandler(_state))
        {
            BaseAddress = new Uri(apiEndpoint, UriKind.Absolute)
        };
        if (!string.IsNullOrWhiteSpace(apiKey))
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        if (!string.IsNullOrWhiteSpace(channelId))
            client.DefaultRequestHeaders.Add("X-Channel-Id", channelId);
        return new ApiService(client);
    }

    public void SetRemoteSettings(string channelId, ChannelConfigurationSnapshot snapshot) =>
        _state.SetRemoteSettings(channelId, snapshot);

    public VideoImporterChannelSettingsResponse? GetSavedSettings(string channelId) =>
        _state.GetSettings(channelId);

    public int GetSettingsRequestCount => _state.GetSettingsRequestCount;
    public int PutSettingsRequestCount => _state.PutSettingsRequestCount;
    public void SetScenario(string scenario) => _state.Scenario = scenario.Trim().ToLowerInvariant();
}

internal sealed class FakeApiScenarioState(string scenario)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, VideoImporterChannelSettingsResponse> _settings =
        new(StringComparer.Ordinal);

    public string Scenario { get; set; } = scenario.Trim().ToLowerInvariant();
    private int _getSettingsRequestCount;
    private int _putSettingsRequestCount;
    public int GetSettingsRequestCount => Volatile.Read(ref _getSettingsRequestCount);
    public int PutSettingsRequestCount => Volatile.Read(ref _putSettingsRequestCount);
    public void RecordSettingsRequest(HttpMethod method)
    {
        if (method == HttpMethod.Get)
            Interlocked.Increment(ref _getSettingsRequestCount);
        else if (method == HttpMethod.Put)
            Interlocked.Increment(ref _putSettingsRequestCount);
    }

    public VideoImporterChannelSettingsResponse? GetSettings(string channelId) =>
        _settings.GetValueOrDefault(channelId);

    public VideoImporterChannelSettingsResponse SaveSettings(
        string channelId,
        VideoImporterChannelSettingsRequest request)
    {
        var response = new VideoImporterChannelSettingsResponse(
            channelId,
            request.SchemaVersion,
            request.Payload.Clone(),
            DateTime.UtcNow);
        _settings[channelId] = response;
        return response;
    }

    public void SetRemoteSettings(string channelId, ChannelConfigurationSnapshot snapshot)
    {
        _settings[channelId] = new VideoImporterChannelSettingsResponse(
            channelId,
            snapshot.SchemaVersion,
            JsonSerializer.SerializeToElement(snapshot, JsonOptions),
            DateTime.UtcNow);
    }
}

internal sealed class FakeApiMessageHandler(FakeApiScenarioState state) : HttpMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isSettingsRequest = path.EndsWith(
            "/api/v1/video-importer-settings",
            StringComparison.OrdinalIgnoreCase);
        if (isSettingsRequest)
            return await HandleSettingsRequestAsync(request, cancellationToken);

        var status = state.Scenario switch
        {
            "auth" => HttpStatusCode.Unauthorized,
            "transient" => HttpStatusCode.ServiceUnavailable,
            "permanent" => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.OK
        };
        var body = state.Scenario == "success" && path.EndsWith("accessible", StringComparison.OrdinalIgnoreCase)
            ? "[]"
            : state.Scenario == "success" && path.EndsWith("translate", StringComparison.OrdinalIgnoreCase)
                ? "[]"
                : "{}";
        return JsonResponse(request, status, body);
    }

    private async Task<HttpResponseMessage> HandleSettingsRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        state.RecordSettingsRequest(request.Method);
        var failure = state.Scenario switch
        {
            "auth" or "settings-auth" => HttpStatusCode.Unauthorized,
            "transient" or "settings-transient" => HttpStatusCode.ServiceUnavailable,
            "permanent" or "settings-permanent" => HttpStatusCode.BadRequest,
            "route-missing" => HttpStatusCode.NotFound,
            _ => (HttpStatusCode?)null
        };
        if (failure.HasValue)
            return JsonResponse(request, failure.Value, "{}");

        if (state.Scenario == "settings-malformed" && request.Method == HttpMethod.Get)
            return JsonResponse(request, HttpStatusCode.OK, "{");

        var channelId = request.Headers.TryGetValues("X-Channel-Id", out var channelHeaders)
            ? channelHeaders.FirstOrDefault() ?? string.Empty
            : string.Empty;

        if (request.Method == HttpMethod.Get)
        {
            if (state.GetSettings(channelId) is { } existing)
                return JsonResponse(request, HttpStatusCode.OK, JsonSerializer.Serialize(existing, JsonOptions));

            if (state.Scenario == "settings-existing")
            {
                var existingSnapshot = new ChannelConfigurationSnapshot
                {
                    ApplicationName = "Remote application",
                    DefaultHashtags = "remote, tags",
                    Languages = [new ChannelLanguageSnapshot("it", "Italiano", true, true, null)],
                    PublishSchedules = []
                };
                state.SetRemoteSettings(channelId, existingSnapshot);
                return JsonResponse(
                    request,
                    HttpStatusCode.OK,
                    JsonSerializer.Serialize(state.GetSettings(channelId), JsonOptions));
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                RequestMessage = request
            };
        }

        if (request.Method == HttpMethod.Put)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            VideoImporterChannelSettingsRequest? payload;
            try
            {
                payload = JsonSerializer.Deserialize<VideoImporterChannelSettingsRequest>(body, JsonOptions);
            }
            catch (JsonException)
            {
                return JsonResponse(request, HttpStatusCode.BadRequest, "{}");
            }

            if (payload is null || payload.Payload.ValueKind != JsonValueKind.Object)
                return JsonResponse(request, HttpStatusCode.BadRequest, "{}");
            var saved = state.SaveSettings(channelId, payload);
            return JsonResponse(request, HttpStatusCode.OK, JsonSerializer.Serialize(saved, JsonOptions));
        }

        return JsonResponse(request, HttpStatusCode.MethodNotAllowed, "{}");
    }

    private static HttpResponseMessage JsonResponse(
        HttpRequestMessage request,
        HttpStatusCode status,
        string body) =>
        new(status)
        {
            RequestMessage = request,
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
}
