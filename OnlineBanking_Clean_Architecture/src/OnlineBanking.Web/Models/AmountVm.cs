using System.ComponentModel.DataAnnotations;

namespace OnlineBanking.Web.Models
{
    public sealed class AmountVm
    {
        [Required]
        [Range(0.01, 100000000)]
        public decimal Amount { get; set; }
    }
}
