using CBS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Domain.Entities;

public class AppUser
{
    public int Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Checker;
    public int BranchId { get; private set; }

    public int FailedAttempts { get; private set; } = 0;
    public DateTime? LockUntil { get; private set; }
    public bool IsLocked { get; private set; } = false;

    public bool IsActive { get; private set; } = true;
    public DateTime? LastLogin { get; private set; }

    public int CreatedBy { get; private set; }
    public int? UpdatedBy { get; private set; }
    public int? ApprovedBy { get; private set; }

    public int RowVersion { get; private set; } = 1;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private AppUser() { }





    // Create user with domain validation (single source of truth)
    public static AppUser Create(string username, string passwordHash, UserRole role, int branchId, int createdBy)
    {
        username= ValidateUsername(username);
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new Exception("Password hash is missing.");
        if (branchId <= 0) throw new Exception("BranchId is required.");

        return new AppUser
        {
            Username = username,
            PasswordHash = passwordHash,
            Role = role,
            BranchId = branchId,
            FailedAttempts = 0,
            LockUntil = null,
            IsLocked  = false,
            IsActive  = true,
            LastLogin = null,
            CreatedBy = createdBy,
            RowVersion = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
        };
    }



 // a static method to reconstruct the object or you map it
    public static AppUser Reconstruct(int id, string username, UserRole role, int branchId, bool isActive, bool isLocked, int rowVersion, int failedAttempts)
    {
        return new AppUser
        {
            Id = id,
            Username = username,
            Role = role,
            BranchId = branchId,
            IsActive = isActive,
            IsLocked = isLocked,
            RowVersion = rowVersion,
            FailedAttempts = failedAttempts
        };
    }

    // Update general info; password hash optional
    public void UpdateGeneralInfo(string username, UserRole role, int branchId, bool isActive, bool isLocked, int rowVersion, int updatedBy)
    {
        username = ValidateUsername(username);
        if (branchId <= 0) throw new Exception("BranchId is required.");
        if (RowVersion <= 0) throw new Exception("RowVersion is missing");

        Username = username;
        Role     = role;
        IsActive = isActive;
        BranchId = branchId;
        IsLocked = isLocked;
        RowVersion = rowVersion + 1;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.Now;

        //if (!string.IsNullOrWhiteSpace(newPasswordHash))
        //    PasswordHash = newPasswordHash;
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
        IsLocked  = false;
        LockUntil = null;
        LastLogin = nowUtc;
    }

    // Unlock manually (admin)
    public void Unlock()
    {
        FailedAttempts = 0;
        IsLocked  = false;
        LockUntil = null;
    }




    private static string ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Username is required.");


        //Normalization: Trim whitespace and convert to Lower Case to prevent duplicates
        var normalized_username = username.Trim().ToLowerInvariant();

        if (normalized_username.Length < 5 || normalized_username.Length > 15)
            throw new Exception("Username must be 5-15 characters.");

        // Optional: enforce same rule as DTO regex (letters+numbers only)
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized_username, "^[a-z0-9]{5,15}$"))
            throw new Exception("Username format invalid.");

        return normalized_username;
    }




}
