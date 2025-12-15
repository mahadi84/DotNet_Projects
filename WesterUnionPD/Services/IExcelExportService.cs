using WesterUnionPD.Models;

namespace WesterUnionPD.Services;

public interface IExcelExportService
{
    byte[] CreateExcel(List<BranchSummary> summaries);
}
