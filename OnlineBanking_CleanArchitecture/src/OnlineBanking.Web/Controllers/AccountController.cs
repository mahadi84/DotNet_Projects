using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Application.Contracts;
using OnlineBanking.Web.Models;
using System.Security.Claims;

namespace OnlineBanking.Web.Controllers
{
    public sealed class AccountController : Controller
    {
        private readonly IAuthService _auth;              // Auth operations (register/login validation) করার abstraction

        // DI দিয়ে IAuthService inject করা হচ্ছে
        public AccountController(IAuthService auth) => _auth = auth;

        
        
        
        
        // Client IP + UserAgent collect করে
        // audit log এ trace রাখতে কাজে লাগে
        private (string ip, string ua) ClientInfo()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"; // client IP না পেলে "unknown"
            var ua = Request.Headers.UserAgent.ToString();                            // browser/device info
            return (ip, ua);                                                          // tuple return
        }






        // Register page দেখায়
        [HttpGet]
        public IActionResult Register() => View(new RegisterVm()); // empty model দিয়ে view render

        // Register form submit handle করে
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            // ViewModel validation fail হলে একই view এ error সহ ফেরত
            if (!ModelState.IsValid) return View(vm);

            // IP/UA নেওয়া হচ্ছে (audit এর জন্য)
            var (ip, ua) = ClientInfo();

            // Application layer auth service call → customer register + account number generate
            var res = await _auth.RegisterAsync(vm.Name, vm.Email, vm.Password, vm.City, ip, ua);

            // যদি register fail হয় → message ModelState এ বসিয়ে view দেখায়
            if (!res.Success)
            {
                ModelState.AddModelError("", res.Message); // global error message
                return View(vm);
            }

            // success: generated account number TempData তে রাখা
            // TempData = next request পর্যন্ত থাকে (Redirect এর পরে read করা যায়)
            TempData["AccountNumber"] = res.Data;

            // success page এ redirect
            return RedirectToAction(nameof(RegisterSuccess));
        }

        // Register success page
        [HttpGet]
        public IActionResult RegisterSuccess()
        {
            // TempData থেকে account number বের করে ViewBag এ রাখা (View এ show করার জন্য)
            ViewBag.AccountNumber = TempData["AccountNumber"]?.ToString();
            return View();
        }









        // Login page show
        [HttpGet]
        public IActionResult Login() => View(new LoginVm()); // empty login model

        
        
        
        // Login submit handle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            // validation fail হলে same page show
            if (!ModelState.IsValid) return View(vm);

            // IP/UA collect
            var (ip, ua) = ClientInfo();

            // auth validation: password verify + lockout rule + audit
            var res = await _auth.ValidateLoginAsync(vm.AccountNumber, vm.Password, ip, ua);

            // login fail হলে error message দেখায়
            if (!res.Success)
            {
                ModelState.AddModelError("", res.Message);
                return View(vm);
            }


            // success হলে returned tuple থেকে customerId + isAdmin নেওয়া
            var (customerId, isAdmin) = res.Data!;

            // Cookie claims তৈরি
            // এগুলো cookie তে store হবে এবং পরের request এ User.Claims এ পাওয়া যাবে
            var claims = new List<Claim>
        {
            new Claim("CustomerId", customerId.ToString()),                // app internal user identification
            new Claim("AccountNumber", vm.AccountNumber),                  // logged-in account number
            new Claim(ClaimTypes.Name, vm.AccountNumber),                  // framework-friendly name (optional)
            new Claim("IsAdmin", isAdmin ? "true" : "false")               // AdminOnly policy check করবে এই claim দিয়ে
        };

            // ClaimsIdentity তৈরি (cookie scheme দিয়ে)
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // ClaimsPrincipal তৈরি (User object এর core)
            var principal = new ClaimsPrincipal(identity);

            // Cookie issue করে user কে signed-in করা
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // login success হলে dashboard এ redirect
            return RedirectToAction("Dashboard", "Banking");
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        // Logout handle (POST রাখা ভালো practice; CSRF protection লাগে)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // cookie invalidate/sign-out
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // login page এ redirect
            return RedirectToAction(nameof(Login));
        }
    }
}
