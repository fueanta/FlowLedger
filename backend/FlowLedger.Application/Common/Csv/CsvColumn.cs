namespace FlowLedger.Application.Common.Csv;

public sealed record CsvColumn<T>(string Header, Func<T, object?> Value);
