using CBS.Domain.Common;

namespace CBS.Domain.Entities;

public class Branch
{
    public int Id { get; set; }
    public string BranchCode { get; set; } // string allows leading zeros like '00123'
    public string BranchName { get; set; }
    public decimal VaultBalance { get; private set; }
    public int RowVersion { get; set; }
    public bool IsActive { get; private set; } = true;

    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; } // টেবিলে updated_by আছে
    public int? ApprovedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }



    // ১. টাকা জমা করার মেথড (Credit/Deposit)
    public Result<bool> DepositToVault(decimal amount)
    {
        if (amount <= 0)
            return Result<bool>.Failure("Deposit amount must be greater than zero!");

        VaultBalance += amount; // ব্যালেন্স বাড়বে
        return Result<bool>.Success(true);
    }

    // ২. টাকা তুলে নেওয়ার মেথড (Debit/Withdraw)
    public Result<bool> WithdrawFromVault(decimal amount)
    {
        if (amount <= 0)
            return Result<bool>.Failure("Withdraw amount must be greater than zero!");

        // চেক করা হচ্ছে টাকা তোলার পর ৫০০ টাকার নিচে নেমে যাবে কি না
        if ((VaultBalance - amount) < 500)
            return Result<bool>.Failure("Insufficient balance! Minimum 500 must remain in vault.");

        VaultBalance -= amount; // ব্যালেন্স কমবে
        return Result<bool>.Success(true);
    }




    // Vault Balance Update Logic
    public Result<bool> UpdateVaultBalance(decimal amount)
    {
        // 1. if positive Deposit
        if (amount > 0)
        {
            return DepositToVault(amount); // এখানে 'decimal' লিখার দরকার নেই
        }

        // ২. if neg. Withdraw method
        if (amount < 0)
        {
            decimal absoluteAmount = Math.Abs(amount);
            return WithdrawFromVault(absoluteAmount);
        }

        return Result<bool>.Failure("Amount cannot be zero!");
    }






    public void UpdateInfo(string code, string name, int updatedBy, int? approvedBy = null)
    {
        BranchCode = code;
        BranchName = name;
        UpdatedBy = updatedBy; 

        if (approvedBy.HasValue)
        {
            ApprovedBy = approvedBy;
        }
    }


    // Logice for Soft Delete
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}