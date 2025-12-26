using System.ComponentModel.DataAnnotations;

namespace OnlineBanking.Models;

public sealed class AdminResetPasswordVm
{
    [Required, StringLength(5, MinimumLength = 5)]
    public string AccountNumber { get; set; } = "";

    [Required, MinLength(6), MaxLength(100)]
    public string NewPassword { get; set; } = "";
}
