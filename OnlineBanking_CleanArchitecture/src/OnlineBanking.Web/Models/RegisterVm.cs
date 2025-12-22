using System.ComponentModel.DataAnnotations;

namespace OnlineBanking.Web.Models
{
    public sealed class RegisterVm
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = "";

        [Required, StringLength(100)]
        public string City { get; set; } = "";

        [Required, MinLength(6), MaxLength(100)]
        public string Password { get; set; } = "";
    }
}
