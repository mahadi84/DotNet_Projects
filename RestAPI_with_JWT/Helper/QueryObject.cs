namespace RestAPI.Helper
{
    public class QueryObject
    {
        // ঐচ্ছিক ফিল্টার প্যারামিটার - মান না দিলে null হবে
        public string? Email { get; set; }
        public string? Name { get; set; }

        // ঐচ্ছিক সর্টিং প্যারামিটার - মান না দিলে null হবে
        public string? SortBy { get; set; }

        // সর্টিং দিক: ডিফল্টভাবে ascending (false)
        public bool IsDecsending { get; set; } = false;

        // পেজিনেশন প্যারামিটার: ডিফল্ট পেজ ১
        public int PageNumber { get; set; } = 1;

        // পেজিনেশন প্যারামিটার: ডিফল্ট পেজ সাইজ ১০
        public int PageSize { get; set; } = 10;
    }
}