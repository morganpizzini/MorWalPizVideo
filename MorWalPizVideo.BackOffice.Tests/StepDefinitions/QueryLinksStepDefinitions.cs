using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MorWalPizVideo.BackOffice.Tests.Infrastructure;
using Reqnroll;
using Xunit;

namespace MorWalPizVideo.BackOffice.Tests.StepDefinitions;

[Binding]
[Collection("WebAppFactory")]
public class QueryLinksStepDefinitions
{
    private readonly HttpClient _client;
    private readonly TestScenarioContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public QueryLinksStepDefinitions(BackOfficeWebApplicationFactory factory, TestScenarioContext context)
    {
        _client = factory.CreateClient();
        _context = context;
    }

    [Given(@"a query link exists in the system")]
    public async Task GivenAQueryLinkExistsInTheSystem()
    {
        var response = await _client.GetAsync("/api/QueryLinks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<Dictionary<string, JsonElement>>>(JsonOptions);

        var first = items?.FirstOrDefault(x => x.TryGetValue("queryLinkId", out var id)
            && id.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(id.GetString()));

        if (first != null)
        {
            _context.ExistingQueryLinkId = first["queryLinkId"].GetString();
        }
        else
        {
            var createRequest = new { Title = $"Seed QL {Guid.NewGuid():N}", Value = "list=SEED" };
            var createResponse = await _client.PostAsJsonAsync("/api/QueryLinks", createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var listAgain = await _client.GetFromJsonAsync<List<Dictionary<string, JsonElement>>>(
                "/api/QueryLinks", JsonOptions);
            _context.ExistingQueryLinkId = listAgain!
                .Where(x => x.TryGetValue("queryLinkId", out var id)
                    && id.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(id.GetString()))
                .Select(x => x["queryLinkId"].GetString())
                .FirstOrDefault();
        }

        _context.ExistingQueryLinkId.Should().NotBeNullOrEmpty();
    }

    [When(@"I request all query links")]
    public async Task WhenIRequestAllQueryLinks()
    {
        _context.Response = await _client.GetAsync("/api/QueryLinks");
    }

    [When(@"I request the query link by its ID")]
    public async Task WhenIRequestTheQueryLinkByItsId()
    {
        _context.ExistingQueryLinkId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.GetAsync($"/api/QueryLinks/{_context.ExistingQueryLinkId}");
    }

    [When(@"I request a query link with ID ""(.*)""")]
    public async Task WhenIRequestAQueryLinkWithId(string id)
    {
        _context.Response = await _client.GetAsync($"/api/QueryLinks/{id}");
    }

    [When(@"I create a query link with title ""(.*)"" and value ""(.*)""")]
    public async Task WhenICreateAQueryLinkWith(string title, string value)
    {
        var request = new { Title = title, Value = value };
        _context.Response = await _client.PostAsJsonAsync("/api/QueryLinks", request);
    }

    [When(@"I update the query link with new title ""(.*)""")]
    public async Task WhenIUpdateTheQueryLinkWithNewTitle(string newTitle)
    {
        _context.ExistingQueryLinkId.Should().NotBeNullOrEmpty();
        var body = new { Title = newTitle, Value = "list=UPDATED" };
        _context.Response = await _client.PutAsJsonAsync(
            $"/api/QueryLinks/{_context.ExistingQueryLinkId}", body);
    }

    [When(@"I update a query link with ID ""(.*)""")]
    public async Task WhenIUpdateAQueryLinkWithId(string id)
    {
        var body = new { Title = "ignored", Value = "ignored" };
        _context.Response = await _client.PutAsJsonAsync($"/api/QueryLinks/{id}", body);
    }

    [When(@"I delete the query link")]
    public async Task WhenIDeleteTheQueryLink()
    {
        _context.ExistingQueryLinkId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.DeleteAsync($"/api/QueryLinks/{_context.ExistingQueryLinkId}");
    }

    [When(@"I delete a query link with ID ""(.*)""")]
    public async Task WhenIDeleteAQueryLinkWithId(string id)
    {
        _context.Response = await _client.DeleteAsync($"/api/QueryLinks/{id}");
    }

    [Then(@"the response should contain a list of query links")]
    public async Task ThenTheResponseShouldContainAListOfQueryLinks()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().StartWith("[");
    }

    [Then(@"the response should contain the query link details")]
    public async Task ThenTheResponseShouldContainTheQueryLinkDetails()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().Contain("title");
        content.Should().Contain("value");
    }

    [Then(@"the query link should appear in the list")]
    public async Task ThenTheQueryLinkShouldAppearInTheList()
    {
        var items = await _client.GetFromJsonAsync<List<Dictionary<string, JsonElement>>>(
            "/api/QueryLinks", JsonOptions);
        items.Should().NotBeNull();
        items!.Any(x => x["title"].GetString() == "Test QL").Should().BeTrue();
    }

    [Then(@"the query link should have the new title")]
    public async Task ThenTheQueryLinkShouldHaveTheNewTitle()
    {
        var response = await _client.GetAsync($"/api/QueryLinks/{_context.ExistingQueryLinkId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions);
        doc!["title"].GetString().Should().Be("Updated QL Title");
        doc["queryLinkId"].GetString().Should().Be(_context.ExistingQueryLinkId);
    }
}
