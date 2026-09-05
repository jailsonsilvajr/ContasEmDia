namespace ContasEmDia.Application.Ports;

public interface ICurrentDateProvider
{
    DateOnly GetCurrentDate();
}
