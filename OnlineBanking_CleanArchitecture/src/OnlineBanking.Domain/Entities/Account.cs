using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OnlineBanking.Domain.Entities
{
    public sealed class Account
    {
        //এটি অ্যাকাউন্টের Primary Key বা ইউনিক আইডি।
        //এটি প্রেডিক্ট করা অসম্ভব এবং মাল্টিপল ডেটাবেস বা মাইক্রোসার্ভিসে ডেটা মার্জ করার সময় আইডির সংঘর্ষ (Collision) হয় না।
        public Guid Id { get; set; } = Guid.NewGuid();

        //এটি একটি Foreign Key।
        //উদ্দেশ্য: এই অ্যাকাউন্টটি কোন গ্রাহকের(Customer) তা এই আইডির মাধ্যমে নিশ্চিত করা হয়। এটি ডেটাবেসে রিলেশনশিপ তৈরি করে।
        public Guid CustomerId { get; set; }

        //একে বলা হয় Navigation Property।
        //উদ্দেশ্য: কোড করার সময় আপনি যেন account.Customer.Name লিখে সরাসরি গ্রাহকের নাম পেতে পারেন, সেজন্য এটি রাখা হয়। = default!;
        //মানে হলো শুরুতে এটি নাল(null) থাকলেও পরে যখন ডেটাবেস থেকে ডেটা আসবে তখন এটি পূর্ণ হবে।
        public Customer Customer { get; set; } = default!;

        //কেন decimal: ব্যাংকিং এবং ফিন্যান্সিয়াল প্রজেক্টে সব সময় decimal ব্যবহার করতে হয়। float বা double ব্যবহার করলে রাউন্ডিং এরর(যেমন ০.০১ পয়সা হারিয়ে যাওয়া) হওয়ার ঝুঁকি থাকে।
        //0m এর 'm' নির্দেশ করে এটি একটি ডেসিমেল ভ্যালু।
        public decimal Balance { get; set; } = 0m;

        //এটি ডাটাবেস লেভেলে Concurrency Conflict ঠেকায়। এটি ব্যাংকিং অ্যাপের জন্য সবচেয়ে গুরুত্বপূর্ণ ফিল্ড।
        //RowVersion নিশ্চিত করে যে, একই সময়ে দুজন ব্যালেন্স আপডেট করতে পারবে না। যদি ডাটাবেসের ডাটা আপনার অগোচরে কেউ পরিবর্তন করে দেয়, তবে এটি এরর থ্রো করবে।
        public byte[] RowVersion { get; set; } = Array.Empty<byte>(); 

        //এটি একটি One-to-Many Relationship।
        //উদ্দেশ্য: একটি অ্যাকাউন্টের অধীনে অনেকগুলো ট্রানজ্যাকশন(জমা, খরচ) থাকতে পারে।
        //এটি সেই ট্রানজ্যাকশনগুলোর একটি লিস্ট ধরে রাখে। new ();
        //দিয়ে একে শুরুতেই খালি লিস্ট হিসেবে ইনিশিয়ালাইজ করা হয়েছে যাতে কোডে null reference এরর না আসে।
        public List<Transaction> Transactions { get; set; } = new();

    }
}
