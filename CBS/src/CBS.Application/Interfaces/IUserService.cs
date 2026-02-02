using CBS.Application.DTO;
using CBS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserResponseDTO>> CreateUserAsync(UserCreateDTO dto, int currentUserId);
    Task<Result<UserSearchDTO>> GetByUsernameAsync(string username, int currentUserId);
    //Task<Result<bool>> UpdateUserAsync(UserUpdateDTO dto, int currentUserId);


    // Login will enforce lock after 3 wrong attempts
    //Task<Result<LoginResultDTO>> LoginAsync(UserLoginDTO dto);

    Task<Result<IEnumerable<GetAllUsersIdAndNameDTO>>> GetAllUsersIdAndNameAsync();
}