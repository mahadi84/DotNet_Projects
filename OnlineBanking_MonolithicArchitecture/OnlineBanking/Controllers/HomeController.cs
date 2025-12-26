using Microsoft.AspNetCore.Mvc;

namespace OnlineBanking.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }
}
