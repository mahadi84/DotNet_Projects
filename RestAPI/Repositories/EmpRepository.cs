using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestAPI.Data;
using RestAPI.Helper;
using RestAPI.Interface;
using RestAPI.Models;
using System;

namespace RestAPI.Repositories
{
    public class EmpRepository : IEmpRepository
    {
        private readonly ApplicationDbContext _context;

        public EmpRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Emp>> GetAllAsync()
        {
            return await _context.Emp.ToListAsync();
        }

        public async Task<Emp?> GetByIdAsync(int id)
        {
            return await _context.Emp.FindAsync(id);
        }

        public async Task AddAsync(Emp emp)
        {
            await _context.Emp.AddAsync(emp);
        }

        public async Task<bool> EmailExistsAsync(string email, int? ignoreId = null)
        {
            return await _context.Emp
                .AnyAsync(e => e.Email == email && (ignoreId == null || e.Id != ignoreId));
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Remove(Emp emp)
        {
            _context.Emp.Remove(emp);
        }



        public async Task<List<Emp>> GetAllAsync(QueryObject query)
        {
            var employees = _context.Emp.AsQueryable(); // সমস্ত কর্মচারীর তথ্য পাওয়ার জন্য Query তৈরি

            // নাম এবং ইমেলের প্যারামিটারগুলি আছে কিনা তা পরীক্ষা করুন
            bool isNameProvided = !string.IsNullOrWhiteSpace(query.Name);
            bool isEmailProvided = !string.IsNullOrWhiteSpace(query.Email);

            // --- ফিল্টারিং: Name OR Email, অথবা BOTH ---
            if (isNameProvided && isEmailProvided)
            {
                // Name AND Email উভয়ই দেওয়া হলে: 
                // যে Employee-এর নাম বা ইমেল যেকোনো একটির সাথে মেলে, তাকে নির্বাচন করা হবে (OR logic).
                employees = employees.Where(e => e.Name.Contains(query.Name) || e.Email.Contains(query.Email));
            }
            else if (isNameProvided)
            {
                // শুধুমাত্র Name দেওয়া হয়েছে
                employees = employees.Where(e => e.Name.Contains(query.Name));
            }
            else if (isEmailProvided)
            {
                // শুধুমাত্র Email দেওয়া হয়েছে
                employees = employees.Where(e => e.Email.Contains(query.Email));
            }

            // --- সোর্টিং: SortBy প্যারামিটার অনুযায়ী সorting করা হবে ---
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                if (query.SortBy.Equals("Email", StringComparison.OrdinalIgnoreCase))
                {
                    employees = query.IsDecsending ? employees.OrderByDescending(e => e.Email) : employees.OrderBy(e => e.Email);
                }
                else if (query.SortBy.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    employees = query.IsDecsending ? employees.OrderByDescending(e => e.Name) : employees.OrderBy(e => e.Name);
                }
            }
            // আপনি যদি একটি ডিফল্ট সর্টিং চান, তবে এখানে একটি 'else' ব্লক যোগ করতে পারেন।

            // --- পেজিনেশন: স্কিপ এবং টেক (Skip, Take) ---
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            employees = employees.Skip(skipNumber).Take(query.PageSize);

            // --- ডেটা ফেরত দেয়া ---
            return await employees.ToListAsync();
        }




    }

}
