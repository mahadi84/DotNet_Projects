using ClosedXML.Excel;
using WesterUnionPD.Models;

namespace WesterUnionPD.Services;

/// <summary>
/// Creates Excel (.xlsx) from aggregated data.
/// </summary>
public sealed class ExcelExportService : IExcelExportService
{
    public byte[] CreateExcel(List<BranchSummary> summaries)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BranchTotals");

        ws.Cell(1, 1).Value = "ABD Code";
        ws.Cell(1, 2).Value = "Charges LOC";
        ws.Cell(1, 3).Value = "FX LOC";
        ws.Cell(1, 4).Value = "Grand Total";

        ws.Range(1, 1, 1, 4).Style.Font.Bold = true;

        int r = 2;
        foreach (var s in summaries)
        {
            ws.Cell(r, 1).Value = s.AbdCode;
            ws.Cell(r, 2).Value = s.ChargesLOC;
            ws.Cell(r, 3).Value = s.FxLOC;
            ws.Cell(r, 4).Value = s.GrandTotal;
            r++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
