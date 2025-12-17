using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestAPI.Data;
using RestAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RestAPI.DTOs;
using Microsoft.AspNetCore.Identity; // <--- Identity প্যাকেজ ইমপোর্ট করা হয়েছে


namespace RestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<User> _passwordHasher; // <--- Identity Hasher ইনজেক্ট করার জন্য নতুন ফিল্ড

        // কনস্ট্রাক্টর: IPasswordHasher<User> ইনজেক্ট করা হয়েছে
        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher; // <--- Identity Hasher অ্যাসাইন করা হয়েছে
        }

        // --- লগইন মেথড ---
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // ইউজারকে খোঁজা DbContext এ `User` নামের DbSet যদি না থেকে থাকে তবে এটি `_context.Users` হতে পারে।
            var user = await _context.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            // ইউজার না পাওয়া গেলে
            if (user == null)
            {
                // সুরক্ষা কারণে সাধারণ ত্রুটির বার্তা ব্যবহার করা হয়
                return Unauthorized(new { message = "Invalid Username or Password" });
            }

            // পাসওয়ার্ড যাচাই করা: Identity এর VerifyHashedPassword মেথড ব্যবহার
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash, // ডেটাবেস থেকে আসা হ্যাশ
                loginDto.Password   // ক্লায়েন্ট থেকে আসা প্লেন টেক্সট পাসওয়ার্ড
            );

            // পাসওয়ার্ড ভুল হলে (বা হ্যাশ ফরম্যাট ভুল হলে)
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Invalid Username or Password" });
            }

            // পাসওয়ার্ড সঠিক হলে JWT টোকেন তৈরি করা
            var token = CreateToken(user);

            return Ok(new { token = token, username = user.Username, role = user.Role });
        }

        // --- JWT টোকেন তৈরির সহায়ক মেথড (অপরিবর্তিত) ---
        private string CreateToken(User user)
        {
            // ১. JWT টোকেনে অন্তর্ভুক্ত করার জন্য Claims তৈরি করা
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };

            // ২. appsettings.json থেকে JWT Key নেওয়া এবং নিরাপত্তা নিশ্চিত করা
            var tokenKey = _configuration.GetSection("AppSettings:Token").Value;

            // নাল বা খালি স্ট্রিং কিনা তা চেক করা (পূর্ববর্তী এরর এড়াতে)
            if (string.IsNullOrEmpty(tokenKey))
            {
                // কী খুঁজে না পেলে একটি স্পষ্ট এরর থ্রো করা
                throw new InvalidOperationException("Configuration Error: JWT Secret Key 'AppSettings:Token' is missing or empty.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));

            // ৩. SigningCredentials তৈরি করা
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // ৪. টোকেন তৈরি
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            // ৫. টোকেনটিকে স্ট্রিং হিসাবে ফিরিয়ে দেওয়া
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }






    }
}