namespace FlowLedger.Application.Common.Csv;

public interface ICsvExportService
{
    CsvResult Export<T>(string fileName, IReadOnlyList<T> rows, IReadOnlyList<CsvColumn<T>> columns);
}
