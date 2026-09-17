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

    private static object ValidBody(string startDate, string? endDate = null, string status = "Active", string? note = null) => new
    {
        name = "Aluguel",
        category = "Housing",
        monthlyAmount = 1850.00m,
        dueDay = 10,
        startDate,
        endDate = endDate ?? LastDayOfMonth(startDate),
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

    private static string LastDayOfMonth(string isoDate)
    {
        var date = DateOnly.ParseExact(isoDate, "yyyy-MM-dd");
        return new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)).ToString("yyyy-MM-dd");
    }

    private static string NMonthsFromNowFirstDay(int n)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return new DateOnly(today.Year, today.Month, 1).AddMonths(n).ToString("yyyy-MM-dd");
    }

    private static string NMonthsFromNowLastDay(int n)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var target = new DateOnly(today.Year, today.Month, 1).AddMonths(n);
        return new DateOnly(target.Year, target.Month, DateTime.DaysInMonth(target.Year, target.Month)).ToString("yyyy-MM-dd");
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
    public async Task Post_ValidPausedBodyInCurrentCompetencia_Returns201WithOneOccurrence()
    {
        // Decision 1 (refinamento data-fim-despesa-recorrente): generation at cadastro no longer
        // checks status — a Paused despesa generates its vigência's occurrences just like Active.
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(FirstDayOfCurrentMonth(), status: "Paused"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").GetProperty("occurrences").GetArrayLength());
    }

    [Fact]
    public async Task Post_ValidPausedBodyWithFutureCompetencia_Returns201WithNoOccurrences()
    {
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(FirstDayOfNextMonth(), status: "Paused"));

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
            endDate = LastDayOfMonth(FirstDayOfCurrentMonth()),
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
            endDate = LastDayOfMonth(FirstDayOfCurrentMonth()),
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

    [Fact]
    public async Task Post_MissingEndDateField_Returns400WithEndDateFieldError()
    {
        var payload = new
        {
            name = "Aluguel",
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
        var errors = root.GetProperty("errors");

        Assert.Contains(errors.EnumerateArray(), error =>
            error.GetProperty("field").GetString() == "endDate" &&
            error.GetProperty("message").GetString() == "Data de fim é obrigatória.");
    }

    [Fact]
    public async Task Post_EndDateOnOrBeforeStartDate_Returns400WithEndDateDomainMessage()
    {
        var startDate = FirstDayOfCurrentMonth();
        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(startDate, endDate: startDate));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;
        var errors = root.GetProperty("errors");

        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("endDate", errors[0].GetProperty("field").GetString());
        Assert.Equal("A data de fim deve ser posterior à data de início.", errors[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Post_EndDateBeyondOneYearFromStartDate_Returns400WithEndDateDomainMessage()
    {
        var startDate = FirstDayOfCurrentMonth();
        var beyondOneYear = DateOnly.ParseExact(startDate, "yyyy-MM-dd").AddYears(1).AddDays(1).ToString("yyyy-MM-dd");

        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(startDate, endDate: beyondOneYear));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;
        var errors = root.GetProperty("errors");

        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("endDate", errors[0].GetProperty("field").GetString());
        Assert.Equal("A vigência não pode ultrapassar 1 ano a partir da data de início.", errors[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Post_EndDateCompetenciaBeforeCurrentReferencePeriod_Returns400WithEndDateDomainMessage()
    {
        var startDate = NMonthsFromNowFirstDay(-6);
        var pastEndDate = NMonthsFromNowLastDay(-1);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(startDate, endDate: pastEndDate));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var root = body!.RootElement;
        var errors = root.GetProperty("errors");

        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("endDate", errors[0].GetProperty("field").GetString());
        Assert.Equal("A data de fim não pode estar no passado.", errors[0].GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Paused")]
    public async Task Post_ValidVigenciaSpanningMultipleCompetencias_Returns201WithOneOccurrencePerCompetenciaAndEndDateEchoed(string status)
    {
        var startDate = FirstDayOfCurrentMonth();
        var endDate = NMonthsFromNowLastDay(3);

        var response = await _client.PostAsJsonAsync(Endpoint, ValidBody(startDate, endDate: endDate, status: status));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var data = body!.RootElement.GetProperty("data");

        Assert.Equal(endDate, data.GetProperty("endDate").GetString());
        Assert.Equal(4, data.GetProperty("occurrences").GetArrayLength());
    }
}
