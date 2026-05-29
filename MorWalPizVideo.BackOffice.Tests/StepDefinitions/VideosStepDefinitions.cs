using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Server.Services.Interfaces;
using Reqnroll;
using Xunit;

namespace MorWalPizVideo.BackOffice.Tests.StepDefinitions;

[Binding]
[Collection("WebAppFactory")]
public class VideosStepDefinitions
{
    private readonly HttpClient _client;
    private readonly TestScenarioContext _context;
    private readonly MatchMockRepository _matchRepository;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public VideosStepDefinitions(BackOfficeWebApplicationFactory factory, TestScenarioContext context)
    {
        _client = factory.CreateClient();
        _context = context;
        _matchRepository = factory.MatchRepository!;
    }

    [Given(@"a video exists in the system")]
    public async Task GivenAVideoExistsInTheSystem()
    {
        var items = await _matchRepository.GetItemsAsync();
        items.Should().NotBeEmpty("mock data should provide at least one match");
        _context.ExistingVideoId = items.First().Id;
    }

    [Given(@"a root match exists in the system")]
    public async Task GivenARootMatchExistsInTheSystem()
    {
        var items = await _matchRepository.GetItemsAsync(x => !x.IsLink);
        items.Should().NotBeEmpty("mock data should provide at least one root match");
        _context.ExistingRootMatchId = items.First().Id;
    }

    [When(@"I request all videos")]
    public async Task WhenIRequestAllVideos()
    {
        _context.Response = await _client.GetAsync("/api/Videos");
    }

    [When(@"I request the video by its ID")]
    public async Task WhenIRequestTheVideoByItsId()
    {
        _context.ExistingVideoId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.GetAsync($"/api/Videos/{_context.ExistingVideoId}");
    }

    [When(@"I request a video with ID ""(.*)""")]
    public async Task WhenIRequestAVideoWithId(string id)
    {
        _context.Response = await _client.GetAsync($"/api/Videos/{id}");
    }

    [When(@"I update the video with new title ""(.*)""")]
    public async Task WhenIUpdateTheVideoWithNewTitle(string newTitle)
    {
        _context.ExistingVideoId.Should().NotBeNullOrEmpty();

        var match = await _matchRepository.GetItemAsync(_context.ExistingVideoId!);
        match.Should().NotBeNull();

        var body = new
        {
            Title = newTitle,
            Description = match!.Description ?? string.Empty,
            Url = match.Url ?? string.Empty,
            ThumbnailVideoId = match.ThumbnailVideoId ?? string.Empty,
            Categories = match.Categories?.Select(c => c.Id).ToArray() ?? Array.Empty<string>()
        };

        _context.Response = await _client.PutAsJsonAsync($"/api/Videos/{_context.ExistingVideoId}", body);
    }

    [When(@"I update a video with ID ""(.*)""")]
    public async Task WhenIUpdateAVideoWithId(string id)
    {
        var body = new
        {
            Title = "ignored",
            Description = "ignored",
            Url = "ignored",
            ThumbnailVideoId = "ignored",
            Categories = Array.Empty<string>()
        };
        _context.Response = await _client.PutAsJsonAsync($"/api/Videos/{id}", body);
    }

    [When(@"I swap the thumbnail to a new video id")]
    public async Task WhenISwapTheThumbnailToANewVideoId()
    {
        _context.ExistingRootMatchId.Should().NotBeNullOrEmpty();
        var body = new
        {
            CurrentVideoId = _context.ExistingRootMatchId,
            NewVideoId = "swap-target-video-id"
        };
        _context.Response = await _client.PostAsJsonAsync("/api/Videos/SwapThumbnailId", body);
    }

    [When(@"I swap the thumbnail for match id ""(.*)""")]
    public async Task WhenISwapTheThumbnailForMatchId(string id)
    {
        var body = new { CurrentVideoId = id, NewVideoId = "any" };
        _context.Response = await _client.PostAsJsonAsync("/api/Videos/SwapThumbnailId", body);
    }

    [When(@"I create a root match with video id ""(.*)"" and title ""(.*)""")]
    public async Task WhenICreateARootMatchWith(string videoId, string title)
    {
        var body = new
        {
            VideoId = videoId,
            Title = title,
            Description = "Root description",
            Url = $"root-url-{videoId}",
            Categories = new[] { "1" }
        };
        _context.Response = await _client.PostAsJsonAsync("/api/Videos/RootCreation", body);
    }

    [Then(@"the response should contain a list of videos")]
    public async Task ThenTheResponseShouldContainAListOfVideos()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().StartWith("[");
    }

    [Then(@"the response should contain the video details")]
    public async Task ThenTheResponseShouldContainTheVideoDetails()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().Contain("contentId");
    }
}
