using System.ComponentModel.DataAnnotations;

namespace OnlineBanking.Models;

public sealed class TransferVm
{
    [Required, StringLength(5, MinimumLength = 5)]
    public string ToAccountNumber { get; set; } = "";

    [Required]
    [Range(0.01, 100000000)]
    public decimal Amount { get; set; }
}
