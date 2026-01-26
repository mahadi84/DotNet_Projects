using CBS.Application.DTO;
using CBS.Application.Interfaces;
using CBS.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CBS.Web.Controllers;

public class BranchController : Controller
{
    private readonly IBranchService _branchService;
    public BranchController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    // GET: BranchController
    public ActionResult Index()
        {
            return View();
        }






    // POST: BranchController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchCreateDTO bdto)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", bdto);
        }

        int UserID = 1; // পরবর্তীতে সেশন বা ইউজার থেকে আসবে
        var result = await _branchService.CreateBranchAsync(bdto, UserID);

        if (result.IsSuccess)
        {

            // get data from dynamic and create message
            var data = result.Data;

            var id = data.Id;
            var branchName = data.BranchName;
            var branchCode = data.BranchCode;
            var vaultBalance = data.VaultBalance;
            

            TempData["Success"] = $"{result.Message}, ID:{id}, Branch: {branchName} ({branchCode}), Vault Balance: ({vaultBalance})";

            return RedirectToAction(nameof(Index));
        }

        //pass error message If faild to insert so that user can edit
        ViewBag.ErrorMessage = result.Message;
        TempData["Error"] = result.Message;

        return View("Index",bdto);
    }









    [HttpGet]
    //[ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(string? searchCode)
    {

        //  Use string.IsNullOrWhiteSpace to check for empty search
        if (string.IsNullOrWhiteSpace(searchCode))
        {
            ViewData["Error"] = "Field cannot be empty";
            return View();
        }

        int currentUserId = 2;
        //  Pass the string searchCode to the service
        var result = await _branchService.GetByBranchCodeAsync(searchCode, currentUserId);


        if (!result.IsSuccess)
        {
            ViewData["Error"] = result.Message;
            return View();
        }

        // Map ResponseDTO to UpdateDTO for the form
        var updateModel = new BranchUpdateDTO(
            result.Data.Id,
            result.Data.BranchName,
            result.Data.BranchCode,
            result.Data.VaultBalance,
            result.Data.RowVersion,
            result.Data.IsActive
        );

        return View(updateModel);
    }










    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(BranchUpdateDTO budto)
    {
        if (!ModelState.IsValid) return View("Manage", budto);

        int currentUserId = 3;

        var result = await _branchService.UpdateBranchAsync(budto, currentUserId);
        if (result.IsSuccess)
        {
            ViewData["Success"] = result.Message;
            return RedirectToAction("Manage", new { searchCode = budto.BranchCode });
        }

        ViewBag.Error = result.Message;
        return View("Manage", budto);
    }











}

