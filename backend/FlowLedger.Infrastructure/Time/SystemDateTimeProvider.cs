using FlowLedger.Application.Common;

namespace FlowLedger.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
