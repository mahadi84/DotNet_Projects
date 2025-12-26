using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Contracts;
using OnlineBanking.Models;

namespace OnlineBanking.Controllers;

public sealed class AccountController : Controller
{
    private readonly IAuthService _auth;
    public AccountController(IAuthService auth) => _auth = auth;

    private (string ip, string ua) ClientInfo()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = Request.Headers.UserAgent.ToString();
        return (ip, ua);
    }

    // Shows registration form
    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    // Registers a new user and redirects to success page with generated account number
    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ip, ua) = ClientInfo();
        var res = await _auth.RegisterAsync(vm.Name, vm.Email, vm.Password, vm.City, ip, ua);

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Message);
            return View(vm);
        }

        TempData["AccountNumber"] = res.Data;
        return RedirectToAction(nameof(RegisterSuccess));
    }

    // Displays the generated account number after successful registration
    [HttpGet]
    public IActionResult RegisterSuccess()
    {
        ViewBag.AccountNumber = TempData["AccountNumber"]?.ToString();
        return View();
    }

    // Shows login form
    [HttpGet]
    public IActionResult Login() => View(new LoginVm());

    // Validates credentials and signs-in via cookie with claims
    [HttpPost]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (ip, ua) = ClientInfo();
        var res = await _auth.ValidateLoginAsync(vm.AccountNumber, vm.Password, ip, ua);

        if (!res.Success)
        {
            ModelState.AddModelError("", res.Message);
            return View(vm);
        }

        var (customerId, isAdmin) = res.Data;

        var claims = new List<Claim>
        {
            new Claim("CustomerId", customerId.ToString()),
            new Claim("AccountNumber", vm.AccountNumber),
            new Claim(ClaimTypes.Name, vm.AccountNumber),
            new Claim("IsAdmin", isAdmin ? "true" : "false")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return RedirectToAction("Dashboard", "Banking");
    }

    // Logs out current user
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
