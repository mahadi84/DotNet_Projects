using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace RestAPI.DTOs
{
    public class EmpUpdateDto
    {
        [MaxLength(256)]
        public string empName { get; set; }

        public string empDesignation { get; set; }


        [RegularExpression(
            @"^[a-zA-Z0-9]+[a-zA-Z0-9\._\-]*@[a-zA-Z0-9\.\-]+\.[a-zA-Z]{2,4}$",
            ErrorMessage = "Email must contain characters, numbers, @, and .")]
        public string empEmail { get; set; }
    }
}
