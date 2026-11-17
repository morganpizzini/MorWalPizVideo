using System.Text.Json;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;

namespace MorWalPizVideo.BackOffice.Tests.Features;

[Trait("Category", "TestGroup:ServerAPI")]
public sealed class ServerApiOpenApiTests
{
    [Fact]
    public async Task OpenApi_document_is_generated_when_enabled()
    {
        await using var factory = ServerApiWebApplicationFactory.CreateWithSwagger();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.EnsureSuccessStatusCode();
        await using var documentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(documentStream);
        Assert.True(document.RootElement.TryGetProperty("paths", out _));
    }
}
