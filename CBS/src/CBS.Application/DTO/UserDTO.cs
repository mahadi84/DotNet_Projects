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

       [Required(ErrorMessage ="Branch Code Required")]
       [RegularExpression(@"^\d{3,5}$", ErrorMessage = "Branch Code, Only 3-5 digits allowed")]
        string BranchCode

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
    public string Username { get; set; } = "";
    public UserRole Role { get; set; }
    public string BranchCode { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockUntil { get; set; }
    public bool IsLocked { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }
}




public record UserUpdateDTO(
    [Required] int Id,
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required] UserRole Role,
    [Required, StringLength(50, MinimumLength = 3)] string BranchCode,
    [Required] bool IsActive,

    // optional password reset by admin
    [StringLength(100, MinimumLength = 6)] string? NewPassword
);




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
    string BranchCode,
    bool IsLocked,
    DateTime? LockUntil,
    string Message
);
