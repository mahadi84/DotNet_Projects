using Login.Data;
using Login.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace Login.Controllers
{
    public class LoginController : Controller // এই কন্ট্রোলারটি লগইন এবং লগআউট হ্যান্ডেল করবে
    {
        private readonly ApplicationDbContext _sqlConn;      // EF Core এর মাধ্যমে ডাটাবেসের সাথে যুক্ত হওয়ার জন্য ApplicationDbContext        
        public LoginController(ApplicationDbContext sqlConn) // Constructor / Dependency Injection এর মাধ্যমে DbContext ইনজেক্ট করা হলো
        {
            _sqlConn = sqlConn;
        }

        // ################ ১. লগইন পেইজ (GET) ################

        // GET: /Login/Login
        public IActionResult Login()        {
            // ইউজার লগইন করা আছে কিনা তা পরীক্ষা করা
            if (User.Identity.IsAuthenticated)
            {          
                return RedirectToAction("Index", "Home"); // এখানে "Home" কন্ট্রোলার, Index অ্যাকশনে রিডাইরেক্ট করা   
            }            
            return View(); //লগইন করা না থাকলে, লগইন ভিউ দেখানো // Searches for Views/Login/Login.cshtml
        }


        // ################ ২. লগইন প্রসেস (POST) ################

        // POST: /Login/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // ১. ইউজার যাচাই করা হলো
            var user = await ValidateUser(username, password);

            if (user == null)
            {
                // লগইন ব্যর্থ হলে
                ViewBag.ErrorMessage = "Username or Password is incorrect";
                return View();
            }

            // ২. Claims তৈরি করা
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // ৩. সাইন ইন করানো (Authentication Properties সহ)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true });

            // ৪. ভূমিকার ভিত্তিতে রিডাইরেক্ট লজিক সংক্ষিপ্ত করা
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            // Admin না হলে ডিফল্ট Home বা User পেজে রিডাইরেক্ট
            return RedirectToAction("Index", "User");
           
        }


        // ################ ৩. লগআউট ফাংশন (POST) ################

        // POST: /Login/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // কুকি ডিলিট করে সাইন আউট করা হলো            
            return RedirectToAction("Login", "Login"); // লগইন পেইজে ফেরত পাঠানো
        }


        // ################ ৪. ইউজার যাচাই করার Helper ফাংশন ################

        private async Task<User> ValidateUser(string username, string password) // EF Core ব্যবহার করে ডাটাবেস থেকে ইউজার (Role সহ) খোঁজা এবং পাসওয়ার্ড যাচাই
        {
            // DbContext ব্যবহার করে ইউজার খুঁজে বের করা হলো
            var user = await _sqlConn.user  // User মডেলের Role প্রপার্টি সহ সমস্ত ডাটা Fetch করা হচ্ছে              
                .FirstOrDefaultAsync(u => u.Username == username);

            // ইউজার পাওয়া গেলে এবং পাসওয়ার্ড যাচাই সফল হলে
            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }



        // ################ ৫. পাসওয়ার্ড যাচাই করার Helper ফাংশন ################

        // ASP.NET Identity এর PasswordHasher ব্যবহার করে পাসওয়ার্ড যাচাই করা
        public bool VerifyPassword(string providedPassword, string storedHash)
        {
            var user = new User();
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, storedHash, providedPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
