using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using MorWalPizVideo.Server.Services.Interfaces;
using Reqnroll;
using Xunit;

namespace MorWalPizVideo.BackOffice.Tests.StepDefinitions;

[Binding]
[Collection("WebAppFactory")]
public class ShortLinksStepDefinitions
{
    private readonly HttpClient _client;
    private readonly TestScenarioContext _context;
    private readonly YTChannelMockRepository _ytChannelRepository;
    private readonly ShortLinkMockRepository _shortLinkRepository;
    private readonly MatchMockRepository _matchRepository;
    public ShortLinksStepDefinitions(BackOfficeWebApplicationFactory factory, TestScenarioContext context)
    {
        _client = factory.CreateClient();
        _context = context;
        
        // Access the same repository instances used by the web application
        _ytChannelRepository = factory.YTChannelRepository!;
        _matchRepository = factory.MatchRepository!;
        _shortLinkRepository = factory.ShortLinkRepository!;
    }

    [Given(@"the application is running in mock mode")]
    public void GivenTheApplicationIsRunningInMockMode()
    {
        // The factory already configures mock mode
        _client.Should().NotBeNull();
    }

    [Given(@"a short link with code ""(.*)"" exists")]
    public async Task GivenAShortLinkWithCodeExists(string code)
    {
        // In mock mode, short links are pre-seeded, so we just verify it exists
        var response = await _client.GetAsync($"/api/ShortLinks/{code}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Given(@"a standalone short link exists")]
    public async Task GivenAStandaloneShortLinkExists()
    {
        var existing = (await _shortLinkRepository.GetItemsAsync()).FirstOrDefault();
        if(existing == null)
        {
            throw new Exception("No short links found in the repository. Ensure mock data is seeded correctly.");
        }
        // Extract the ID from the URL response
        _context.CreatedShortLinkId = existing.Id;
    }

    [When(@"I request all short links")]
    public async Task WhenIRequestAllShortLinks()
    {
        _context.Response = await _client.GetAsync("/api/ShortLinks");
    }

    [When(@"I request the short link with code ""(.*)""")]
    public async Task WhenIRequestTheShortLinkWithCode(string code)
    {
        _context.Response = await _client.GetAsync($"/api/ShortLinks/{code}");
    }

    [When(@"I create a short link with target ""(.*)"" and link type ""(.*)""")]
    public async Task WhenICreateAShortLinkWithTargetAndLinkType(string target, string linkType)
    {
        var request = new
        {
            Target = target,
            LinkType = linkType == "Other" ? 2 : 0,
            QueryLinkIds = Array.Empty<string>(),
            Message = ""
        };

        _context.Response = await _client.PostAsJsonAsync("/api/ShortLinks", request);
    }

    [When(@"I delete the short link")]
    public async Task WhenIDeleteTheShortLink()
    {
        _context.CreatedShortLinkId.Should().NotBeNullOrEmpty("A short link must be created before deletion");        
        _context.Response = await _client.DeleteAsync($"/api/ShortLinks/{_context.CreatedShortLinkId}");
    }

    [Then(@"the response should be successful")]
    public void ThenTheResponseShouldBeSuccessful()
    {
        _context.Response.Should().NotBeNull();
        _context.Response!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"the response should be No Content")]
    public void ThenTheResponseShouldBeNoContent()
    {
        _context.Response.Should().NotBeNull();
        _context.Response!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then(@"the response should contain a list of short links")]
    public async Task ThenTheResponseShouldContainAListOfShortLinks()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("["); // JSON array
    }

