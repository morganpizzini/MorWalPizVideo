using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MorWalPizVideo.BackOffice.Services;
using MorWalPizVideo.Domain;
using MorWalPizVideo.Models.Configuration;
using System.Net;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class BlobStorageConfigurationTests
{
  [Fact]
  public void Managed_identity_endpoint_is_used_when_preferred()
  {
    var options = new BlobStorageOptions
    {
      Endpoint = "https://storage.example.test",
      ConnectionString = "UseDevelopmentStorage=true",
      PreferManagedIdentity = true
    };

    var client = BlobStorageClientFactory.Create(options, new TestTokenCredential());

    Assert.Equal(new Uri("https://storage.example.test"), client.Uri);
  }

  [Fact]
  public void Connection_string_remains_the_default_fallback()
  {
    var options = new BlobStorageOptions
    {
      Endpoint = "https://storage.example.test",
      ConnectionString = "UseDevelopmentStorage=true"
    };

    var client = BlobStorageClientFactory.Create(options, new TestTokenCredential());

    Assert.Equal("127.0.0.1", client.Uri.Host);
  }

  [Fact]
  public void Missing_endpoint_and_connection_string_fails_when_client_is_resolved()
  {
    var exception = Assert.Throws<InvalidOperationException>(() =>
        BlobStorageClientFactory.Create(new BlobStorageOptions(), new TestTokenCredential()));

    Assert.Contains("BlobStorage:Endpoint", exception.Message, StringComparison.Ordinal);
  }

  [Fact]
  public void Blob_options_require_credentials_and_core_container_names()
  {
    var result = new BlobStorageOptionsValidator().Validate(null, new BlobStorageOptions());

    Assert.True(result.Failed);
    Assert.Contains(result.Failures, failure => failure.Contains("Endpoint or ConnectionString", StringComparison.Ordinal));
    Assert.Contains(result.Failures, failure => failure.Contains("ContainerName", StringComparison.Ordinal));
    Assert.Contains(result.Failures, failure => failure.Contains("SponsorContainerName", StringComparison.Ordinal));
    Assert.Contains(result.Failures, failure => failure.Contains("PageContainerName", StringComparison.Ordinal));
  }

  [Theory]
  [InlineData("preview.jpg", "image/jpeg")]
  [InlineData("preview.webp", "image/webp")]
  [InlineData("original.mp4", "video/mp4")]
  [InlineData("artifact.unknown", "application/octet-stream")]
  public void Upload_content_type_is_derived_from_the_blob_path(string path, string expected)
  {
    Assert.Equal(expected, BlobService.GetContentType(path));
  }

  [Fact]
  public void Download_checksum_verification_is_case_insensitive_and_preserves_position()
  {
    using var content = new MemoryStream("phase-5-recovery"u8.ToArray());
    content.Position = 3;

    var verified = BlobService.VerifyChecksum(
        content,
        "5937F2E04B9C7D7115FE202ABC2C06000337F27F3AE8A630BE9FA6A0899E733C");

    Assert.True(verified);
    Assert.Equal(3, content.Position);
  }

  [Fact]
  public async Task Upload_sets_content_type_checksum_and_size_metadata()
  {
    HttpRequestMessage? capturedRequest = null;
    var handler = new StubHttpMessageHandler(async request =>
    {
      capturedRequest = await CloneAsync(request);
      return CreateResponse(HttpStatusCode.Created);
    });
    var service = CreateBlobService(handler);
    var content = "blob-metadata"u8.ToArray();

    await service.UploadImagesAsync("folder/preview.jpg", new MemoryStream(content));

    Assert.NotNull(capturedRequest);
    Assert.Equal(HttpMethod.Put, capturedRequest.Method);
    Assert.Equal("image/jpeg", GetHeader(capturedRequest, "x-ms-blob-content-type"));
    Assert.Equal(content.Length.ToString(), GetHeader(capturedRequest, "x-ms-meta-sizebytes"));
    Assert.Equal(
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content)).ToLowerInvariant(),
        GetHeader(capturedRequest, "x-ms-meta-sha256"));
  }

  [Theory]
  [InlineData(HttpStatusCode.NotFound, BlobDownloadStatus.NotFound)]
  [InlineData(HttpStatusCode.ServiceUnavailable, BlobDownloadStatus.Unavailable)]
  public async Task Download_distinguishes_not_found_from_operational_failure(
      HttpStatusCode responseStatus,
      BlobDownloadStatus expectedStatus)
  {
    var service = CreateBlobService(new StubHttpMessageHandler(_ =>
        Task.FromResult(CreateResponse(responseStatus))));

    var result = await service.DownloadWithMetadataAsync("missing.jpg");

    Assert.Equal(expectedStatus, result.Status);
    Assert.Null(result.Content);
  }

  [Theory]
  [InlineData(HttpStatusCode.OK, HealthStatus.Healthy)]
  [InlineData(HttpStatusCode.Forbidden, HealthStatus.Unhealthy)]
  public async Task Readiness_reports_authorized_service_access(
      HttpStatusCode responseStatus,
      HealthStatus expectedStatus)
  {
    var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateResponse(
        responseStatus,
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><StorageServiceProperties />")));
    var options = CreateOptions();
    var client = CreateClient(handler);
    var healthCheck = new BlobStorageHealthCheck(client, Options.Create(options));

    var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

    Assert.Equal(expectedStatus, result.Status);
    Assert.Equal("https://storage.example.test", result.Data["endpoint"]);
    Assert.DoesNotContain(result.Data.Keys, key => key.Contains("credential", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void Configured_blob_storage_registers_a_readiness_health_check()
  {
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["FeatureManagement:EnableMock"] = "false",
          ["BlobStorage:Endpoint"] = "https://storage.example.test",
          ["BlobStorage:PreferManagedIdentity"] = "true"
        })
        .Build();
    var services = new ServiceCollection();

    services.ConfigureHealthChecks(configuration);
    using var provider = services.BuildServiceProvider();
    var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

    Assert.Contains(registrations, registration =>
        registration.Name == "blob-storage" && registration.Tags.Contains("ready"));
  }

  private static BlobService CreateBlobService(HttpMessageHandler handler)
  {
    var options = CreateOptions();
    return new BlobService(
        Options.Create(options),
        CreateClient(handler),
        NullLogger<BlobService>.Instance);
  }

  private static BlobServiceClient CreateClient(HttpMessageHandler handler)
  {
    var clientOptions = new BlobClientOptions
    {
      Transport = new HttpClientTransport(new HttpClient(handler))
    };
    return new BlobServiceClient(new Uri("https://storage.example.test"), clientOptions);
  }

  private static BlobStorageOptions CreateOptions() => new()
  {
    Endpoint = "https://storage.example.test",
    PreferManagedIdentity = true,
    ContainerName = "previews",
    UploadContainerName = "uploads",
    SponsorContainerName = "sponsors",
    PageContainerName = "pages",
    RecoveryContainerName = "recovery"
  };

  private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string? content = null)
  {
    var response = new HttpResponseMessage(statusCode)
    {
      Content = new StringContent(content ?? string.Empty)
    };
    response.Headers.TryAddWithoutValidation("x-ms-request-id", "request-id");
    response.Headers.TryAddWithoutValidation("ETag", "\"etag\"");
    response.Content.Headers.LastModified = DateTimeOffset.UtcNow;
    return response;
  }

  private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
  {
    var clone = new HttpRequestMessage(request.Method, request.RequestUri);
    foreach (var header in request.Headers)
    {
      clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    if (request.Content is not null)
    {
      clone.Content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync());
      clone.Content.Headers.Clear();
      foreach (var header in request.Content.Headers)
      {
        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
    }

    return clone;
  }

  private static string? GetHeader(HttpRequestMessage request, string name) =>
      request.Headers.TryGetValues(name, out var values) ? values.SingleOrDefault() : null;

  private sealed class StubHttpMessageHandler(
      Func<HttpRequestMessage, Task<HttpResponseMessage>> handleAsync) : HttpMessageHandler
  {
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) => handleAsync(request);
  }

  private sealed class TestTokenCredential : TokenCredential
  {
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new("test-token", DateTimeOffset.MaxValue);

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(new AccessToken("test-token", DateTimeOffset.MaxValue));
  }
}