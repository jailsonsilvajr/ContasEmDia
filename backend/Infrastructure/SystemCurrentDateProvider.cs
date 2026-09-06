using ContasEmDia.Application.Ports;

namespace ContasEmDia.Infrastructure;

public sealed class SystemCurrentDateProvider : ICurrentDateProvider
{
    public DateOnly GetCurrentDate() => DateOnly.FromDateTime(DateTime.Now);
}
