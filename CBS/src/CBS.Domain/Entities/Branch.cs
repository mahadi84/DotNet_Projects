using CBS.Domain.Common;

public class Branch
{
    // Properties are private set to ensure data modification only via domain logic
    public int Id { get; private set; }
    public string BranchCode { get; private set; }
    public string BranchName { get; private set; }
    public decimal VaultBalance { get; private set; }
    public int RowVersion { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int CreatedBy { get; private set; }
    public int ApprovedBy { get; private set; }
    public int? UpdatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; private set; }

    // Private constructor for Dapper mapping and internal use
    private Branch() { }

    // Static Factory Method: Ensures all business rules (e.g., Min Balance 500) are met during creation
    public static Branch Create(string code, string name, decimal initialBalance, int creatorId)
    {
        if (initialBalance < 500) throw new Exception("Initial balance must be 500+");

        return new Branch
        {
            BranchCode = code,
            BranchName = name,
            VaultBalance = initialBalance,
            CreatedBy = creatorId,
            CreatedAt = DateTime.Now,
            IsActive = true,
            RowVersion = 1
        };
    }

    // Reconstruction Method: Used to restore an existing Branch object from database records
    public static Branch Reconstruct(int id, string code, string name, decimal balance, int version, bool active)
    {
        return new Branch
        {
            Id = id,
            BranchCode = code,
            BranchName = name,
            VaultBalance = balance,
            RowVersion = version,
            IsActive = active
        };
    }

    // Update Info: Handles general branch profile updates (Balance is set, not incremented)
    public void UpdateGeneralInfo(string code, string name, decimal finalBalance, int updaterId)
    {
        if (finalBalance < 500) throw new Exception("Vault balance cannot be less than 500");

        this.BranchCode = code;
        this.BranchName = name;
        this.VaultBalance = finalBalance;
        this.UpdatedBy = updaterId;
        this.UpdatedAt = DateTime.Now;
    }

    // Execute Transaction: Handles Deposits/Withdrawals (Balance is incremented or decremented)
    public Result<bool> ExecuteTransaction(decimal amount)
    {
        if (amount == 0) return Result<bool>.Failure("Amount cannot be zero");

        if (amount > 0) // Deposit Logic
        {
            VaultBalance += amount;
        }
        else // Withdrawal Logic (amount is negative)
        {
            decimal absAmount = Math.Abs(amount);
            if ((VaultBalance - absAmount) < 500)
                return Result<bool>.Failure("Insufficient balance! Min 500 required.");

            VaultBalance -= absAmount;
        }
        return Result<bool>.Success(true);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}




























//using CBS.Domain.Common;

//public class Branch
//{
//    public int Id { get; set; }
//    public string BranchCode { get; set; }
//    public string BranchName { get; set; }
//    public decimal VaultBalance { get; private set; } // শুধুমাত্র মেথড দিয়ে পরিবর্তন হবে
//    public int RowVersion { get; set; }
//    public bool IsActive { get; private set; } = true;
//    public int CreatedBy { get; set; }
//    public int ApprovedBy { get; set; }
//    public int? UpdatedBy { get; set; }
//    public DateTime CreatedAt { get; set; } = DateTime.Now;
//    public DateTime? UpdatedAt { get; set; }




//    // Create a new Branch
//    public static Branch Create(string code, string name, decimal initialBalance, int creatorId)
//    {
//        if (initialBalance < 500) throw new Exception("Initial balance must be 500+");

//        return new Branch
//        {
//            BranchCode = code,
//            BranchName = name,
//            VaultBalance = initialBalance,
//            CreatedBy = creatorId,
//            CreatedAt = DateTime.Now,
//            IsActive = true
//        };
//    }





//    //Update Info (Balance `=` NOT `+=`)
//    public void UpdateGeneralInfo(string code, string name, decimal finalBalance, int updaterId)
//    {
//        if (finalBalance < 500) throw new Exception("Vault balance cannot be less than 500");

//        this.BranchCode = code;
//        this.BranchName = name;
//        this.VaultBalance = finalBalance; // এখানে সরাসরি নতুন ভ্যালু বসবে (এটিই ডাবল হওয়া আটকাবে)
//        this.UpdatedBy = updaterId;
//        this.UpdatedAt = DateTime.Now;
//    }





//    // Deposit/Withdraw (`+=` OR `-=`) 
//    public Result<bool> ExecuteTransaction(decimal amount)
//    {
//        if (amount <= 0) return Result<bool>.Failure("Amount cannot be zero or negetive");

//        if (amount > 0) // Deposit
//        {
//            VaultBalance += amount;
//        }
//        else // Withdraw (amount is negative)
//        {
//            decimal absAmount = Math.Abs(amount);
//            if ((VaultBalance - absAmount) < 500)
//                return Result<bool>.Failure("Insufficient balance! Min 500 required.");

//            VaultBalance -= absAmount;
//        }
//        return Result<bool>.Success(true);
//    }


//    public void Deactivate() => IsActive = false;
//    public void Activate() => IsActive = true;
//}