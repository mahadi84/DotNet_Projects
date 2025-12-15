using Login.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Login.Controllers;

[Authorize(Roles = "User")]
public class UserController : Controller
{
    
    public IActionResult Index()
    {

        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Login"); // অলরেডি লগইন থাকলে হোম পেজে রিডাইরেক্ট 
        }
        return View();
    }

}
