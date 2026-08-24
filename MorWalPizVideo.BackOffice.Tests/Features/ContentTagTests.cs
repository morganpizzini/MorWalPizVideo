using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MorWalPiz.Contracts;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Models.Constraints;
using MorWalPizVideo.Server.Models;

namespace MorWalPizVideo.BackOffice.Tests.Features;

public sealed class ContentTagRulesTests
{
    [Fact]
    public void Normalization_trims_whitespace()
    {
        Assert.True(ContentTagRules.TryNormalize(new[] { "  tutorial  " }, out var normalized, out var error));

        Assert.Null(error);
        Assert.Equal(new[] { "tutorial" }, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Normalization_rejects_empty_values(string tag)
    {
        Assert.False(ContentTagRules.TryNormalize(new[] { "valid", tag }, out var normalized, out var error));

        Assert.Empty(normalized);
        Assert.Equal("Tags cannot be empty or whitespace", error);
    }

    [Fact]
    public void Normalization_dedupes_case_insensitively_preserving_first_display_casing()
    {
        Assert.True(
            ContentTagRules.TryNormalize(new[] { "Tutorial", "tutorial", " TUTORIAL " }, out var normalized, out _));

        Assert.Equal(new[] { "Tutorial" }, normalized);
    }

    [Fact]
    public void Normalization_rejects_tags_longer_than_the_bound()
    {
        var tooLong = new string('a', ContentTagRules.MaxTagLength + 1);

        Assert.False(ContentTagRules.TryNormalize(new[] { tooLong }, out var normalized, out var error));

        Assert.Empty(normalized);
        Assert.Equal($"Tags cannot exceed {ContentTagRules.MaxTagLength} characters", error);
    }

    [Fact]
    public void Normalization_accepts_a_tag_exactly_at_the_length_bound()
    {
        var atBound = new string('a', ContentTagRules.MaxTagLength);

        Assert.True(ContentTagRules.TryNormalize(new[] { atBound }, out var normalized, out _));

        Assert.Equal(new[] { atBound }, normalized);
    }

    [Fact]
    public void Normalization_rejects_more_than_the_maximum_number_of_tags()
    {
        var tooMany = Enumerable.Range(0, ContentTagRules.MaxTagsPerContent + 1)
            .Select(index => $"tag-{index}")
            .ToArray();

        Assert.False(ContentTagRules.TryNormalize(tooMany, out var normalized, out var error));

        Assert.Empty(normalized);
        Assert.Equal($"A maximum of {ContentTagRules.MaxTagsPerContent} tags is allowed", error);
    }

    [Fact]
    public void Normalization_counts_the_bound_after_deduplication()
    {
        var duplicates = Enumerable.Range(0, ContentTagRules.MaxTagsPerContent)
            .Select(index => $"tag-{index}")
            .Concat(Enumerable.Range(0, ContentTagRules.MaxTagsPerContent).Select(index => $"TAG-{index}"))
            .ToArray();

        Assert.True(ContentTagRules.TryNormalize(duplicates, out var normalized, out _));

        Assert.Equal(ContentTagRules.MaxTagsPerContent, normalized.Length);
    }

    [Fact]
    public void Normalization_of_null_yields_an_empty_array()
    {
        Assert.True(ContentTagRules.TryNormalize(null, out var normalized, out var error));

        Assert.Empty(normalized);
        Assert.Null(error);
    }
}

public sealed class ContentTagContractTests
{
    private static YouTubeContent CreateContent(string[]? tags = null)
    {
        var content = new YouTubeContent(
            "content-1",
            "Title",
            "Description",
            "url",
            "video-1",
            [],
            [],
            YoutubeContentType.Collection);

        return tags is null ? content : content with { Tags = tags };
    }

    [Fact]
    public void Legacy_content_without_tags_defaults_to_an_empty_array()
    {
        Assert.Empty(CreateContent().Tags);
    }

    [Fact]
    public void Admin_contract_serializes_tags_as_empty_array_when_absent()
    {
        var json = JsonSerializer.Serialize(ContractUtils.Convert(CreateContent()));

        Assert.Contains("\"Tags\":[]", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_contract_serializes_tags_as_empty_array_when_absent()
    {
        var json = JsonSerializer.Serialize(ContractUtils.ConvertPublic(CreateContent()));

        Assert.Contains("\"Tags\":[]", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_contract_carries_tags_for_the_homepage_filter()
    {
        var contract = ContractUtils.ConvertPublic(CreateContent(["Tutorial", "Gear"]));

        Assert.Equal(new[] { "Tutorial", "Gear" }, contract.Tags);
    }

    [Fact]
    public void Admin_contract_carries_tags()
    {
        var contract = ContractUtils.Convert(CreateContent(["Tutorial"]));

        Assert.Equal(new[] { "Tutorial" }, contract.Tags);
    }
}

[Collection("WebAppFactory")]
public sealed class VideoTagEndpointTests
{
    private readonly BackOfficeWebApplicationFactory _factory;

    public VideoTagEndpointTests(BackOfficeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> SeedTaggedMatchAsync(params string[] tags)
    {
        var repository = _factory.MatchRepository!;
        var match = (await repository.GetItemsAsync()).First();
        await repository.UpdateItemAsync(match with { Tags = tags });
        return match.Id;
    }

    private static object BuildUpdateBody(YouTubeContent match, object? tags)
        => new
        {
            Title = match.Title,
            Description = match.Description ?? string.Empty,
            Url = match.Url ?? string.Empty,
            ThumbnailVideoId = match.ThumbnailVideoId ?? string.Empty,
            Categories = match.Categories?.Select(category => category.Id).ToArray() ?? [],
            Tags = tags
        };

    [Fact]
    public async Task Update_normalizes_tags_before_persisting()
    {
        var id = await SeedTaggedMatchAsync();
        using var client = _factory.CreateClientWithPermissions("videos.manage");
        var match = await _factory.MatchRepository!.GetItemAsync(id);

        var response = await client.PutAsJsonAsync(
            $"/api/Videos/{id}",
            BuildUpdateBody(match, new[] { "  Tutorial ", "tutorial", "Gear", "Optics" }));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await _factory.MatchRepository.GetItemAsync(id);
        Assert.Equal(new[] { "Tutorial", "Gear", "Optics" }, persisted.Tags);
    }

    [Fact]
    public async Task Update_rejects_empty_tags()
    {
        var id = await SeedTaggedMatchAsync("Existing");
        using var client = _factory.CreateClientWithPermissions("videos.manage");
        var match = await _factory.MatchRepository!.GetItemAsync(id);

        var response = await client.PutAsJsonAsync($"/api/Videos/{id}", BuildUpdateBody(match, new[] { "  " }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var persisted = await _factory.MatchRepository.GetItemAsync(id);
        Assert.Equal(new[] { "Existing" }, persisted.Tags);
    }

    [Fact]
    public async Task Update_rejects_more_tags_than_allowed()
    {
        var id = await SeedTaggedMatchAsync();
        using var client = _factory.CreateClientWithPermissions("videos.manage");
        var match = await _factory.MatchRepository!.GetItemAsync(id);
        var tooMany = Enumerable.Range(0, ContentTagRules.MaxTagsPerContent + 1)
            .Select(index => $"tag-{index}")
            .ToArray();

        var response = await client.PutAsJsonAsync($"/api/Videos/{id}", BuildUpdateBody(match, tooMany));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_without_tags_preserves_the_persisted_tags()
    {
        var id = await SeedTaggedMatchAsync("Kept");
        using var client = _factory.CreateClientWithPermissions("videos.manage");
        var match = await _factory.MatchRepository!.GetItemAsync(id);

        var response = await client.PutAsJsonAsync($"/api/Videos/{id}", BuildUpdateBody(match, null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await _factory.MatchRepository.GetItemAsync(id);
        Assert.Equal(new[] { "Kept" }, persisted.Tags);
    }

    [Fact]
    public async Task Update_with_an_empty_tag_list_clears_the_tags()
    {
        var id = await SeedTaggedMatchAsync("Removed");
        using var client = _factory.CreateClientWithPermissions("videos.manage");
        var match = await _factory.MatchRepository!.GetItemAsync(id);

        var response = await client.PutAsJsonAsync($"/api/Videos/{id}", BuildUpdateBody(match, Array.Empty<string>()));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var persisted = await _factory.MatchRepository.GetItemAsync(id);
        Assert.Empty(persisted.Tags);
    }

    [Fact]
    public async Task Tag_suggestions_return_distinct_tags_from_authorized_content()
    {
        await SeedTaggedMatchAsync("Tutorial", "Gear");
        using var client = _factory.CreateClientWithPermissions("videos.view");

        var response = await client.GetAsync("/api/Videos/tag-suggestions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.NotNull(suggestions);
        Assert.Equal(new[] { "Gear", "Tutorial" }, suggestions!.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Tag_suggestions_filter_case_insensitively_on_the_query()
    {
        await SeedTaggedMatchAsync("Tutorial", "Gear");
        using var client = _factory.CreateClientWithPermissions("videos.view");

        var response = await client.GetAsync("/api/Videos/tag-suggestions?q=TUT");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.Equal(new[] { "Tutorial" }, suggestions);
    }

    [Fact]
    public async Task Tag_suggestions_are_bounded_by_take()
    {
        await SeedTaggedMatchAsync("Tutorial", "Gear", "Optics");
        using var client = _factory.CreateClientWithPermissions("videos.view");

        var response = await client.GetAsync("/api/Videos/tag-suggestions?take=2");

        var suggestions = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.Equal(2, suggestions!.Length);
    }

    [Fact]
    public async Task Tag_suggestions_are_capped_at_the_server_maximum()
    {
        await SeedTaggedMatchAsync("Tutorial");
        using var client = _factory.CreateClientWithPermissions("videos.view");

        var response = await client.GetAsync(
            $"/api/Videos/tag-suggestions?take={ContentTagRules.MaxSuggestions + 1000}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<string[]>();
        Assert.True(suggestions!.Length <= ContentTagRules.MaxSuggestions);
    }

    [Fact]
    public async Task Tag_suggestions_require_an_authorized_permission()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);

        var response = await client.GetAsync("/api/Videos/tag-suggestions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        client.Dispose();
    }
}
