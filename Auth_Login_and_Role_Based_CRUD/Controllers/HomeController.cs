using Login.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Login.Controllers
{

    public class HomeController : Controller
    {

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        // এই পেজটি সুরক্ষিত করার দরকার নেই, কারণ এটি Access Denied মেসেজ দেখানোর জন্য ব্যবহৃত হয়।
        public IActionResult AccessDenied()
        {
            // HTTP 403 Forbidden স্ট্যাটাস কোড সেট করুন
            Response.StatusCode = 403;
            ViewData["Title"] = "প্রবেশাধিকার নেই";
            return View();
        }

        // এই রুটটি UseStatusCodePagesWithReExecute থেকে রিকোয়েস্ট গ্রহণ করবে
        [Route("/Home/Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statusCode)
        {
            // নিশ্চিত করুন যে HTTP স্ট্যাটাস কোডটি 404 সেট করা হয়েছে
            // নাহলে ক্লায়েন্ট-সাইডে 200 OK কোড যেতে পারে।
            HttpContext.Response.StatusCode = statusCode;

            if (statusCode == 404)
            {
                ViewData["Title"] = "404 Not Found";
                ViewData["Message"] = "দুঃখিত, আপনি যে ঠিকানাটি খুঁজেছেন তা পাওয়া যায়নি।";
                return View("CustomError");
            }
            else
            {
                // অন্যান্য এরর কোড (যেমন 500) হ্যান্ডেল করার জন্য
                ViewData["Title"] = $"Error {statusCode}";
                ViewData["Message"] = "দুঃখিত, কোনো একটি ত্রুটি ঘটেছে।";
                return View("CustomError");
            }
        }






    }

}
