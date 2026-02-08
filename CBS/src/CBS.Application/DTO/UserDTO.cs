using CBS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Application.DTO;





public record UserCreateDTO(
        [Required(ErrorMessage ="User Name Required")]
        [RegularExpression(@"^[a-zA-Z0-9]{5,15}$", ErrorMessage = "Username must be 5-15 characters long and contain only letters and numbers.")]
        string Username,

        [Required(ErrorMessage ="Password is Required")]
        [RegularExpression(@"^[a-zA-Z0-9._-]{5,50}$", ErrorMessage = "Password must be 5-50 with (letters, numbers, Special character")]
        string Password,

       [Required]
        UserRole Role,

      [Required(ErrorMessage = "Branch Code Required")]
      [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Must be a positive number")]
      int BranchId
    
       

   );




//Return newly created data to show in the view
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




//Search first
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
    public int? ApprovedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public int RowVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}




//and show searched data in the edit-form to validate submission
public class UserUpdateDTO
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 5)]
    public string Username { get; set; }

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    [Required(ErrorMessage = "Branch ID Required")]
    [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Must be a positive number")]
    public int BranchId { get; set; }


    public string? BranchCode { get; set; }



    public bool IsActive { get; set; }

    public bool IsLocked { get; set; }

    public int FailedAttempts { get; set; }


    // Auditing Fields (Usually Read-Only in the UI)
    public int CreatedBy { get; set; }

    public int ApprovedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public int RowVersion { get; set; }

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
