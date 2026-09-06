using ContasEmDia.Api.Mappings;
using ContasEmDia.Api.Requests;
using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using Microsoft.AspNetCore.Mvc;

namespace ContasEmDia.Api.Controllers;

[ApiController]
[Route("api/v1/recurring-expenses")]
public sealed class RecurringExpensesController : ControllerBase
{
    private readonly ICreateRecurringExpenseUseCase _createRecurringExpenseUseCase;

    public RecurringExpensesController(ICreateRecurringExpenseUseCase createRecurringExpenseUseCase)
    {
        _createRecurringExpenseUseCase = createRecurringExpenseUseCase;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateRecurringExpenseDataResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CreateRecurringExpenseDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateRecurringExpenseDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] CreateRecurringExpenseDataRequest request)
    {
        var output = await _createRecurringExpenseUseCase.ExecuteAsync(request.ToUseCaseInput());

        if (!output.IsSuccess)
        {
            return BadRequest(ApiResponse<CreateRecurringExpenseDataResponse>.Failure(output.Errors.ToApiErrors()));
        }

        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreateRecurringExpenseDataResponse>.Success(output.ToDataResponse()));
    }
}
