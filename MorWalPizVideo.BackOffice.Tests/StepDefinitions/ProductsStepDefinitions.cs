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
public class ProductsStepDefinitions
{
    private readonly HttpClient _client;
    private readonly TestScenarioContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProductsStepDefinitions(BackOfficeWebApplicationFactory factory, TestScenarioContext context)
    {
        _client = factory.CreateClient();
        _context = context;
    }

    [Given(@"a product exists in the system")]
    public async Task GivenAProductExistsInTheSystem()
    {
        var response = await _client.GetAsync("/api/Products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<Dictionary<string, JsonElement>>>(JsonOptions);

        // Pick an existing product with a non-null id, otherwise seed a new one
        var existing = items?.FirstOrDefault(x =>
            x.TryGetValue("id", out var idElem)
            && idElem.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(idElem.GetString()));

        if (existing != null)
        {
            _context.ExistingProductId = existing["id"].GetString();
        }
        else
        {
            var createRequest = new
            {
                Title = $"Seed Product {Guid.NewGuid():N}",
                Description = "Seed description",
                Url = "https://example.com/seed",
                CategoryIds = Array.Empty<string>()
            };
            var createResponse = await _client.PostAsJsonAsync("/api/Products", createRequest);
            createResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var listAgain = await _client.GetFromJsonAsync<List<Dictionary<string, JsonElement>>>(
                "/api/Products", JsonOptions);
            _context.ExistingProductId = listAgain!
                .Where(x => x.TryGetValue("id", out var i)
                    && i.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(i.GetString()))
                .Select(x => x["id"].GetString())
                .FirstOrDefault();
        }

        _context.ExistingProductId.Should().NotBeNullOrEmpty();
    }

    [When(@"I request all products")]
    public async Task WhenIRequestAllProducts()
    {
        _context.Response = await _client.GetAsync("/api/Products");
    }

    [When(@"I request the product by its ID")]
    public async Task WhenIRequestTheProductByItsId()
    {
        _context.ExistingProductId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.GetAsync($"/api/Products/{_context.ExistingProductId}");
    }

    [When(@"I request a product with ID ""(.*)""")]
    public async Task WhenIRequestAProductWithId(string id)
    {
        _context.Response = await _client.GetAsync($"/api/Products/{id}");
    }

    [When(@"I create a product with title ""(.*)"" and description ""(.*)"" and url ""(.*)""")]
    public async Task WhenICreateAProductWith(string title, string description, string url)
    {
        var request = new
        {
            Title = title,
            Description = description,
            Url = url,
            CategoryIds = Array.Empty<string>()
        };
        _context.Response = await _client.PostAsJsonAsync("/api/Products", request);
    }

    [When(@"I update the product with new title ""(.*)""")]
    public async Task WhenIUpdateTheProductWithNewTitle(string newTitle)
    {
        _context.ExistingProductId.Should().NotBeNullOrEmpty();
        var updateBody = new
        {
            Title = newTitle,
            Description = "Updated description",
            Url = "https://example.com/updated",
            CategoryIds = Array.Empty<string>()
        };
        _context.Response = await _client.PutAsJsonAsync(
            $"/api/Products/{_context.ExistingProductId}", updateBody);
    }

    [When(@"I update a product with ID ""(.*)""")]
    public async Task WhenIUpdateAProductWithId(string id)
    {
        var updateBody = new
        {
            Title = "ignored",
            Description = "ignored",
            Url = "https://example.com/ignored",
            CategoryIds = Array.Empty<string>()
        };
        _context.Response = await _client.PutAsJsonAsync($"/api/Products/{id}", updateBody);
    }

    [When(@"I delete the product")]
    public async Task WhenIDeleteTheProduct()
    {
        _context.ExistingProductId.Should().NotBeNullOrEmpty();
        _context.Response = await _client.DeleteAsync($"/api/Products/{_context.ExistingProductId}");
    }

    [When(@"I delete a product with ID ""(.*)""")]
    public async Task WhenIDeleteAProductWithId(string id)
    {
        _context.Response = await _client.DeleteAsync($"/api/Products/{id}");
    }

    [Then(@"the response should contain a list of products")]
    public async Task ThenTheResponseShouldContainAListOfProducts()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().StartWith("[");
    }

    [Then(@"the response should contain the product details")]
    public async Task ThenTheResponseShouldContainTheProductDetails()
    {
        _context.Response.Should().NotBeNull();
        var content = await _context.Response!.Content.ReadAsStringAsync();
        content.Should().Contain("title");
        content.Should().Contain("url");
    }

    [Then(@"the product should have the new title")]
    public async Task ThenTheProductShouldHaveTheNewTitle()
    {
        var response = await _client.GetAsync($"/api/Products/{_context.ExistingProductId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var doc = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(JsonOptions);
        doc!["title"].GetString().Should().Be("Updated Product Title");
    }
}
