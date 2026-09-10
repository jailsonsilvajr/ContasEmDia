using System.ComponentModel.DataAnnotations;

namespace ContasEmDia.Api.Requests;

public sealed record UpdateRecurringExpenseDataRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Categoria é obrigatória.")]
    public string? Category { get; init; }

    [Required(ErrorMessage = "Valor previsto mensal é obrigatório.")]
    public decimal? MonthlyAmount { get; init; }

    [Required(ErrorMessage = "Dia de vencimento é obrigatório.")]
    public int? DueDay { get; init; }

    [Required(ErrorMessage = "Data de início é obrigatória.")]
    public string? StartDate { get; init; }

    [Required(ErrorMessage = "Status é obrigatório.")]
    public string? Status { get; init; }

    public string? Note { get; init; }
}
