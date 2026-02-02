using CBS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Application.DTO;





public record UserCreateDTO(
        [Required(ErrorMessage ="Branch Name Required")]
        [RegularExpression(@"^[a-zA-Z0-9]{5,50}$", ErrorMessage = "Username must be 5-50 characters long and contain only letters and numbers.")]
        string Username,

        [Required(ErrorMessage ="Password is Required")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{5,50}$", ErrorMessage = "Password must be 5-50 with (letters, numbers, Special character")]
        string Password,

       [Required]
        UserRole Role,

      [Required(ErrorMessage = "Branch ID Required")]
      [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Must be a positive number")]
      int BranchId
    
       

   );





public record UserResponseDTO(
    int Id,
    string Username,
    UserRole Role,
    string BranchCode,
    bool IsActive,
    bool IsLocked,
    int FailedAttempts,
    DateTime? LockUntil,
    DateTime? LastLogin
);





public class UserSearchDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public UserRole Role { get; set; }
    public int BranchId { get; set; }
    public string BranchCode { get; set; }
    public string BranchName { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockUntil { get; set; }
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
    public int CreatedBy { get; set; }
    public int ApprovedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}





public class UserUpdateDTO
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    [Required(ErrorMessage = "Branch ID Required")]
    [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Must be a positive number")]
    public int BranchId { get; set; }


    public string BranchCode { get; set; }



    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public int FailedAttempts { get; set; }

    // Password is usually optional in an Update DTO
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string? NewPassword { get; set; }

    // Auditing Fields (Usually Read-Only in the UI)
    public int CreatedBy { get; set; }

    public int ApprovedBy { get; set; }

    public int? UpdatedBy { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTime CreatedAt { get; set; }

    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
    public DateTime? UpdatedAt { get; set; }



}










public class GetAllUsersIdAndNameDTO
{
    public int Id { get; set; }
    public string UserName { get; set; }

    // Constructor with parameters
    public GetAllUsersIdAndNameDTO(int userId, string userName)
    {
        Id = userId;
        UserName = UserName;
    }
}










// Login input
public record UserLoginDTO(
    [Required] string Username,
    [Required] string Password
);




// Login output
public record LoginResultDTO(
    int UserId,
    string Username,
    UserRole Role,
    int BranchId,
    bool IsLocked,
    DateTime? LockUntil,
    string Message
);
