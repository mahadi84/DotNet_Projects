using Microsoft.EntityFrameworkCore;

namespace Login.Models
{
    // এই ক্লাসটি পেজিনেশনের জন্য ডেটা এবং পেজ মেটাডেটা ধরে রাখে।
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        // পূর্ববর্তী পেজ আছে কিনা তা চেক করে
        public bool HasPreviousPage => PageIndex > 1;

        // পরবর্তী পেজ আছে কিনা তা চেক করে
        public bool HasNextPage => PageIndex < TotalPages;

        // অ্যাসিঙ্ক্রোনাসভাবে PaginatedList তৈরি করে
        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync(); // মোট আইটেম গণনা
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        // IQueryable না থাকলে, কিন্তু পেজিং লজিক পেতে চাইলে (শুধুমাত্র ডিজাইনের জন্য)
        public static PaginatedList<T> Create(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}