// Models/LoginViewModel.cs

using System.ComponentModel.DataAnnotations;

namespace Login.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "ইউজারনেম দিন।")]
        [Display(Name = "ইউজারনেম")]
        public string Username { get; set; }

        [Required(ErrorMessage = "পাসওয়ার্ড দিন।")]
        [DataType(DataType.Password)]
        [Display(Name = "পাসওয়ার্ড")]
        public string Password { get; set; }
    }
}