using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using CBS.Domain.Entities;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


using Microsoft.AspNetCore.Authentication; // .SignInAsync, AuthenticationProperties এবং .SignOutAsync এর জন্য
using Microsoft.AspNetCore.Authentication.Cookies; // CookieAuthenticationDefaults এর জন্য
using Microsoft.AspNetCore.Http; // HttpContext এর জন্য


using Dapper;

namespace CBS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly MySqlDataSource _dataSource;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogService _auditLogService;


    private const int MAX_FAILED_ATTEMPTS = 3;
    private readonly TimeSpan LOCK_DURATION = TimeSpan.FromMinutes(15);



    public AuthService(MySqlDataSource dataSource, IHttpContextAccessor httpContextAccessor, IAuditLogService auditLogService)
    {
        _dataSource = dataSource;
        _httpContextAccessor = httpContextAccessor;
        _auditLogService = auditLogService;
    }






    public async Task<Result<UserSessionDTO>> LoginAsync(LoginRequestDTO dto)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        // 1. Fetch user by username
        const string sql = "SELECT * FROM users WHERE username = @Username";
        var userRow = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Username = dto.Username.Trim().ToLower() });

        if (userRow == null) 
            return Result<UserSessionDTO>.Failure("Invalid credentials.");

        // 2. Reconstruct Domain Entity
        var user = AppUser.Reconstruct(
            (int)userRow.id, (string)userRow.username,
            Enum.Parse<UserRole>(userRow.role), (int)userRow.branch_id,
            (bool)userRow.is_active, (bool)userRow.is_locked, (int)userRow.row_version, (int)userRow.failed_attempts
        );

        // 3. Check if account is locked
        if (user.IsLocked && userRow.lock_until > DateTime.UtcNow)
            return Result<UserSessionDTO>.Failure($"Account locked until {userRow.lock_until:HH:mm}");

        // 4. Verify Password (Using BCrypt)
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, (string)userRow.password_hash);

        // Get client IP for logging
        var ip = GetClientIp();

        if (!isPasswordValid)
        {

            user.RegisterFailedLogin(MAX_FAILED_ATTEMPTS, LOCK_DURATION, DateTime.UtcNow);
            await UpdateUserSecurityStatus(conn, user, user.LockUntil);

            await _auditLogService.CreateAuditLogAsync(
        new AuditLogCreateDTO(
            BranchCode: "", // if you have branch code, put it here
            TableName: "users",
            Action: "LoginFailed",
            UpdatedBy: null,
            ApprovedBy: null,
            OldValue: "Login attempt",
            NewValue: $"Username:{user.Username}, IP:{ip}, FailedAttempts:{user.FailedAttempts}/{MAX_FAILED_ATTEMPTS}, Locked:{user.IsLocked}, LockUntil:{user.LockUntil:O}",
            Description: "Login failed: wrong password",
            CreatedBy: user.Id // or null/system id if you want
        ), conn, null );

            return Result<UserSessionDTO>.Failure($"Invalid credentials. Attempt {user.FailedAttempts}/{MAX_FAILED_ATTEMPTS}");

          
        }



        var auditRes = await _auditLogService.CreateAuditLogAsync(new AuditLogCreateDTO(
            BranchCode: "", // Using the fetched code
             TableName: "users",
             Action: "LoginSuccess",
             UpdatedBy: null,
             ApprovedBy: null,
             OldValue: "Login Successfull",
             NewValue: $"Username:{user.Username}, IP:{ip}, Role:{user.Role}, Branch:{user.BranchId}",
             Description: "Login Successfull",
             CreatedBy: 1
         ), conn, null);

        //if (!auditRes.IsSuccess)
        //{
        //    return Result<bool>.Failure("failed");
        //}







        // 5. Success Logic
        user.RegisterSuccessfulLogin(DateTime.UtcNow);
        await UpdateUserSecurityStatus(conn, user, user.LockUntil);

        // 6. Create Claims & Sign In (The "Session" part)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("BranchId", user.BranchId.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await _httpContextAccessor.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return Result<UserSessionDTO>.Success(new UserSessionDTO(user.Id, user.Username, user.Role.ToString(), user.BranchId));
    }






    private async Task UpdateUserSecurityStatus(MySqlConnection conn, AppUser user, DateTime? lockUntil)
    {
        const string sql = @"UPDATE users
                         SET failed_attempts = @FA, is_locked = @IL,
                             lock_until = @LU, last_login = @LL
                         WHERE id = @Id";
        await conn.ExecuteAsync(sql, new
        {
            FA = user.FailedAttempts,
            IL = user.IsLocked,
            LU = lockUntil,
            LL = user.LastLogin,
            Id = user.Id
        });
    }





    //Get client IP for logging
    private string GetClientIp()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http == null) return "unknown";

        var addr = http.Connection.RemoteIpAddress;
        if (addr == null) return "unknown";

        // IPv6 loopback ::1 -> 127.0.0.1, IPv6 mapped -> IPv4
        var ip = addr.MapToIPv4().ToString();

        // extra safety: never allow 0.0.0.0/0.0.0.1 style odd values
        if (ip == "0.0.0.0" || ip == "0.0.0.1")
            ip = "127.0.0.1";

        return ip;
    }




    public async Task LogoutAsync() =>
        await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);






}