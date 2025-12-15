using Login.Data;
using Login.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace Login.Controllers;


[Authorize]
public class EmployeeController : Controller
{

    private readonly ApplicationDbContext _context;

    // প্রতি পাতায় ডিফল্ট ৫টি করে এন্ট্রি (PageSize)
    public const int PageSize = 5;

    public EmployeeController(ApplicationDbContext context)
    { // Dependency Injection
        _context = context;
    }

    // --- GET: Employee/Index (Search, Sort, Filter, Paginate) ---
    public async Task<IActionResult> Index(
        string sortOrder,
        string currentFilter,
        string searchString,
        int? pageNumber)
    {
        // ১. লগইন করা ইউজারের UserId Claims থেকে নেওয়া
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int loggedInUserId))
        {
            // নিরাপত্তা যাচাই: যদি ইউজার আইডি না পাওয়া যায়, তবে খালি লিস্ট দেখাবে।
            return View(PaginatedList<Login.Models.Emp>.Create(new List<Login.Models.Emp>().AsQueryable(), 1, PageSize));
        }

        // ২. সর্টিং প্যারামিটার সেট করা
        ViewData["CurrentSort"] = sortOrder;
        ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["EmailSortParm"] = sortOrder == "Email" ? "email_desc" : "Email";

        // ৩. পেজিং-এর জন্য ফিল্টার সংরক্ষণ করা
        if (searchString != null)
        {
            pageNumber = 1; // নতুন সার্চ শুরু হলে প্রথম পাতায় যাওয়া
        }
        else
        {
            searchString = currentFilter; // অন্যথায়, বর্তমান ফিল্টার বজায় রাখা
        }
        ViewData["CurrentFilter"] = searchString;

        // ৪. ইউজারের Role চেক করা
        bool isAdmin = User.IsInRole("Admin");
        ViewData["Role"] = isAdmin ? "Admin" : "User";

        IQueryable<Login.Models.Emp> employeesQuery = _context.emp.AsQueryable();

        // ৫. রোল-ভিত্তিক ডেটা ফিল্টারিং (সাধারণ User-এর জন্য)
        if (!isAdmin)
        {
            // সাধারণ User: শুধুমাত্র তার UserId-এর সাথে মেলা এন্ট্রিগুলি দেখা যাবে।
            employeesQuery = employeesQuery.Where(e => e.UserId == loggedInUserId);
        }

        // ৬. সার্চিং/ফিল্টারিং (নাম বা ইমেল দ্বারা)
        if (!string.IsNullOrEmpty(searchString))
        {
            employeesQuery = employeesQuery.Where(e => e.Name.Contains(searchString)
                                                   || e.Email.Contains(searchString));
        }

        // ৭. সর্টিং লজিক প্রয়োগ
        employeesQuery = sortOrder switch
        {
            "name_desc" => employeesQuery.OrderByDescending(e => e.Name),
            "Email" => employeesQuery.OrderBy(e => e.Email),
            "email_desc" => employeesQuery.OrderByDescending(e => e.Email),
            _ => employeesQuery.OrderBy(e => e.Name), // ডিফল্ট সর্টিং: নাম অনুসারে
        };

        // ৮. পেজিনেশন
        var employees = await PaginatedList<Login.Models.Emp>.CreateAsync(
            employeesQuery.AsNoTracking(),
            pageNumber ?? 1,
            PageSize);

        return View(employees);
    }


    //----------------------------------- ADD User  -----------------------------------

    [HttpGet]
    public IActionResult CreateForm()
    {
        return View(new Emp());
    }

    // --- POST: Employee/Create ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEmployee([Bind("Name,Designation,Email")] Emp employee)
    {
        // ১. মডেল ভ্যালিডেশন পরীক্ষা করা (যদি ব্যর্থ হয়, সরাসরি ভিউতে ফেরত)
        if (!ModelState.IsValid)
        {
            return View("CreateForm", employee);
        }

        // ২. ডুপ্লিকেট ইমেল পরীক্ষা করা
        if (await _context.emp.AnyAsync(e => e.Email == employee.Email))
        {
            ModelState.AddModelError("Email", "This email address is already registered.");
            return View("CreateForm", employee);
        }

        // ৩. ইউজারের নিরাপত্তা যাচাই এবং UserId সেট করা (Authentication Check)
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int loggedInUserId))
        {
            // যদি ইউজার আইডি না পাওয়া যায় বা অবৈধ হয় (নিরাপত্তা ত্রুটি)
            ModelState.AddModelError(string.Empty, "User ID not found or invalid format. Please log in again.");
            return View("CreateForm", employee);
        }

        // ৪. ডেটাবেজে ডেটা সংরক্ষণ করা
        employee.UserId = loggedInUserId;
        _context.Add(employee);
        await _context.SaveChangesAsync();

        // ৫. সফল বার্তা সেট করা এবং PRG প্যাটার্ন অনুসরণ করে রিডাইরেক্ট করা
        TempData["SuccessMessage"] = "Employee created successfully.";
        return RedirectToAction(nameof(CreateForm));
    }


    // --- GET: Employee/Edit/5 ---
    public async Task<IActionResult> Edit(int? id)
    {
        // ১. ID ভ্যালিডেশন
        if (id == null) return NotFound();

        // ২. লগইন করা ইউজারের UserId যাচাই
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int loggedInUserId))
        {
            TempData["ErrorMessage"] = "Please log in to perform this action.";
            return RedirectToAction(nameof(Index));
        }

        // ৩. কর্মচারী খোঁজা
        var employee = await _context.emp.FirstOrDefaultAsync(e => e.Id == id);

        // ৪. অস্তিত্ব এবং মালিকানা যাচাই (সর্বোত্তম নিরাপত্তা)
        // যদি কর্মচারী না পাওয়া যায় অথবা মালিকানা না মেলে
        if (employee == null || employee.UserId != loggedInUserId)
        {
            TempData["ErrorMessage"] = "You do not have permission to edit this employee.";
            return RedirectToAction(nameof(Index));
        }

        // ৫. এডিট ভিউ পাঠানো
        return View(employee);
    }



    // --- POST: Employee/Edit/5 ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Designation,Email")] Emp employee)
    {
        // ১. ID এবং মডেল ভ্যালিডেশন
        if (id != employee.Id || !ModelState.IsValid) return View(employee);

        // ২. লগইন করা ইউজারের UserId যাচাই
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int loggedInUserId))
        {
            TempData["ErrorMessage"] = "Authentication failed. Please log in again.";
            return RedirectToAction(nameof(Index));
        }

        // ৩. কর্মচারী লোড এবং সুরক্ষা যাচাই (Horizontal Privilege Check)
        var originalEmployee = await _context.emp.FirstOrDefaultAsync(e => e.Id == id);

        // যদি কর্মচারী না পাওয়া যায় OR অন্য ইউজারের ডেটা হয়
        if (originalEmployee == null || originalEmployee.UserId != loggedInUserId)
        {
            return NotFound();
        }

        // ৪. ডুপ্লিকেট ইমেল পরীক্ষা করা (নিজেকে ছাড়া)
        if (await _context.emp.AnyAsync(e => e.Email == employee.Email && e.Id != employee.Id))
        {
            ModelState.AddModelError("Email", "This email address is already registered by another employee.");
            return View(employee);
        }

        // ৫. ডেটা পরিবর্তন পরীক্ষা এবং আপডেট
        if (originalEmployee.Name != employee.Name ||
            originalEmployee.Designation != employee.Designation ||
            originalEmployee.Email != employee.Email)
        {
            // ডেটা আপডেট
            originalEmployee.Name = employee.Name;
            originalEmployee.Designation = employee.Designation;
            originalEmployee.Email = employee.Email;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee updated successfully";
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException) when (!EmployeeExists(employee.Id))
            {
                return NotFound();
            }
        }
        else
        {
            TempData["ErrorMessage"] = "You updated nothing";
        }

        // ৬. Index পেজে রিডাইরেক্ট করা ভালো (Edit এ নয়)
        return RedirectToAction(nameof(Index));
    }

    // Private Helper Method
    private bool EmployeeExists(int id)
    {
        return _context.emp.Any(e => e.Id == id);
    }


    // --- POST: Employee/Delete/5 ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // ১. লগইন করা ইউজারের ID যাচাই
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int loggedInUserId))
        {
            return Unauthorized(); // লগইন বা ID inválid হলে
        }

        // ২. কর্মচারী লোড এবং মালিকানা যাচাই (একই সাথে)
        var employee = await _context.emp
                                     .FirstOrDefaultAsync(e => e.Id == id && e.UserId == loggedInUserId);

        // ৩. কর্মচারী খুঁজে না পেলে (বা অন্য ইউজারের ডেটা হলে)
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Employee not found or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        // ৪. ডেটাবেস থেকে মুছে ফেলা
        try
        {
            _context.emp.Remove(employee);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Employee '{employee.Name}' deleted successfully.";
        }
        catch (Exception)
        {
            // ত্রুটি ঘটলে
            TempData["ErrorMessage"] = "An error occurred while deleting the employee.";
        }

        // ৫. তালিকা ভিউতে রিডাইরেক্ট করা
        return RedirectToAction(nameof(Index));
    }
}