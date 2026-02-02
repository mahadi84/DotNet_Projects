using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Domain.Entities;

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Maker;
    public int? BranchId { get; private set; }

    public int FailedAttempts { get; private set; } = 0;
    public DateTime? LockUntil { get; private set; }
    public bool IsLocked { get; private set; } = false;

    public bool IsActive { get; private set; } = true;
    public DateTime? LastLogin { get; private set; }

    public int CreatedBy { get; set; }
    public int ApprovedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }





    // Create user with domain validation (single source of truth)
    public static AppUser Create(string username, string passwordHash, UserRole role, int branchId)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Username is required.");
        if (username.Length < 3 || username.Length > 50) throw new Exception("Username must be 3-50 characters.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new Exception("Password hash is missing.");
        if (branchId == null) throw new Exception("BranchId is required.");

        return new AppUser
        {
            Username = username.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            BranchId = branchId,
            FailedAttempts = 0,
            LockUntil = null,
            IsLocked = false,
            IsActive = true,
            LastLogin = null
        };
    }

    // Update general info; password hash optional
    public void UpdateGeneralInfo(string username, UserRole role, int branchId, bool isActive, string? newPasswordHash = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Username is required.");
        if (username.Length < 3 || username.Length > 50) throw new Exception("Username must be 3-50 characters.");
        if (branchId == null) throw new Exception("BranchId is required.");

        Username = username.Trim();
        Role = role;
        IsActive = isActive;
        BranchId = branchId;
        IsActive = isActive;

        if (!string.IsNullOrWhiteSpace(newPasswordHash))
            PasswordHash = newPasswordHash;
    }

    // Called when password is wrong
    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockDuration, DateTime nowUtc)
    {
        FailedAttempts += 1;

        // If reached max attempts => lock account
        if (FailedAttempts >= maxAttempts)
        {
            IsLocked = true;
            LockUntil = nowUtc.Add(lockDuration);
        }
    }

    // Called when login successful
    public void RegisterSuccessfulLogin(DateTime nowUtc)
    {
        FailedAttempts = 0;
        IsLocked = false;
        LockUntil = null;
        LastLogin = nowUtc;
    }

    // Unlock manually (admin)
    public void Unlock()
    {
        FailedAttempts = 0;
        IsLocked = false;
        LockUntil = null;
    }






}
