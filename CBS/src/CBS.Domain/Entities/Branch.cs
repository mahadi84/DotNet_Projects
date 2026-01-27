using CBS.Domain.Common;

public class Branch
{
    public int Id { get; set; }
    public string BranchCode { get; set; }
    public string BranchName { get; set; }
    public decimal VaultBalance { get; private set; } // শুধুমাত্র মেথড দিয়ে পরিবর্তন হবে
    public int RowVersion { get; set; }
    public bool IsActive { get; private set; } = true;
    public int CreatedBy { get; set; }
    public int ApprovedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }




    // Create a new Branch
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
            IsActive = true
        };
    }

   
    
    
    
    //Update Info (Balance `=` NOT `+=`)
    public void UpdateGeneralInfo(string code, string name, decimal finalBalance, int updaterId)
    {
        if (finalBalance < 500) throw new Exception("Vault balance cannot be less than 500");

        this.BranchCode = code;
        this.BranchName = name;
        this.VaultBalance = finalBalance; // এখানে সরাসরি নতুন ভ্যালু বসবে (এটিই ডাবল হওয়া আটকাবে)
        this.UpdatedBy = updaterId;
        this.UpdatedAt = DateTime.Now;
    }

    
    
    
    
    // Deposit/Withdraw (`+=` OR `-=`) 
    public Result<bool> ExecuteTransaction(decimal amount)
    {
        if (amount <= 0) return Result<bool>.Failure("Amount cannot be zero or negetive");

        if (amount > 0) // Deposit
        {
            VaultBalance += amount;
        }
        else // Withdraw (amount is negative)
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