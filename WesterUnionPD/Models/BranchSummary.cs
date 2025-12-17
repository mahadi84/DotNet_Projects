namespace WesterUnionPD.Models;

/// <summary>
/// Aggregated result per ABD branch.
/// </summary>
public sealed class BranchSummary   //sealed কিওয়ার্ডটি ব্যবহার করা হয়, এই ক্লাসটিকে অন্য কেউ Inherit করতে পারবে না।
{
    public int Id { get; set; }
    public Guid UploadJobId { get; set; } //Globally Unique Identifier

    public string AbdCode { get; set; } = "";
    public decimal ChargesLOC { get; set; }
    public decimal FxLOC { get; set; }

    public decimal GrandTotal => ChargesLOC + FxLOC;
}


/// Guid: এটি একটি ডেটা টাইপ (Globally Unique Identifier)। 
/// এটি সাধারণ int (১, ২, ৩...) আইডির বদলে একটি লম্বা ইউনিক স্ট্রিং তৈরি করে (যেমন: 550e8400-e29b-41d4-a716-446655440000)। 
/// এটি অত্যন্ত নিরাপদ এবং বড় সিস্টেমে ডুপ্লিকেট আইডি হওয়ার ঝুঁকি থাকে না।




.
