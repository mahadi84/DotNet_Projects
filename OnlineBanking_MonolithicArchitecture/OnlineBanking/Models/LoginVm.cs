using System.ComponentModel.DataAnnotations;

namespace OnlineBanking.Models;

public sealed class LoginVm
{
    [Required, StringLength(5, MinimumLength = 5)]
    public string AccountNumber { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
