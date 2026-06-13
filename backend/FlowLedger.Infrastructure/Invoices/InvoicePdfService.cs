using System.Globalization;
using System.Text;
using FlowLedger.Application.Common;
using FlowLedger.Application.Invoices;

namespace FlowLedger.Infrastructure.Invoices;

public sealed class InvoicePdfService : IInvoicePdfService
{
    private readonly IInvoiceService _invoiceService;

    public InvoicePdfService(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public async Task<InvoicePdfResult> GenerateAsync(Guid id, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, currentUser, cancellationToken);
        var content = SimpleInvoicePdfWriter.Write(invoice);

        return new InvoicePdfResult($"{invoice.InvoiceNumber}.pdf", content);
    }
}

internal static class SimpleInvoicePdfWriter
{
    public static byte[] Write(InvoiceDetailDto invoice)
    {
        var lines = BuildLines(invoice);
        var stream = BuildContentStream(lines);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };

        return BuildPdf(objects);
    }

    private static IReadOnlyList<PdfLine> BuildLines(InvoiceDetailDto invoice)
    {
        var lines = new List<PdfLine>
        {
            new("FlowLedger", 40, 744, 14),
            new("INVOICE", 40, 716, 22),
            new(invoice.InvoiceNumber, 40, 690, 16),
            new($"Status: {invoice.Status}", 430, 716, 11),
            new($"Issued: {FormatDate(invoice.IssuedAtUtc)}", 430, 698, 11),
            new($"Due: {FormatDate(invoice.DueAtUtc)}", 430, 680, 11),
            new($"Due period: {invoice.DueDays} days", 430, 662, 11),
            new("Bill To", 40, 630, 13),
            new(invoice.Customer.Name, 40, 610, 11),
            new(invoice.Customer.ContactEmail, 40, 594, 10),
            new(invoice.Customer.BillingAddress, 40, 578, 10),
            new("Billing Request", 40, 540, 13),
            new($"{invoice.BillingRequest.RequestNumber} - {invoice.BillingRequest.Title}", 40, 520, 10),
            new($"Request status: {invoice.BillingRequest.Status}", 40, 504, 10),
            new("Description", 40, 458, 11),
            new("Amount", 470, 458, 11),
            new(invoice.BillingRequest.Title, 40, 430, 10),
            new(FormatMoney(invoice.SubtotalAmount), 470, 430, 10),
            new($"Subtotal: {FormatMoney(invoice.SubtotalAmount)}", 380, 370, 11),
            new($"VAT ({invoice.VatPercentage.ToString("0.##", CultureInfo.InvariantCulture)}%): {FormatMoney(invoice.VatAmount)}", 380, 350, 11),
            new($"Total: {FormatMoney(invoice.TotalAmount)}", 380, 326, 14)
        };

        if (invoice.PaidAtUtc is not null)
        {
            lines.Add(new($"Paid: {FormatDate(invoice.PaidAtUtc.Value)}", 430, 644, 11));
        }

        return lines;
    }

    private static string BuildContentStream(IEnumerable<PdfLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("q");
        builder.AppendLine("0.93 0.95 0.98 rg 36 670 540 92 re f");
        builder.AppendLine("0.90 0.90 0.90 RG 36 466 540 1 re S");
        builder.AppendLine("0.90 0.90 0.90 RG 36 416 540 1 re S");
        builder.AppendLine("0 g");

        foreach (var line in lines)
        {
            builder.AppendLine("BT");
            builder.AppendLine($"/F1 {line.FontSize.ToString(CultureInfo.InvariantCulture)} Tf");
            builder.AppendLine($"{line.X.ToString(CultureInfo.InvariantCulture)} {line.Y.ToString(CultureInfo.InvariantCulture)} Td");
            builder.AppendLine($"({Escape(line.Text)}) Tj");
            builder.AppendLine("ET");
        }

        builder.AppendLine("Q");
        return builder.ToString();
    }

    private static byte[] BuildPdf(IReadOnlyList<string> objects)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Root 1 0 R /Size {objects.Count + 1} >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return stream.ToArray();
    }

    private static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatMoney(decimal value) => $"BDT {value.ToString("#,0.##", CultureInfo.InvariantCulture)}";

    private static string Escape(string value)
    {
        var ascii = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
        return ascii.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private sealed record PdfLine(string Text, int X, int Y, int FontSize);
}
