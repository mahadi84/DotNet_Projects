using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Application.Contracts;
using OnlineBanking.Web.Models;

namespace OnlineBanking.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")] // Admin policy enforce: IsAdmin=true claim required
    public sealed class AdminController : Controller
    {
        private readonly IAdminService _admin; // admin operations service

        // DI injection
        public AdminController(IAdminService admin) => _admin = admin;

        // logged-in admin এর account number claim থেকে বের করা
        // audit log এ "actor" হিসেবে যাবে
        private string ActorAcc => User.Claims.First(x => x.Type == "AccountNumber").Value;

        // client info collect (audit)
        private (string ip, string ua) ClientInfo()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ua = Request.Headers.UserAgent.ToString();
            return (ip, ua);
        }

        // Customers list page
        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            // সব customer list fetch
            var list = await _admin.GetCustomersAsync();

            // view এ list পাঠানো
            return View(list);
        }

        // Admin role set/unset (POST)
        [HttpPost]
        public async Task<IActionResult> SetAdmin(string accountNumber, bool isAdmin)
        {
            var (ip, ua) = ClientInfo();

            // service call: role change + audit
            var res = await _admin.SetAdminAsync(accountNumber, isAdmin, ActorAcc, ip, ua);

            // feedback
            TempData["Msg"] = res.Message;

            // list page refresh
            return RedirectToAction(nameof(Customers));
        }

        // Lock user (POST)
        [HttpPost]
        public async Task<IActionResult> Lock(string accountNumber, int minutes = 30)
        {
            var (ip, ua) = ClientInfo();

            // service call: lock for N minutes + audit
            var res = await _admin.LockAsync(accountNumber, minutes, ActorAcc, ip, ua);

            TempData["Msg"] = res.Message;

            return RedirectToAction(nameof(Customers));
        }

        // Unlock user (POST)
        [HttpPost]
        public async Task<IActionResult> Unlock(string accountNumber)
        {
            var (ip, ua) = ClientInfo();

            // service call: unlock + audit
            var res = await _admin.UnlockAsync(accountNumber, ActorAcc, ip, ua);

            TempData["Msg"] = res.Message;

            return RedirectToAction(nameof(Customers));
        }

        // Reset password page show
        [HttpGet]
        public IActionResult ResetPassword() => View(new AdminResetPasswordVm());

        // Reset password submit (POST)
        [HttpPost]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordVm vm)
        {
            var (ip, ua) = ClientInfo();

            // validation
            if (!ModelState.IsValid) return View(vm);

            // service call: reset + audit
            var res = await _admin.ResetPasswordAsync(vm.AccountNumber, vm.NewPassword, ActorAcc, ip, ua);

            TempData["Msg"] = res.Message;

            return RedirectToAction(nameof(Customers));
        }

        // Latest audit logs page
        [HttpGet]
        public async Task<IActionResult> Audit()
        {
            // last 150 audit logs load
            var logs = await _admin.GetLatestAuditAsync(150);

            // view এ পাঠানো
            return View(logs);
        }
    }
}
