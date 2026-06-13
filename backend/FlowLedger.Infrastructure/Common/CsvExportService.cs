using System.Globalization;
using System.Text;
using FlowLedger.Application.Common.Csv;

namespace FlowLedger.Infrastructure.Common;

public sealed class CsvExportService : ICsvExportService
{
    public CsvResult Export<T>(string fileName, IReadOnlyList<T> rows, IReadOnlyList<CsvColumn<T>> columns)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", columns.Select(x => Escape(x.Header))));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", columns.Select(x => Escape(FormatValue(x.Value(row))))));
        }

        return new CsvResult(fileName, builder.ToString());
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            double number => number.ToString(CultureInfo.InvariantCulture),
            float number => number.ToString(CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
