using System.Text.Json;
using ContasEmDia.Api.Middlewares;
using ContasEmDia.Api.Responses;
using ContasEmDia.Application.Ports;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Application.UseCases.GetMonthlyPanel;
using ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;
using ContasEmDia.Application.UseCases.UndoOccurrencePayment;
using ContasEmDia.Domain.Repositories;
using ContasEmDia.Infrastructure;
using ContasEmDia.Infrastructure.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContasEmDiaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ContasEmDia")));

builder.Services.AddScoped<IRepositoryManager, RepositoryManager>();
builder.Services.AddScoped<ICreateRecurringExpenseUseCase, CreateRecurringExpenseUseCase>();
builder.Services.AddScoped<IGetMonthlyPanelUseCase, GetMonthlyPanelUseCase>();
builder.Services.AddScoped<IMarkOccurrenceAsPaidUseCase, MarkOccurrenceAsPaidUseCase>();
builder.Services.AddScoped<IUndoOccurrencePaymentUseCase, UndoOccurrencePaymentUseCase>();
builder.Services.AddSingleton<ICurrentDateProvider, SystemCurrentDateProvider>();

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                new ApiError(Field: JsonNamingPolicy.CamelCase.ConvertName(entry.Key), Message: error.ErrorMessage)))
            .ToList();

        return new BadRequestObjectResult(ApiResponse<object>.Failure(errors));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ContasEmDiaDbContext>();
    if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
    {
        dbContext.Database.Migrate();
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program;
