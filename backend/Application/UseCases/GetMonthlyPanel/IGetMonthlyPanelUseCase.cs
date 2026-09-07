namespace ContasEmDia.Application.UseCases.GetMonthlyPanel;

public interface IGetMonthlyPanelUseCase
{
    Task<GetMonthlyPanelUseCaseOutput> ExecuteAsync(GetMonthlyPanelUseCaseInput input);
}
