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
            return View(new UserCreateDTO("", "", UserRole.Maker, 0));
        }


        // ড্রপডাউনের জন্য নাম এবং কোড কনক্যাটিনেট করা
        var branchList = result.Data.Select(b => new {
            BranchId = b.BranchId,
            DisplayText = $"{b.BranchName} ({b.BranchCode})"
        }).ToList();

        ViewBag.Branches = new SelectList(branchList, "BranchId", "DisplayText");

        return View(new UserCreateDTO("", "", UserRole.Maker, 0));
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




    // -------------------------
    // MANAGE (Search) + UPDATE
    // -------------------------
    [HttpGet]
    public async Task<IActionResult> ShowUserInfo(string? username)
    {
        // ১. ইউজারনেম চেক
        if (string.IsNullOrWhiteSpace(username))
        {
            ViewData["Error"] = "Please provide a username to search.";
            return View();
        }

        int currentUserId = 2; // সাধারণত এটি User.Identity থেকে আসে

        // ২. সার্ভিস থেকে ডেটা আনা
        var result = await _userService.GetByUsernameAsync(username.Trim(), currentUserId);

        if (!result.IsSuccess)
        {
            ViewData["Error"] = result.Message;
            return View(); // মডেল নাল থাকবে, তাই ভিউতে 'Not Found' মেসেজ দেখাবে
        }

        // ৩. মডেল রিটার্ন করা (সরাসরি result.Data ও পাঠানো যায়)
        return View(result.Data);
    }






    // -------------------------
    // MANAGE (Search and Show)
    // -------------------------


    [HttpGet]
    public async Task<IActionResult> Manage(string? username)
    {

        // Always load the branch list first to prevent NullReferenceException in the View
        await PopulateBranchList();

        if (string.IsNullOrWhiteSpace(username))
        {
            ViewData["Error"] = "Username cannot be empty.";
            return View();
        }



        int currentUserId = 2;
        var result = await _userService.GetByUsernameAsync(username, currentUserId);
        if (!result.IsSuccess)
        {
            ViewData["Error"] = result.Message;
            return View();
        }

        //manual mapping (map SearchDTO into UpdateDTO to show in editable field)
        var model = new UserUpdateDTO
        {
            Id = result.Data.Id,
            Username = result.Data.Username,
            Role = result.Data.Role,
            BranchId = result.Data.BranchId,
            BranchCode = result.Data.BranchCode,
            IsActive = result.Data.IsActive,
            IsLocked = result.Data.IsLocked,
            FailedAttempts = result.Data.FailedAttempts,
            CreatedBy = result.Data.CreatedBy,
            ApprovedBy = result.Data.ApprovedBy ?? 0,
            UpdatedBy = result.Data.UpdatedBy,
            CreatedAt = result.Data.CreatedAt,
            UpdatedAt = result.Data.UpdatedAt,
            RowVersion = result.Data.RowVersion
            
        };



       


        return View(model);
    }






    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UserUpdateDTO model)
    {
        if (!ModelState.IsValid)
        {
            // If validation fails, we must re-populate the branch list before returning the view
            await PopulateBranchList();
            return View("Manage", model);
        }

        int currentUserId = 2; // Usually from User.Identity
        var result = await _userService.UpdateUserAsync(model, currentUserId);

        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message;
            // Redirect to Manage with the username to show the updated data
            return RedirectToAction("Manage", new { username = model.Username });
        }
        else
        {
            TempData["Error"] = result.Message;
            await PopulateBranchList();
            return View("Manage", model);
        }
    }

    // Helper method to avoid code duplication
    private async Task PopulateBranchList()
    {
        var branchResult = await _branchService.GetAllBranchNameAndCodeAsync();
        if (branchResult != null && branchResult.IsSuccess)
        {
            var branchList = branchResult.Data.Select(b => new {
                BranchId = b.BranchId,
                DisplayText = $"{b.BranchName} ({b.BranchCode})"
            }).ToList();
            ViewBag.Branches = new SelectList(branchList, "BranchId", "DisplayText");
        }
    }









}

