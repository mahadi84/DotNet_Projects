using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CBS.Application.DTO;

namespace CBS.Infrastructure;

public class AuditReportPDF : IDocument
{
    public IEnumerable<AuditReportViewDTO> Data { get; }
    public AuditReportPDF(IEnumerable<AuditReportViewDTO> data) => Data = data;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape()); // রিপোর্ট বড় হলে Landscape ভালো
            page.Margin(1, Unit.Centimetre);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeTable);
            page.Footer().AlignCenter().Text(x => {
                x.Span("Page ");
                x.CurrentPageNumber();
            });
        });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row => {
            row.RelativeItem().Column(col => {
                col.Item().Text("Audit Report").FontSize(20).SemiBold().FontColor(Colors.Green.Medium);
                col.Item().Text($"Generated on: {DateTime.Now:dd-MMM-yyyy HH:mm}");
            });
        });
    }

    void ComposeTable(IContainer container)
    {
        container.Table(table => {
            table.ColumnsDefinition(columns => {
                columns.ConstantColumn(100); // Timestamp
                columns.ConstantColumn(60);  // Branch
                columns.ConstantColumn(50);  // User
                columns.ConstantColumn(60);  // Action
                columns.ConstantColumn(60);  // OldValue
                columns.ConstantColumn(60);  // NewValue
                columns.RelativeColumn();    // Description/Values
            });

            table.Header(header => {
                header.Cell().Element(CellStyle).Text("Timestamp");
                header.Cell().Element(CellStyle).Text("Branch");
                header.Cell().Element(CellStyle).Text("User");
                header.Cell().Element(CellStyle).Text("Action");
                header.Cell().Element(CellStyle).Text("OldValue");
                header.Cell().Element(CellStyle).Text("NewValue");
                header.Cell().Element(CellStyle).Text("Description");

                static IContainer CellStyle(IContainer container) =>
                    container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1);
            });

            foreach (var item in Data)
            {
                table.Cell().Element(ContentStyle).Text(item.CreatedAt.ToString("dd-MMM-yyyy HH:mm"));
                table.Cell().Element(ContentStyle).Text(item.BranchCode);
                table.Cell().Element(ContentStyle).Text(item.CreatedBy.ToString());
                table.Cell().Element(ContentStyle).Text(item.Action);
                table.Cell().Element(ContentStyle).Text(item.OldValue);
                table.Cell().Element(ContentStyle).Text(item.NewValue);
                table.Cell().Element(ContentStyle).Text($"{item.TableName}: {item.Description}");

                static IContainer ContentStyle(IContainer container) =>
                    container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).DefaultTextStyle(x => x.FontSize(9)); 
            }
        });
    }
}