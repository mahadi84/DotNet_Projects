using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;



namespace RestAPI.Models
{

    public class Emp
    {
        [Key] // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // এটি 1, 2, 3... হিসাবে তৈরি হবে
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        [Required]
        public string Designation { get; set; }


        [Required(ErrorMessage = "Email is required")]
        // কাস্টম রেগুলার এক্সপ্রেশন: এটি নিশ্চিত করে যে ইমেলের লোকাল পার্ট (অ্যাট চিহ্নের আগে)
        // কমপক্ষে একটি অক্ষর বা সংখ্যা দিয়ে শুরু হয় এবং @ ও . চিহ্ন আছে।
        [RegularExpression(@"^[a-zA-Z0-9]+[a-zA-Z0-9\._\-]*@[a-zA-Z0-9\.\-]+\.[a-zA-Z]{2,4}$",
           ErrorMessage = "Email must contain characters, numbers, @, and .")]
        public string Email { get; set; }


        [Required]
        public int UserId { get; set; }
    }

}



