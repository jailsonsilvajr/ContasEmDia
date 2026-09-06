using System.Net;
using System.Text.Json;

namespace ContasEmDia.Api.Tests;

public sealed class SwaggerDocumentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerDocumentTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSwaggerJson_DescribesRecurringExpensesEndpointAndItsTypes()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.True(root.GetProperty("paths").TryGetProperty("/api/v1/recurring-expenses", out _));

        var schemas = root.GetProperty("components").GetProperty("schemas");
        Assert.Contains(schemas.EnumerateObject(), s => s.Name.Contains("CreateRecurringExpenseDataRequest"));
        Assert.Contains(schemas.EnumerateObject(), s => s.Name.Contains("CreateRecurringExpenseDataResponse"));
    }

    [Fact]
    public async Task GetSwaggerJson_DeclaresAllThreeDocumentedResponsesForTheEndpoint()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var responses = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/recurring-expenses")
            .GetProperty("post")
            .GetProperty("responses");

        Assert.True(responses.TryGetProperty("201", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("500", out _));
    }
}
