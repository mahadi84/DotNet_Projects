using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;




public class User
{
    [Key] // Primary Key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // এটি 1, 2, 3... হিসাবে তৈরি হবে
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Username { get; set; }

    [Required]
    public string PasswordHash { get; set; } // হ্যাশড পাসওয়ার্ড সংরক্ষণের জন্য


    [Required]
    [MaxLength(50)]
    public string Role { get; set; }
}

