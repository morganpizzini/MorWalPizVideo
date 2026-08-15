using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Domain.Scenarios;
using MorWalPizVideo.Server.Models;
using MorWalPizVideo.Server.Services.Interfaces;
using MorWalPizVideo.Models.Models;
using Reqnroll;
using Xunit;

namespace MorWalPizVideo.BackOffice.Tests.StepDefinitions;

[Binding]
[Collection("WebAppFactory")]
public class VideoChannelAssignmentStepDefinitions
{
    private readonly HttpClient _client;
    private readonly TestScenarioContext _context;
    private readonly MatchMockRepository _matchRepository;
    private readonly YTChannelMockRepository _ytChannelRepository;
    private readonly UserChannelOwnerMockRepository _ownerRepository;

    public VideoChannelAssignmentStepDefinitions(BackOfficeWebApplicationFactory factory, TestScenarioContext context)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-Permissions", "videos.manage");
        _client.DefaultRequestHeaders.Add("X-Channel-Id", PrimaryScenario.ChannelId);
        _context = context;
        _matchRepository = factory.MatchRepository!;
        _ytChannelRepository = factory.YTChannelRepository!;
        _ownerRepository = factory.Services.GetRequiredService<IUserChannelOwnerRepository>() as UserChannelOwnerMockRepository
            ?? throw new InvalidOperationException("Owner repository is not configured");
    }

    private async Task<string> ResolveExistingVideoIdAsync()
    {
        if (!string.IsNullOrEmpty(_context.AssignVideoYoutubeId))
        {
            return _context.AssignVideoYoutubeId!;
        }

        var matches = await _matchRepository.GetItemsAsync();
        var withVideo = matches.FirstOrDefault(m => m.VideoRefs != null && m.VideoRefs.Any(v => !string.IsNullOrEmpty(v.YoutubeId)));
        withVideo.Should().NotBeNull("mock data should expose at least one match with a YouTube video ref");
        var ytId = withVideo!.VideoRefs!.First(v => !string.IsNullOrEmpty(v.YoutubeId)).YoutubeId;
        _context.AssignVideoYoutubeId = ytId;
        return ytId;
    }

    private async Task<YTChannel> ResolveTargetChannelAsync()
    {
        var channels = await _ytChannelRepository.GetItemsAsync();
        channels.Should().NotBeEmpty("mock data should expose at least one channel");
        var chosen = channels.First();
        await EnsureTestUserOwnsChannelAsync(chosen.ChannelId);
        _context.AssignTargetChannelId = chosen.ChannelId;
        return chosen;
    }

    [Given(@"a channel exists in the system")]
    public async Task GivenAChannelExistsInTheSystem()
    {
        await ResolveTargetChannelAsync();
    }

    [Given(@"a video exists on a source channel")]
    public async Task GivenAVideoExistsOnASourceChannel()
    {
        var channels = await _ytChannelRepository.GetItemsAsync();
        channels.Should().HaveCountGreaterThanOrEqualTo(1);

        var source = channels.First();
        var ytId = $"reassign-{Guid.NewGuid():N}";
        var updatedSource = source with
        {
            Videos = (source.Videos ?? new List<YouTubeVideo>())
                .Concat(new[] { new YouTubeVideo { VideoId = ytId } })
                .ToList()
        };
        await _ytChannelRepository.UpdateItemAsync(updatedSource);

        await _matchRepository.AddItemAsync(
            YouTubeContent.CreateSingleVideo(ytId, []) with
            {
                CreatorUserId = "test-user-id",
                OwnerChannelId = source.ChannelId,
                VideoRefs = [new VideoRef(ytId, channelIds: [source.ChannelId])]
            });

        _context.AssignSourceChannelId = source.ChannelId;
        _context.AssignVideoYoutubeId = ytId;
    }

    [Given(@"a different target channel exists")]
    public async Task GivenADifferentTargetChannelExists()
    {
        var channels = await _ytChannelRepository.GetItemsAsync();
        var target = channels.FirstOrDefault(c => c.ChannelId != _context.AssignSourceChannelId);
        if (target is null)
        {
            target = new YTChannel($"target-{Guid.NewGuid():N}", "Target Channel");
            await _ytChannelRepository.AddItemAsync(target);
        }
        await EnsureTestUserOwnsChannelAsync(target.ChannelId);
        _context.AssignTargetChannelId = target.ChannelId;
    }

    private async Task EnsureTestUserOwnsChannelAsync(string channelId)
    {
        var existing = await _ownerRepository.GetItemsAsync(owner =>
            owner.UserId == "test-user-id" && owner.ChannelId == channelId && owner.IsActive);
        if (existing.Count == 0)
        {
            await _ownerRepository.AddItemAsync(new UserChannelOwner
            {
                UserId = "test-user-id",
                ChannelId = channelId,
                IsActive = true
            });
        }
    }

    [When(@"I assign the video to the channel")]
    [When(@"I assign the video to the channel again")]
    [When(@"I assign the video to the target channel")]
    public async Task WhenIAssignTheVideoToTheChannel()
    {
        var ytId = await ResolveExistingVideoIdAsync();
        var channelId = _context.AssignTargetChannelId ?? (await ResolveTargetChannelAsync()).ChannelId;
        _context.Response = await _client.PostAsJsonAsync(
            $"/api/Videos/{ytId}/channel",
            new { channelId });
    }

    [When(@"I assign the video to a channel with ID ""(.*)""")]
    public async Task WhenIAssignTheVideoToAChannelWithId(string channelId)
    {
        var ytId = await ResolveExistingVideoIdAsync();
        _context.Response = await _client.PostAsJsonAsync(
            $"/api/Videos/{ytId}/channel",
            new { channelId });
    }

    [When(@"I assign video with ID ""(.*)"" to the channel")]
    public async Task WhenIAssignVideoWithIdToTheChannel(string ytId)
    {
        var channelId = _context.AssignTargetChannelId ?? (await ResolveTargetChannelAsync()).ChannelId;
        _context.Response = await _client.PostAsJsonAsync(
            $"/api/Videos/{ytId}/channel",
            new { channelId });
    }

    [Then(@"the channel should contain the video")]
    public async Task ThenTheChannelShouldContainTheVideo()
    {
        var ytId = _context.AssignVideoYoutubeId!;
        var channelId = _context.AssignTargetChannelId!;
        var updated = await _ytChannelRepository.GetItemsAsync(c => c.ChannelId == channelId);
        updated.Should().HaveCount(1);
        updated.First().Videos.Should().Contain(v => v.VideoId == ytId);
    }

    [Then(@"the channel should contain the video exactly once")]
    public async Task ThenTheChannelShouldContainTheVideoExactlyOnce()
    {
        var ytId = _context.AssignVideoYoutubeId!;
        var channelId = _context.AssignTargetChannelId!;
        var updated = (await _ytChannelRepository.GetItemsAsync(c => c.ChannelId == channelId)).First();
        updated.Videos.Count(v => v.VideoId == ytId).Should().Be(1);
    }

    [Then(@"the target channel should contain the video")]
    public async Task ThenTheTargetChannelShouldContainTheVideo()
    {
        var ytId = _context.AssignVideoYoutubeId!;
        var target = (await _ytChannelRepository.GetItemsAsync(c => c.ChannelId == _context.AssignTargetChannelId)).First();
        target.Videos.Should().Contain(v => v.VideoId == ytId);
    }

    [Then(@"the source channel should contain the video")]
    public async Task ThenTheSourceChannelShouldContainTheVideo()
    {
        var ytId = _context.AssignVideoYoutubeId!;
        var source = (await _ytChannelRepository.GetItemsAsync(c => c.ChannelId == _context.AssignSourceChannelId)).First();
        source.Videos.Should().Contain(v => v.VideoId == ytId);
    }
}
