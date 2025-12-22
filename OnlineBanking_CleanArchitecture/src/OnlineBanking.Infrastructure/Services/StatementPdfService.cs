using OnlineBanking.Application.Contracts;
using OnlineBanking.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Infrastructure.Services
{
    public sealed class StatementPdfService : IStatementPdfService
    {
        public byte[] BuildMiniStatementPdf(string currency, string tzId, string accountNumber, string customerName, decimal currentBalance, IReadOnlyList<Transaction> last10)
        {
            // QuestPDF community license use
            QuestPDF.Settings.License = LicenseType.Community;

            // timezone resolve (e.g., Asia/Dhaka)
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);

            // UTC time কে local time (Dhaka) তে convert করে string বানায়
            string ToLocal(DateTimeOffset utc)
            {
                var local = TimeZoneInfo.ConvertTime(utc, tz);
                return local.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // PDF document build
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // page margin
                    page.Margin(30);

                    // A4 size
                    page.Size(PageSizes.A4);

                    // Header section (statement info)
                    page.Header().Column(col =>
                    {
                        // title
                        col.Item().Text("Mini Statement (Last 10 Transactions)").FontSize(18).SemiBold();

                        // account info
                        col.Item().Text($"Account: {accountNumber}").FontSize(12);

                        // customer name
                        col.Item().Text($"Customer: {customerName}").FontSize(12);

                        // current balance with currency
                        col.Item().Text($"Current Balance: {currency}{currentBalance:0.00}").FontSize(12);

                        // time zone info
                        col.Item().Text($"Time Zone: {tzId}").FontSize(10).FontColor(Colors.Grey.Darken2);

                        // separator line
                        col.Item().LineHorizontal(1);
                    });

                    // Content: transactions table
                    page.Content().Table(table =>
                    {
                        // 3 columns: DateTime | Type | Amount
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3); // datetime column wide
                            columns.RelativeColumn(2); // type
                            columns.RelativeColumn(2); // amount
                        });

                        // table header
                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text($"Date & Time ({tzId})").SemiBold();
                            header.Cell().Element(CellStyle).Text("Type").SemiBold();
                            header.Cell().Element(CellStyle).Text("Amount").SemiBold();
                        });

                        // rows: last10 tx ordered latest first
                        foreach (var t in last10.OrderByDescending(x => x.CreatedAtUtc))
                        {
                            // local time show
                            table.Cell().Element(CellStyle).Text(ToLocal(t.CreatedAtUtc));

                            // enum as string
                            table.Cell().Element(CellStyle).Text(t.Type.ToString());

                            // amount with currency
                            table.Cell().Element(CellStyle).Text($"{currency}{t.Amount:0.00}");
                        }

                        // cell styling helper
                        static IContainer CellStyle(IContainer container) =>
                            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6);
                    });

                    // Footer: generated time (UTC)
                    page.Footer().AlignRight().Text($"Generated (UTC): {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
                });
            });

            // PDF byte array generate করে return
            return doc.GeneratePdf();
        }
    }
}
