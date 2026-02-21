using CBS.Application.DTO;
using CBS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Application.Interfaces;

public interface IAuthService
{
    // সফল হলে UserSessionDTO ফিরবে, ব্যর্থ হলে Result Error
    Task<Result<UserSessionDTO>> LoginAsync(LoginRequestDTO loginDto);
    Task LogoutAsync();
}