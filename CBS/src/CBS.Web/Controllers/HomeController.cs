using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CBS.Web.Models;

namespace CBS.Web.Controllers;


public class HomeController : Controller
{
    // Default action (Home/Index)
    public IActionResult Index()
    {
        return View();
    }

    // Action to handle general errors (e.g., server errors)
    public IActionResult Error()
    {
        var exceptionDetails = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exceptionMessage = exceptionDetails?.Error.Message;

        ViewBag.ErrorMessage = exceptionMessage ?? "An unexpected error occurred.";
        return View();
    }

    // Action to handle 404 errors (Page Not Found)
    public IActionResult Error404()
    {
        return View(); // This will render the Error404.cshtml view
    }
}