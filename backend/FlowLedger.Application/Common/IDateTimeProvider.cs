namespace FlowLedger.Application.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
