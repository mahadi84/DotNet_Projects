using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestAPI.Data;
using RestAPI.DTOs;
using RestAPI.Helper;
using RestAPI.Interface;
using RestAPI.Mappers;
using RestAPI.Models;
using System.Security.Claims; // ClaimTypes ব্যবহারের জন্য যোগ করা হয়েছে

namespace RestAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // সমস্ত মেথড সুরক্ষিত
    public class EmployeeController : ControllerBase
    {
        private readonly IEmpRepository _repoEmp;

        public EmployeeController(IEmpRepository repo)
        {
            _repoEmp = repo;
        }



        // --- সহায়ক মেথড: লগইন ইউজারের আইডি বের করা ---
        private int? GetLoggedInUserId()
        {
            // ClaimTypes.NameIdentifier (ইউজার আইডি) টোকেন থেকে বের করা
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }





        // --- ১. GET: /api/Emps (সমস্ত ডেটা পুনরুদ্ধার) ---
        [HttpGet]
        [Authorize(Roles = "Admin, User")] // <--- Role Authorization যোগ করা হয়েছে
        public async Task<IActionResult> GetAll()
        {
            var emps = await _repoEmp.GetAllAsync();

            var result = emps
                .Select(e => e.ToEmpReadDto())
                .ToList();

            return Ok(result);
        }





        // --- ২. GET: /api/Emps/{id} (একটি নির্দিষ্ট ডেটা পুনরুদ্ধার) ---
        // যেকোনো লগইন করা ইউজার দেখতে পারবে (কন্ট্রোলার লেভেলের Authorize দ্বারা সুরক্ষিত)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emp = await _repoEmp.GetByIdAsync(id);

            if (emp == null)
            {
                return NotFound(new
                {
                    message = "User not found"
                });
            }

            return Ok(emp.ToEmpReadDto());
        }







        // --- ৩. POST: api/Emps (নতুন ডেটা তৈরি) ---
        // শুধুমাত্র Admin রোল-এর ইউজাররা তৈরি করতে পারবে
        [HttpPost]
        [Authorize(Roles = "Admin")] // <--- Role Authorization যোগ করা হয়েছে
        public async Task<IActionResult> Create([FromBody] EmpCreateDto dto)
        {
            // ✅ email duplicate check via repository
            bool emailExists = await _repoEmp.EmailExistsAsync(dto.empEmail);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "Email already exists"
                });
            }

            // ✅ হার্ডকোডেড মান প্রতিস্থাপন করে JWT টোকেন থেকে ইউজার আইডি বের করা
            var loggedInUserId = GetLoggedInUserId();
            if (loggedInUserId == null)
            {
                // টোকেন বৈধ হলেও Claim না থাকলে এই এরর আসতে পারে
                return Unauthorized(new { message = "User ID not found in token." });
            }

            // Emp মডেলে UserId সেট করা
            var emp = dto.ToEmp(loggedInUserId.Value);

            // ✅ use repository instead of DbContext
            await _repoEmp.AddAsync(emp);
            await _repoEmp.SaveAsync();

            return StatusCode(201, new
            {
                message = "Employee created successfully",
                data = emp.ToEmpReadDto()
            });
        }






        // --- ৪. PUT: /api/Emps/{id} (বিদ্যমান ডেটা আপডেট) ---
        // যেকোনো লগইন করা ইউজার আপডেট করতে পারবে
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmpUpdateDto dto)
        {
            // ... (আপডেটের লজিক অপরিবর্তিত)
            var emp = await _repoEmp.GetByIdAsync(id);

            if (emp == null)
            {
                return NotFound(new { message = "No data found" });
            }

            // যদি আপনি চান যে শুধুমাত্র যে ইউজার তৈরি করেছে, সেই ইউজারই আপডেট করতে পারবে:
            // if (emp.UserId != GetLoggedInUserId())
            // {
            //     return Forbid("You can only update your own created records.");
            // }

            bool isChanged = false;

            if (!string.IsNullOrWhiteSpace(dto.empEmail) && emp.Email != dto.empEmail)
            {
                bool emailExists = await _repoEmp.EmailExistsAsync(dto.empEmail, id);

                if (emailExists)
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                emp.Email = dto.empEmail;
                isChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.empName) && emp.Name != dto.empName)
            {
                emp.Name = dto.empName;
                isChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.empDesignation) && emp.Designation != dto.empDesignation)
            {
                emp.Designation = dto.empDesignation;
                isChanged = true;
            }

            if (!isChanged)
            {
                return Ok(new { message = "No changed made." });
            }

            await _repoEmp.SaveAsync();

            return Ok(new
            {
                message = "Update Successful",
                data = emp.ToEmpReadDto()
            });
        }





        // --- ৫. DELETE: /api/Emps/{id} (নির্দিষ্ট ডেটা মুছে ফেলা) ---
        // শুধুমাত্র Admin রোল-এর ইউজাররা ডিলিট করতে পারবে
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // <--- Role Authorization যোগ করা হয়েছে
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _repoEmp.GetByIdAsync(id);

            if (emp == null)
            {
                return NotFound(new
                {
                    message = "No data found"
                });
            }

            _repoEmp.Remove(emp);
            await _repoEmp.SaveAsync();

            return Ok(new
            {
                message = "Employee deleted successfully"
            });
        }





        // --- ৬. GET: /api/Emps/search (ফিল্টারিং এবং সোর্টিং সহ ইউজার খোঁজা) ---
        [HttpGet("search")]
        public async Task<IActionResult> SearchEmployees([FromQuery] QueryObject query)
        {
            var employees = await _repoEmp.GetAllAsync(query); // রেপোজিটরি থেকে সার্চ করা ডেটা আনুন

            return Ok(employees.Select(e => e.ToEmpReadDto())); // DTO রিটার্ন করা
        }

















    }
}