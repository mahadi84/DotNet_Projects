using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace CBS.Web.Controllers;
    



    public class UserController : Controller
    {
       

    private readonly IUserService _userService;
    private readonly IBranchService _branchService;
    public UserController(IUserService userService, IBranchService branchService)
    {
        _userService = userService;
        _branchService = branchService;
    }




    //-------------------------
    //CREATE
    // -------------------------
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _branchService.GetAllBranchNameAndCodeAsync();

        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Message;
            return View(new UserCreateDTO("", "", UserRole.Maker, ""));
        }

        // Ensure ViewBag.Branches is a SelectList
        ViewBag.Branches = new SelectList(result.Data, "BranchCode", "BranchName");

        return View(new UserCreateDTO("", "", UserRole.Maker, ""));
    }





    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateDTO dto)
    {
        if (!ModelState.IsValid) return View("Index", dto);

        int currentUserId = 1; // TODO: get from session/auth
        var result = await _userService.CreateUserAsync(dto, currentUserId);

        if (result.IsSuccess)
        {
            TempData["Success"] = $"{result.Message} | ID:{result.Data.Id} | Username:{result.Data.Username}";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Message;
        return View("Index", dto);
    }




    //[HttpGet]
    //public async Task<IActionResult> Index()
    //{
    //    // ড্রপডাউন লিস্ট লোড করার জন্য প্রাইভেট মেথড কল করা
    //    await PopulateBranchListAsync();

    //    // শুরুতে একটি খালি DTO পাঠানো হচ্ছে
    //    return View(new UserCreateDTO("", "", UserRole.Maker, ""));
    //}

    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Create(UserCreateDTO dto)
    //{
    //    if (!ModelState.IsValid)
    //    {
    //        // ভ্যালিডেশন ফেইল করলে ড্রপডাউন ডেটা আবার লোড করতে হবে
    //        await PopulateBranchListAsync();
    //        return View("Index", dto);
    //    }

    //    int currentUserId = 1; // TODO: Get from Auth/Session
    //    var result = await _userService.CreateUserAsync(dto, currentUserId);

    //    if (result.IsSuccess)
    //    {
    //        TempData["Success"] = $"{result.Message} | Username: {result.Data.Username}";
    //        return RedirectToAction(nameof(Index));
    //    }

    //    // সার্ভিস থেকে এরর আসলে সেটি দেখানো এবং ড্রপডাউন আবার লোড করা
    //    TempData["Error"] = result.Message;
    //    await PopulateBranchListAsync();
    //    return View("Index", dto);
    //}

    //// --- হেল্পার মেথড (Private Helper Method) ---
    //private async Task PopulateBranchListAsync()
    //{
    //    var branchesResult = await _branchService.GetAllBranchNameAndCodeAsync();

    //    if (branchesResult.IsSuccess && branchesResult.Data != null)
    //    {
    //        // ব্রাঞ্চের লিস্টকে SelectListItem এ রূপান্তর করে ViewBag এ রাখা
    //        ViewBag.BranchList = branchesResult.Data.Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
    //        {
    //            Value = b.BranchCode,
    //            Text = $"{b.BranchName} ({b.BranchCode})"
    //        }).ToList();
    //    }
    //    else
    //    {
    //        // যদি ডাটা না পাওয়া যায় তবে অন্তত একটি খালি লিস্ট পাঠানো যাতে এরর না হয়
    //        ViewBag.BranchList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
    //    }
    //}




}

