using ContasEmDia.Api.Mappings;
using ContasEmDia.Api.Requests;
using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.CreateRecurringExpense;
using ContasEmDia.Application.UseCases.GetRecurringExpenseById;
using ContasEmDia.Application.UseCases.UpdateRecurringExpense;
using Microsoft.AspNetCore.Mvc;

namespace ContasEmDia.Api.Controllers;

[ApiController]
[Route("api/v1/recurring-expenses")]
public sealed class RecurringExpensesController : ControllerBase
{
    private readonly ICreateRecurringExpenseUseCase _createRecurringExpenseUseCase;
    private readonly IGetRecurringExpenseByIdUseCase _getRecurringExpenseByIdUseCase;
    private readonly IUpdateRecurringExpenseUseCase _updateRecurringExpenseUseCase;

    public RecurringExpensesController(
        ICreateRecurringExpenseUseCase createRecurringExpenseUseCase,
        IGetRecurringExpenseByIdUseCase getRecurringExpenseByIdUseCase,
        IUpdateRecurringExpenseUseCase updateRecurringExpenseUseCase)
    {
        _createRecurringExpenseUseCase = createRecurringExpenseUseCase;
        _getRecurringExpenseByIdUseCase = getRecurringExpenseByIdUseCase;
        _updateRecurringExpenseUseCase = updateRecurringExpenseUseCase;
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var output = await _getRecurringExpenseByIdUseCase.ExecuteAsync(new GetRecurringExpenseByIdUseCaseInput { Id = id });

        return Ok(ApiResponse<RecurringExpenseDataResponse>.Success(output.RecurringExpense.ToDataResponse()));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<RecurringExpenseDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateRecurringExpenseDataRequest request)
    {
        var output = await _updateRecurringExpenseUseCase.ExecuteAsync(request.ToUseCaseInput(id));

        if (!output.IsSuccess)
        {
            return BadRequest(ApiResponse<RecurringExpenseDataResponse>.Failure(output.Errors.ToApiErrors()));
        }

        return Ok(ApiResponse<RecurringExpenseDataResponse>.Success(output.RecurringExpense!.ToDataResponse()));
    }
}
