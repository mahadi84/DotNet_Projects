using OnlineBanking.Contracts;
using OnlineBanking.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OnlineBanking.Services;

public sealed class StatementPdfService : IStatementPdfService
{
    // Builds a PDF statement showing last 10 transactions + local time conversion
    public byte[] BuildMiniStatementPdf(string currency, string tzId, string accountNumber, string customerName, decimal currentBalance, IReadOnlyList<Transaction> last10)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        string ToLocal(DateTimeOffset utc)
        {
            var local = TimeZoneInfo.ConvertTime(utc, tz);
            return local.ToString("yyyy-MM-dd HH:mm:ss");
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);

                page.Header().Column(col =>
                {
                    col.Item().Text("Mini Statement (Last 10 Transactions)").FontSize(18).SemiBold();
                    col.Item().Text($"Account: {accountNumber}").FontSize(12);
                    col.Item().Text($"Customer: {customerName}").FontSize(12);
                    col.Item().Text($"Current Balance: {currency}{currentBalance:0.00}").FontSize(12);
                    col.Item().Text($"Time Zone: {tzId}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    col.Item().LineHorizontal(1);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text($"Date & Time ({tzId})").SemiBold();
                        header.Cell().Element(CellStyle).Text("Type").SemiBold();
                        header.Cell().Element(CellStyle).Text("Amount").SemiBold();
                    });

                    foreach (var t in last10.OrderByDescending(x => x.CreatedAtUtc))
                    {
                        table.Cell().Element(CellStyle).Text(ToLocal(t.CreatedAtUtc));
                        table.Cell().Element(CellStyle).Text(t.Type.ToString());
                        table.Cell().Element(CellStyle).Text($"{currency}{t.Amount:0.00}");
                    }

                    static IContainer CellStyle(IContainer container) =>
                        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6);
                });

                page.Footer().AlignRight().Text($"Generated (UTC): {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
            });
        });

        return doc.GeneratePdf();
    }
}
