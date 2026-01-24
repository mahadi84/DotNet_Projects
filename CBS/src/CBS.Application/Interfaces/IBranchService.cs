using CBS.Domain.Common;
using CBS.Application.DTO;


namespace CBS.Application.Interfaces;

public interface IBranchService
{
    Task<Result<dynamic>> CreateBranchAsync(BranchCreateDTO bdto, int UserID);
   
    Task<Result<BranchSearchDTO>> GetByBranchCodeAsync(string branchCode, int UserID);
    Task<Result<bool>> UpdateBranchAsync(BranchUpdateDTO dto, int UserID);
}
    

