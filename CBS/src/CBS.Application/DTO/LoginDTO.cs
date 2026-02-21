using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Application.DTO;



public record LoginRequestDTO(
    [Required] string Username,
    [Required][DataType(DataType.Password)] string Password
);

// to keep data into session after successfull Login 
public record UserSessionDTO(
    int Id,
    string Username,
    string Role,
    int BranchId
);