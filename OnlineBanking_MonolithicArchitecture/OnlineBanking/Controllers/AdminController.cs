using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Contracts;
using OnlineBanking.Models;

namespace OnlineBanking.Controllers;

[Authorize(Policy = "AdminOnly")]
public sealed class AdminController : Controller
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    private string ActorAcc => User.Claims.First(x => x.Type == "AccountNumber").Value;

    private (string ip, string ua) ClientInfo()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = Request.Headers.UserAgent.ToString();
        return (ip, ua);
    }

    // Shows all customers for admin management
    [HttpGet]
    public async Task<IActionResult> Customers()
    {
        var list = await _admin.GetCustomersAsync();
        return View(list);
    }

    // Toggles admin role for a user
    [HttpPost]
    public async Task<IActionResult> SetAdmin(string accountNumber, bool isAdmin)
    {
        var (ip, ua) = ClientInfo();
        var res = await _admin.SetAdminAsync(accountNumber, isAdmin, ActorAcc, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Customers));
    }

    // Locks a user for N minutes
    [HttpPost]
    public async Task<IActionResult> Lock(string accountNumber, int minutes = 30)
    {
        var (ip, ua) = ClientInfo();
        var res = await _admin.LockAsync(accountNumber, minutes, ActorAcc, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Customers));
    }

    // Unlocks a user
    [HttpPost]
    public async Task<IActionResult> Unlock(string accountNumber)
    {
        var (ip, ua) = ClientInfo();
        var res = await _admin.UnlockAsync(accountNumber, ActorAcc, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Customers));
    }

    // Shows password reset form
    [HttpGet]
    public IActionResult ResetPassword() => View(new AdminResetPasswordVm());

    // Resets user password (BCrypt hashing happens inside AdminService)
    [HttpPost]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordVm vm)
    {
        var (ip, ua) = ClientInfo();

        if (!ModelState.IsValid) return View(vm);

        var res = await _admin.ResetPasswordAsync(vm.AccountNumber, vm.NewPassword, ActorAcc, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Customers));
    }

    // Shows latest audit logs
    [HttpGet]
    public async Task<IActionResult> Audit()
    {
        var logs = await _admin.GetLatestAuditAsync(150);
        return View(logs);
    }
}