    [Then(@"the response should contain the short link details")]
    public async Task ThenTheResponseShouldContainTheShortLinkDetails()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("code");
        content.Should().Contain("target");
    }

    [Then(@"the response should contain message ""(.*)""")]
    public async Task ThenTheResponseShouldContainMessage(string message)
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().Contain(message);
    }

    [Then(@"the response should contain a short link URL")]
    public async Task ThenTheResponseShouldContainAShortLinkUrl()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("http://localhost/");
    }

    [Then(@"the response should contain short links from matches")]
    public async Task ThenTheResponseShouldContainShortLinksFromMatches()
    {
        _context.Response.Should().NotBeNull();
        var links = await _context.Response!.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        links.Should().NotBeNull();
        
        // Verify that at least one short link has LinkType = 0 (YouTubeVideo from matches)
        var hasYouTubeVideoLinks = links!.Any(link => 
            link.ContainsKey("linkType") && 
            link["linkType"].ToString() == "0");
        
        hasYouTubeVideoLinks.Should().BeTrue("Response should contain short links from matches with LinkType.YouTubeVideo");
    }

    [Then(@"the response should contain short links from channels")]
    public async Task ThenTheResponseShouldContainShortLinksFromChannels()
    {
        _context.Response.Should().NotBeNull();
        var links = await _context.Response!.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        links.Should().NotBeNull();
        
        // Verify that at least one short link has LinkType = 1 (YouTubeChannel from channels)
        var hasYouTubeChannelLinks = links!.Any(link => 
            link.ContainsKey("linkType") && 
            link["linkType"].ToString() == "1");
        
        hasYouTubeChannelLinks.Should().BeTrue("Response should contain short links from channels with LinkType.YouTubeChannel");
    }

    [Given(@"a match with embedded short link exists")]
    public async Task GivenAMatchWithEmbeddedShortLinkExists()
    {
        var match = (await _matchRepository.GetItemsAsync(x => x.ShortLinks.Length > 0)).FirstOrDefault();
        if(match == null)
        {
            throw new Exception("No match with embedded short links found in the repository. Ensure mock data is seeded correctly.");
        }
        _context.EmbeddedShortLinkId = match.ShortLinks[0].Id;
    }

    [Given(@"a channel with embedded short link exists")]
    public async Task GivenAChannelWithEmbeddedShortLinkExists()
    {
        var result = (await _ytChannelRepository.GetItemsAsync(x => x.ShortLinks.Length > 0)).FirstOrDefault();

        if(result == null)
        {
            throw new Exception("No channels with embedded short links found in the repository. Ensure mock data is seeded correctly.");
        }
        
        _context.EmbeddedShortLinkId = result.Id;
    }

    [Given(@"a match exists for short link creation")]
    public async Task GivenAMatchExistsForShortLinkCreation()
    {
        var entity = (await _matchRepository.GetItemsAsync()).FirstOrDefault();
        if (entity == null)
        {
            throw new Exception("No channels found in the repository. Ensure mock data is seeded correctly.");
        }
        // In mock mode, matches are pre-seeded
        // Store a known match ID for testing
        _context.TestMatchId = entity.Id;
    }

    [Given(@"a channel exists for short link creation")]
    public async Task GivenAChannelExistsForShortLinkCreation()
    {
        var entity = (await _ytChannelRepository.GetItemsAsync()).FirstOrDefault();
        if(entity== null)
        {
            throw new Exception("No channels found in the repository. Ensure mock data is seeded correctly.");
        }
        // In mock mode, channels are pre-seeded
        // Store a known channel ID for testing
        _context.TestChannelId = entity.ChannelId;
    }

    [When(@"I request the short link by its code")]
    public async Task WhenIRequestTheShortLinkByItsCode()
    {
        _context.EmbeddedShortLinkId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.GetAsync($"/api/ShortLinks/{_context.EmbeddedShortLinkId}");
    }

    [When(@"I update the embedded short link")]
    public async Task WhenIUpdateTheEmbeddedShortLink()
    {
        _context.EmbeddedShortLinkId.Should().NotBeNullOrEmpty();
        
        var updateRequest = new
        {
            Target = "updated-target",
            LinkType = 0, // Keep as YouTubeVideo
            QueryLinkIds = Array.Empty<string>()
        };

        _context.Response = await _client.PutAsJsonAsync(
            $"/api/ShortLinks/{_context.EmbeddedShortLinkId}",
            new { Id = _context.EmbeddedShortLinkId, Body = updateRequest });
    }

    [When(@"I delete the embedded short link by code")]
    public async Task WhenIDeleteTheEmbeddedShortLinkByCode()
    {
        _context.EmbeddedShortLinkId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.DeleteAsync($"/api/ShortLinks/{_context.EmbeddedShortLinkId}");
    }

    [When(@"I create a short link for the match")]
    public async Task WhenICreateAShortLinkForTheMatch()
    {
        _context.TestMatchId.Should().NotBeNullOrEmpty();
        
        var request = new
        {
            Target = _context.TestMatchId,
            LinkType = 0, // YouTubeVideo
            QueryLinkIds = Array.Empty<string>(),
            Message = ""
        };

        _context.Response = await _client.PostAsJsonAsync("/api/ShortLinks", request);
        
        if (_context.Response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await _context.Response.Content.ReadAsStringAsync();
            _context.CreatedShortLinkId = responseContent.Replace("http://localhost/", "").Trim('"');
        }
    }

    [When(@"I create a short link for the channel")]
    public async Task WhenICreateAShortLinkForTheChannel()
    {
        _context.TestChannelId.Should().NotBeNullOrEmpty();
        
        var request = new
        {
            Target = _context.TestChannelId,
            LinkType = 1, // YouTubeChannel
            QueryLinkIds = Array.Empty<string>(),
            Message = ""
        };

        _context.Response = await _client.PostAsJsonAsync("/api/ShortLinks", request);
        
        if (_context.Response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await _context.Response.Content.ReadAsStringAsync();
            _context.CreatedShortLinkId = responseContent.Replace("http://localhost/", "").Trim('"');
        }
    }

    [Then(@"the embedded short link should be updated in the match")]
    public async Task ThenTheEmbeddedShortLinkShouldBeUpdatedInTheMatch()
    {
        _context.Response.Should().NotBeNull();
        _context.Response!.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the update by fetching the short link again
        var verifyResponse = await _client.GetAsync($"/api/ShortLinks/{_context.EmbeddedShortLinkId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"the short link should be removed from the match")]
    public async Task ThenTheShortLinkShouldBeRemovedFromTheMatch()
    {
        _context.EmbeddedShortLinkId.Should().NotBeNullOrEmpty();
        
        // Verify deletion by trying to fetch the short link again
        var verifyResponse = await _client.GetAsync($"/api/ShortLinks/{_context.EmbeddedShortLinkId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then(@"the short link should be removed from the channel")]
    public async Task ThenTheShortLinkShouldBeRemovedFromTheChannel()
    {
        _context.EmbeddedShortLinkId.Should().NotBeNullOrEmpty();
        
        // Verify deletion by trying to fetch the short link again
        var verifyResponse = await _client.GetAsync($"/api/ShortLinks/{_context.EmbeddedShortLinkId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then(@"the short link should have LinkType YouTubeVideo")]
    public async Task ThenTheShortLinkShouldHaveLinkTypeYouTubeVideo()
    {
        _context.CreatedShortLinkId.Should().NotBeNullOrEmpty();
        
        // Fetch the created short link and verify LinkType
        var verifyResponse = await _client.GetAsync($"/api/ShortLinks/{_context.CreatedShortLinkId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var shortLink = await verifyResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        shortLink.Should().NotBeNull();
        shortLink!.Should().ContainKey("linkType");
        shortLink["linkType"].ToString().Should().Be("0", "LinkType should be YouTubeVideo (0)");
    }

    [Then(@"the short link should have LinkType YouTubeChannel")]
    public async Task ThenTheShortLinkShouldHaveLinkTypeYouTubeChannel()
    {
        _context.CreatedShortLinkId.Should().NotBeNullOrEmpty();
        
        
        // Fetch the created short link and verify LinkType
        var verifyResponse = await _client.GetAsync($"/api/ShortLinks/{_context.CreatedShortLinkId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var shortLink = await verifyResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        shortLink.Should().NotBeNull();
        shortLink!.Should().ContainKey("linkType");
        shortLink["linkType"].ToString().Should().Be("1", "LinkType should be YouTubeChannel (1)");
    }
}
