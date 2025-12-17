using System.Threading.Channels;

namespace WesterUnionPD.Services;

public sealed class UploadJobQueue : IUploadJobQueue
{
    private readonly Channel<(Guid, string)> _ch =
        Channel.CreateUnbounded<(Guid, string)>();

    public ValueTask EnqueueAsync(Guid jobId, string path, CancellationToken ct)
        => _ch.Writer.WriteAsync((jobId, path), ct);

    public ValueTask<(Guid jobId, string path)> DequeueAsync(CancellationToken ct)
        => _ch.Reader.ReadAsync(ct);
}


// কেন এটি ব্যবহার করা হয়েছে?

// ১. অ্যাসিঙ্ক্রোনাস কমিউনিকেশন
// ২. থ্রেড সেফটি (Thread Safety): যদি একসাথে ১০ জন ইউজার ফাইল আপলোড করে, তবে Channels নিজে থেকেই নিশ্চিত করে যে কোনো ডেটা ওভারল্যাপ হবে না এবং সিরিয়াল ঠিক থাকবে। 
// ৩. পারফরম্যান্স: এটি প্রথাগত Queue<T> বা লিস্টের চেয়ে অনেক বেশি ফাস্ট কারণ এটি .NET-এর আধুনিক কনকারেন্সি ফিচারের ওপর ভিত্তি করে তৈরি।
// ৪. বাস্তব জীবনের উদাহরণ

// এটি একটি ব্যাংকের টোকেন সিস্টেমের মতো।

// -   EnqueueAsync হলো সেই মেশিন যা আপনাকে টোকেন দেয় এবং আপনার নাম সিরিয়ালে যুক্ত করে।
// -  _ch হলো সেই ডিজিটাল ডিসপ্লে যেখানে সিরিয়াল নম্বর জমা থাকে।
// -   DequeueAsync হলো ব্যাংকের ক্যাশিয়ার যে নম্বর ডেকে আপনাকে কাউন্টারে ডাকে।

// সারসংক্ষেপ: এই কোডটি আপনার প্রজেক্টের "ট্রাফিক পুলিশ" হিসেবে কাজ করছে, যা নিশ্চিত করছে যে ফাইল আপলোড হওয়ার পর সেগুলো যেন জ্যাম না লাগিয়ে একটির পর একটি সুন্দরভাবে প্রসেস হয়।
