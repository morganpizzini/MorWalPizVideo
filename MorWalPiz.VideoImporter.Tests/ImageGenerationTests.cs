using System.Net;
using System.Net.Http;
using System.IO;
using System.Text;
using FluentAssertions;
using MorWalPiz.VideoImporter.Models;
using MorWalPiz.VideoImporter.Services;
using Xunit;

namespace MorWalPiz.VideoImporter.Tests;

public sealed class ImageGenerationTests
{
    [Fact]
    public async Task GenerateAsync_sends_configured_json_and_bearer_key()
    {
        var handler = new RecordingHandler(_ => JsonResponse("{\"data\":[{\"b64_json\":\"aGVsbG8=\"}] }"));
        var service = CreateService(handler);

        var result = await service.GenerateAsync(new("a red fox", "1024x1024", "low", "opaque", "", "png", 1), CancellationToken.None);

        result.Single().Content.Should().Equal(Encoding.UTF8.GetBytes("hello"));
        handler.Request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Request.Headers.Authorization.Parameter.Should().Be("secret");
        handler.Body.Should().Contain("\"prompt\":\"a red fox\"");
        handler.Body.Should().Contain("\"output_format\":\"png\"");
    }

    [Fact]
    public async Task EditAsync_sends_one_image_and_optional_mask_as_multipart()
    {
        var source = Path.GetTempFileName();
        var mask = Path.GetTempFileName();
        await File.WriteAllBytesAsync(source, [1, 2]);
        await File.WriteAllBytesAsync(mask, [3, 4]);
        try
        {
            var handler = new RecordingHandler(_ => JsonResponse("{\"data\":[{\"b64_json\":\"aA==\"}]}"));
            await CreateService(handler).EditAsync(new("edit", source, mask), CancellationToken.None);
            handler.ContentType.Should().StartWith("multipart/form-data");
            handler.Body.Should().Contain("name=prompt");
            handler.Body.Should().Contain("name=image");
            handler.Body.Should().Contain("name=mask");
        }
        finally { File.Delete(source); File.Delete(mask); }
    }

    [Fact]
    public void Size_mapping_rejects_unconfigured_dimensions()
    {
        var options = new ImageGenerationOptions { SupportedSizes = new(StringComparer.OrdinalIgnoreCase) { ["16:9"] = "1536x864" } };
        ImageGenerationValidation.ResolveProviderSize(options, "16:9").Should().Be("1536x864");
        var action = () => ImageGenerationValidation.ResolveProviderSize(options, "4:3");
        action.Should().Throw<ImageProviderException>().WithMessage("*non è supportato*");
    }

    [Fact]
    public async Task Output_names_are_collision_free()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var output = new ImageOutputService();
            var image = new GeneratedImage([1], "png", "image/png");
            var first = await output.SaveAsync([image], directory, "same name", CancellationToken.None);
            var second = await output.SaveAsync([image], directory, "same name", CancellationToken.None);
            first.Single().Should().NotBe(second.Single());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Templates_persist_and_replace_by_name()
    {
        var path = Path.Combine(Path.GetTempPath(), $"templates-{Guid.NewGuid():N}.json");
        try
        {
            var store = new PromptTemplateStore(path);
            store.Save(new("Fox", "one"));
            store.Save(new("Fox", "two"));
            new PromptTemplateStore(path).GetAll().Should().ContainSingle().Which.Prompt.Should().Be("two");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Provider_errors_include_status_code()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad request") });
        var action = () => CreateService(handler).GenerateAsync(new("p", "1024x1024", "auto", "auto", "", "png", 1), CancellationToken.None);
        await action.Should().ThrowAsync<ImageProviderException>().Where(exception => exception.StatusCode == 400);
    }

    private static ImageGenerationService CreateService(RecordingHandler handler) => new(new TestHttpClientFactory(handler), new TestKeyProvider(), new ImageGenerationOptions { Endpoint = "https://provider.test/images", Model = "test", OutputFormat = "png" });
    private static HttpResponseMessage JsonResponse(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class TestKeyProvider : IImageApiKeyProvider
    {
        public Task<string> GetApiKeyAsync(CancellationToken cancellationToken) => Task.FromResult("secret");
    }

    private sealed class TestHttpClientFactory(RecordingHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage Request { get; private set; } = null!;
        public string Body { get; private set; } = string.Empty;
        public string ContentType { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            ContentType = request.Content?.Headers.ContentType?.ToString() ?? string.Empty;
            return responseFactory(request);
        }
    }
}