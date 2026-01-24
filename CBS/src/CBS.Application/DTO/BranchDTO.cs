using System.ComponentModel.DataAnnotations;


namespace CBS.Application.DTO;


public record BranchCreateDTO(
    [Required(ErrorMessage ="Branch Name Required")]
    [RegularExpression(@"^[a-zA-Z\s]{3,50}$", ErrorMessage = " Branch Name must be 3-50 characters long and contain only letters and spaces")]
    string BranchName, 
    
    [Required(ErrorMessage ="Branch Code Required")]
    [RegularExpression(@"^\d{3,5}$", ErrorMessage = "Branch Code, Only 3-5 digits allowed")]
    string BranchCode, 
    
    [Required(ErrorMessage ="Branch Vault Required")]
    [Range(500, 1000000000, ErrorMessage = "Vault balance must be at least 500 Taka")]
    decimal VaultBalance
    );




//public record BranchSearchDTO(
//    int Id,
//    string BranchName,
//    string BranchCode,
//    decimal VaultBalance,
//    int RowVersion
//);

public class BranchSearchDTO
{
    public int Id { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public decimal VaultBalance { get; set; }
    public int RowVersion { get; set; }
}






// Used for updating data form
public record BranchUpdateDTO(
    [Required(ErrorMessage = "Id is missing")]
    int Id,

    [Required(ErrorMessage ="Branch Name Required")]
    [RegularExpression(@"^[a-zA-Z\s]{3,50}$", ErrorMessage = "Branch Name must be 3-50 characters long and contain only letters and spaces")]
    string BranchName,

    [Required(ErrorMessage ="Branch Code Required")]
    // Changed to string to allow leading zeros (e.g., '00123')
    [RegularExpression(@"^\d{3,20}$", ErrorMessage = "Branch Code must be 3-20 digits")]
    string BranchCode,

    [Required(ErrorMessage ="Branch Vault Required")]
    [Range(500, 1000000000, ErrorMessage = "Vault balance must be at least 500 Taka")]
    decimal VaultBalance,

    [Required(ErrorMessage = "RowVersion is missing")]
    // RowVersion is a number, so we don't need a Regex for it
    int RowVersion
);

