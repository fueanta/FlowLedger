namespace FlowLedger.Application.Configuration;

public interface IAppSettingReader
{
    Task<string?> ReadValueAsync(string key, CancellationToken cancellationToken);
}
