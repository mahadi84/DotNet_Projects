namespace WesterUnionPD.Services;

public interface IUploadJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, string path, CancellationToken ct);  // টোকেন নেওয়া এবং লাইনে দাঁড়ানো হলো `Enqueue`।
    ValueTask<(Guid jobId, string path)> DequeueAsync(CancellationToken ct); // ব্যাংক অফিসার যখন "পরবর্তী গ্রাহক আসুন" বলে সিরিয়াল ডাকেন, তখন লাইন থেকে একজনকে সরিয়ে ডেস্কে নেওয়া হলো `Dequeue`।
}




//আপনি যখন আপনার প্রজেক্টে ১ লাখ রো-এর ফাইল আপলোড করবেন, তখন ইউজারকে ব্রাউজারে বসিয়ে রাখা ঠিক নয়। 
//ইউজার ফাইল আপলোড বাটনে ক্লিক করলে আপনি দ্রুত ডাটাবেসে এন্ট্রি দিয়ে `EnqueueAsync` কল করে দেবেন 
//এবং ইউজারকে বলবেন "আপনার ফাইলটি প্রসেস হচ্ছে"। এরপর ব্যাকগ্রাউন্ডে একটি HostedService এই `DequeueAsync` ব্যবহার করে কাজ সম্পন্ন করবে।

//#### `ValueTask EnqueueAsync(Guid jobId, string path, CancellationToken ct);`
// কাজ: এটি লাইনের শেষে একটি নতুন কাজ যোগ করে।
// প্যারামিটার:
// `Guid jobId`: আপনার ডাটাবেসে সেভ হওয়া জবের ইউনিক আইডি (যাতে পরে স্ট্যাটাস আপডেট করা যায়)।
// `string path`: আপলোড হওয়া ফাইলটি সার্ভারের কোথায় আছে তার লোকেশন।
// `CancellationToken ct`: যদি মাঝপথে অপারেশনটি বন্ধ করার প্রয়োজন হয়।

// ValueTask: এটি `Task`-এর একটি লাইট-ওয়েট সংস্করণ যা মেমরি পারফরম্যান্স বাড়াতে সাহায্য করে।
//
//#### `ValueTask<(Guid jobId, string path)> DequeueAsync(CancellationToken ct);`
// কাজ: ব্যাকগ্রাউন্ড সার্ভিস যখন কাজ করার জন্য প্রস্তুত হবে, তখন সে এই মেথড কল করে লাইনের একদম সামনে থাকা কাজটি তুলে নেবে।
// রিটার্ন টাইপ: এটি একটি Tuple ফেরত দেয় যার মধ্যে `jobId` এবং `path` থাকে, যাতে সার্ভিসটি জানে তাকে কোন ফাইলটি নিয়ে কাজ করতে হবে।


//
