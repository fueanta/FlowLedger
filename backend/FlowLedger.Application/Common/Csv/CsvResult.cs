using System.Text;

namespace FlowLedger.Application.Common.Csv;

public sealed record CsvResult(string FileName, string Content)
{
    public const string ContentType = "text/csv; charset=utf-8";

    public byte[] ToBytes() => Encoding.UTF8.GetBytes(Content);
}
