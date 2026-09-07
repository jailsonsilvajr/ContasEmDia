using ContasEmDia.Api.Mappings;
using ContasEmDia.Api.Requests;
using ContasEmDia.Api.Responses;
using ContasEmDia.Application.UseCases.GetMonthlyPanel;
using ContasEmDia.Application.UseCases.MarkOccurrenceAsPaid;
using ContasEmDia.Application.UseCases.UndoOccurrencePayment;
using Microsoft.AspNetCore.Mvc;

namespace ContasEmDia.Api.Controllers;

[ApiController]
[Route("api/v1/occurrences")]
public sealed class OccurrencesController : ControllerBase
{
    private readonly IGetMonthlyPanelUseCase _getMonthlyPanelUseCase;
    private readonly IMarkOccurrenceAsPaidUseCase _markOccurrenceAsPaidUseCase;
    private readonly IUndoOccurrencePaymentUseCase _undoOccurrencePaymentUseCase;

    public OccurrencesController(
        IGetMonthlyPanelUseCase getMonthlyPanelUseCase,
        IMarkOccurrenceAsPaidUseCase markOccurrenceAsPaidUseCase,
        IUndoOccurrencePaymentUseCase undoOccurrencePaymentUseCase)
    {
        _getMonthlyPanelUseCase = getMonthlyPanelUseCase;
        _markOccurrenceAsPaidUseCase = markOccurrenceAsPaidUseCase;
        _undoOccurrencePaymentUseCase = undoOccurrencePaymentUseCase;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetMonthlyPanelDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<GetMonthlyPanelDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<GetMonthlyPanelDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] string? year, [FromQuery] string? month)
    {
        var output = await _getMonthlyPanelUseCase.ExecuteAsync(new GetMonthlyPanelUseCaseInput { Year = year, Month = month });

        if (!output.IsSuccess)
        {
            return BadRequest(ApiResponse<GetMonthlyPanelDataResponse>.Failure(output.Errors.ToApiErrors()));
        }

        return Ok(ApiResponse<GetMonthlyPanelDataResponse>.Success(output.ToDataResponse()));
    }

    [HttpPatch("{occurrenceId:guid}/payment")]
    [ProducesResponseType(typeof(ApiResponse<MarkOccurrenceAsPaidDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MarkOccurrenceAsPaidDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MarkOccurrenceAsPaidDataResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MarkOccurrenceAsPaidDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkAsPaid(Guid occurrenceId, [FromBody] MarkOccurrenceAsPaidDataRequest? request)
    {
        var input = (request ?? new MarkOccurrenceAsPaidDataRequest()).ToUseCaseInput(occurrenceId);
        var output = await _markOccurrenceAsPaidUseCase.ExecuteAsync(input);

        return Ok(ApiResponse<MarkOccurrenceAsPaidDataResponse>.Success(output.ToDataResponse()));
    }

    [HttpDelete("{occurrenceId:guid}/payment")]
    [ProducesResponseType(typeof(ApiResponse<UndoOccurrencePaymentDataResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UndoOccurrencePaymentDataResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UndoOccurrencePaymentDataResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UndoOccurrencePaymentDataResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UndoPayment(Guid occurrenceId)
    {
        var output = await _undoOccurrencePaymentUseCase.ExecuteAsync(new UndoOccurrencePaymentUseCaseInput { OccurrenceId = occurrenceId });

        return Ok(ApiResponse<UndoOccurrencePaymentDataResponse>.Success(output.ToDataResponse()));
    }
}
