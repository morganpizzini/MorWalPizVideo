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
public class CompilationsStepDefinitions
{
    private readonly HttpClient _client;
    private readonly TestScenarioContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CompilationsStepDefinitions(BackOfficeWebApplicationFactory factory, TestScenarioContext context)
    {
        _client = factory.CreateClient();
        _context = context;
    }

    [Given(@"a compilation exists in the system")]
    public async Task GivenACompilationExistsInTheSystem()
    {
        // First check if there are any existing compilations in mock data
        var response = await _client.GetAsync("/api/Compilations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var compilations = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(content, JsonOptions);
        
        if (compilations != null && compilations.Count > 0)
        {
            // Use an existing compilation
            _context.ExistingCompilationId = compilations[0]["id"].GetString();
        }
        else
        {
            // Create a new compilation for testing
            var createRequest = new
            {
                Title = "Test Compilation for Scenario",
                Description = "Created for testing purposes",
                Url = "test-scenario-compilation",
                Videos = Array.Empty<string>()
            };

            var createResponse = await _client.PostAsJsonAsync("/api/Compilations", createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createdCompilation = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(createContent, JsonOptions);
            _context.ExistingCompilationId = createdCompilation?["id"].GetString();
        }

        _context.ExistingCompilationId.Should().NotBeNullOrEmpty();
    }

    [When(@"I request all compilations")]
    public async Task WhenIRequestAllCompilations()
    {
        _context.Response = await _client.GetAsync("/api/Compilations");
    }

    [When(@"I request the compilation by its ID")]
    public async Task WhenIRequestTheCompilationByItsId()
    {
        _context.ExistingCompilationId.Should().NotBeNullOrEmpty("A compilation must exist before requesting it");
        _context.Response = await _client.GetAsync($"/api/Compilations/{_context.ExistingCompilationId}");
    }

    [When(@"I request a compilation with ID ""(.*)""")]
    public async Task WhenIRequestACompilationWithId(string id)
    {
        try
        {
            _context.Response = await _client.GetAsync($"/api/Compilations/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in GetAsync: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [When(@"I create a compilation with title ""(.*)"" and description ""(.*)"" and url ""(.*)""")]
    public async Task WhenICreateACompilationWithTitleAndDescriptionAndUrl(string title, string description, string url)
    {
        var request = new
        {
            Title = title,
            Description = description,
            Url = url,
            Videos = Array.Empty<string>()
        };

        _context.Response = await _client.PostAsJsonAsync("/api/Compilations", request);
        
        if (_context.Response.StatusCode == HttpStatusCode.Created)
        {
            var content = await _context.Response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {content}");
            
            // Try to extract ID from Location header first
            if (_context.Response.Headers.Location != null)
            {
                var locationPath = _context.Response.Headers.Location.ToString();
                Console.WriteLine($"Location Header: {locationPath}");
                var segments = locationPath.Split('/');
                _context.CreatedCompilationId = segments[^1]; // Get last segment
            }
            else
            {
                // Fall back to reading from response body
                var compilation = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content, JsonOptions);
                _context.CreatedCompilationId = compilation?["id"].GetString();
            }
            
            Console.WriteLine($"Extracted ID: {_context.CreatedCompilationId}");
        }
    }

    [When(@"I create a compilation with empty title")]
    public async Task WhenICreateACompilationWithEmptyTitle()
    {
        var request = new
        {
            Title = "",
            Description = "Test",
            Url = "test",
            Videos = Array.Empty<string>()
        };

        _context.Response = await _client.PostAsJsonAsync("/api/Compilations", request);
    }

    [When(@"I update the compilation with new title ""(.*)""")]
    public async Task WhenIUpdateTheCompilationWithNewTitle(string newTitle)
    {
        _context.ExistingCompilationId.Should().NotBeNullOrEmpty("A compilation must exist before updating it");
        
        var request = new
        {
            Title = newTitle,
            Description = "Updated Description",
            Url = "updated-url",
            Videos = Array.Empty<string>()
        };

        _context.Response = await _client.PutAsJsonAsync($"/api/Compilations/{_context.ExistingCompilationId}", request);
    }

    [When(@"I update a compilation with ID ""(.*)""")]
    public async Task WhenIUpdateACompilationWithId(string id)
    {
        var request = new
        {
            Title = "Test",
            Description = "Test",
            Url = "test",
            Videos = Array.Empty<string>()
        };

        _context.Response = await _client.PutAsJsonAsync($"/api/Compilations/{id}", request);
    }

    [When(@"I delete the compilation")]
    public async Task WhenIDeleteTheCompilation()
    {
        _context.ExistingCompilationId.Should().NotBeNullOrEmpty("A compilation must exist before deleting it");
        
        _context.Response = await _client.DeleteAsync($"/api/Compilations/{_context.ExistingCompilationId}");
    }

    [When(@"I delete a compilation with ID ""(.*)""")]
    public async Task WhenIDeleteACompilationWithId(string id)
    {
        _context.Response = await _client.DeleteAsync($"/api/Compilations/{id}");
    }

    [Then(@"the response should be Created")]
    public void ThenTheResponseShouldBeCreated()
    {
        _context.Response.Should().NotBeNull();
        _context.Response!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Then(@"the response should be Bad Request")]
    public void ThenTheResponseShouldBeBadRequest()
    {
        _context.Response.Should().NotBeNull();
        _context.Response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Then(@"the response should be Not Found")]
    public void ThenTheResponseShouldBeNotFound()
    {
        _context.Response.Should().NotBeNull();
        _context.Response!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then(@"the response should contain a list of compilations")]
    public async Task ThenTheResponseShouldContainAListOfCompilations()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("["); // JSON array
    }

    [Then(@"the response should contain the compilation details")]
    public async Task ThenTheResponseShouldContainTheCompilationDetails()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("id");
        content.Should().Contain("title");
        content.Should().Contain("description");
    }

    [Then(@"the response should contain the compilation ID")]
    public async Task ThenTheResponseShouldContainTheCompilationId()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("id");
        
        _context.CreatedCompilationId.Should().NotBeNullOrEmpty();
    }
}