using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContasEmDia.Api.Tests.Controllers;

public sealed class RecurringExpensesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Endpoint = "/api/v1/recurring-expenses";

    private readonly HttpClient _client;

    public RecurringExpensesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object ValidBody(string startDate, string status = "Active", string? note = null) => new
    {
        name = "Aluguel",
        category = "Housing",
        monthlyAmount = 1850.00m,
        dueDay = 10,
        startDate,
        frequency = "Monthly",
        status,
        note
    };

    private static string FirstDayOfCurrentMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return new DateOnly(today.Year, today.Month, 1).ToString("yyyy-MM-dd");
    }

    private static string FirstDayOfNextMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return new DateOnly(today.Year, today.Month, 1).AddMonths(1).ToString("yyyy-MM-dd");
    }

    private static void AssertAllPropertyNamesAreCamelCase(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    Assert.True(
                        property.Name.Length == 0 || char.IsLower(property.Name[0]),
                        $"Property '{property.Name}' is not camelCase.");
                    AssertAllPropertyNamesAreCamelCase(property.Value);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AssertAllPropertyNamesAreCamelCase(item);
                }
                break;
        }
    }

    [Fact]
    public async Task Post_ValidActiveBodyInCurrentCompetencia_Returns201WithOneOccurrence()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(FirstDayOfCurrentMonth()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").GetProperty("occurrences").GetArrayLength());
    }

    [Fact]
    public async Task Post_ValidPausedBody_Returns201WithNoOccurrences()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(FirstDayOfCurrentMonth(), status: "Paused"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("data").GetProperty("occurrences").GetArrayLength());
    }

    [Fact]
    public async Task Post_ValidActiveBodyWithFutureCompetencia_Returns201WithNoOccurrences()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(FirstDayOfNextMonth()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("data").GetProperty("occurrences").GetArrayLength());
    }

    [Fact]
    public async Task Post_ValidBody_ResponseFieldNamesAreAllCamelCase()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(FirstDayOfCurrentMonth()));

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();

        AssertAllPropertyNamesAreCamelCase(body!.RootElement);
    }

    [Fact]
    public async Task Post_SingleBusinessRuleViolation_Returns400WithOneError()
    {
        var payload = new
        {
            name = "Aluguel",
            category = "CategoriaInexistente",
            monthlyAmount = 1850.00m,
            dueDay = 10,
            startDate = FirstDayOfCurrentMonth(),
            frequency = "Monthly",
            status = "Active",
            note = (string?)null
        };

        var response = await _client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);

        var errors = root.GetProperty("errors");
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("category", errors[0].GetProperty("field").GetString());
    }

    [Fact]
    public async Task Post_MultipleBusinessRuleViolations_Returns400WithAllErrorsAggregated()
    {
        var payload = new
        {
            name = "Aluguel",
            category = "CategoriaInexistente",
            monthlyAmount = -1m,
            dueDay = 10,
            startDate = FirstDayOfCurrentMonth(),
            frequency = "Monthly",
            status = "Active",
            note = (string?)null
        };

        var response = await _client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);

        var errors = root.GetProperty("errors");
        Assert.True(errors.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task Post_MissingRequiredNameField_Returns400WithNameFieldError()
    {
        var payload = new
        {
            category = "Housing",
            monthlyAmount = 1850.00m,
            dueDay = 10,
            startDate = FirstDayOfCurrentMonth(),
            frequency = "Monthly",
            status = "Active"
        };

        var response = await _client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("data").ValueKind);

        var errors = root.GetProperty("errors");
        Assert.Contains(errors.EnumerateArray(), error =>
            error.GetProperty("field").GetString() == "name" &&
            error.GetProperty("message").GetString() == "Nome é obrigatório.");
    }

    [Fact]
    public async Task Post_MissingRequiredMonthlyAmountField_Returns400NeverTreatingAbsenceAsZero()
    {
        var payload = new
        {
            name = "Aluguel",
            category = "Housing",
            dueDay = 10,
            startDate = FirstDayOfCurrentMonth(),
            frequency = "Monthly",
            status = "Active"
        };

        var response = await _client.PostAsJsonAsync(Endpoint, payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        var errors = root.GetProperty("errors");
        Assert.Contains(errors.EnumerateArray(), error => error.GetProperty("field").GetString() == "monthlyAmount");
    }

    [Fact]
    public async Task Post_ShapeOrPresenceFailure_IsNeverAspNetCoreValidationProblemDetailsAndIsInPortuguese()
    {
        var payload = new
        {
            category = "Housing",
            monthlyAmount = 1850.00m,
            dueDay = 10,
            startDate = FirstDayOfCurrentMonth(),
            frequency = "Monthly",
            status = "Active"
        };

        var response = await _client.PostAsJsonAsync(Endpoint, payload);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.False(root.TryGetProperty("title", out _));
        Assert.False(root.TryGetProperty("status", out _));
        Assert.False(root.TryGetProperty("traceId", out _));
        Assert.True(root.TryGetProperty("success", out _));
        Assert.True(root.TryGetProperty("errors", out _));
    }
}
