using CBS.Domain.Common;
using CBS.Application.DTO;


namespace CBS.Application.Interfaces;

public interface IBranchService
{
    Task<Result<BranchResponseDTO>> CreateBranchAsync(BranchCreateDTO bdto, int UserID);
   
    Task<Result<SearchBranchByCodeDTO>> SearchBranchByCodeAsync(string branchCode, int UserID);
    Task<Result<bool>> UpdateBranchAsync(BranchUpdateDTO dto, int UserID);



    //IEnumerable<T> to hold a collection of items like List<T>, Array, HashSet<T>, use loops to iterate through the collection
    Task<Result<IEnumerable<GetAllBranchNameAndCodeDTO>>> GetAllBranchNameAndCodeAsync();
}
    

