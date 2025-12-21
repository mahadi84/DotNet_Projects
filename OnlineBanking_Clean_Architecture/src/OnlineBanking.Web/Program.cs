using System.Security.Claims;                 // Claims-based authentication এর জন্য
using Microsoft.AspNetCore.Authentication.Cookies; // Cookie authentication middleware
using Microsoft.EntityFrameworkCore;           // EF Core DbContext ও migration
using Microsoft.Extensions.Options;            // Options pattern (IOptions<T>)
using OnlineBanking.Application.Contracts;     // Application layer interfaces
using OnlineBanking.Application.Options;       // BankingRulesOptions
using OnlineBanking.Infrastructure.Persistence; // AppDbContext
using OnlineBanking.Infrastructure.Services;   // Service implementations
using OnlineBanking.Infrastructure.Security;   // PasswordHasher
using OnlineBanking.Domain.Entities;            // Domain entities (Customer, Account, AuditLog)


// WebApplication builder তৈরি
// এখানে configuration + DI container setup হয়
var builder = WebApplication.CreateBuilder(args);


// MVC controller + Razor view enable করা
builder.Services.AddControllersWithViews(options =>
{
    // সব POST request এ automatically Anti-Forgery token check করবে
    // CSRF attack প্রতিরোধের জন্য
    options.Filters.Add(
        new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()
    );
});


// BankingRulesOptions কে appsettings.json এর BankingRules section এর সাথে bind করা
// Options pattern implementation
builder.Services.Configure<BankingRulesOptions>(
    builder.Configuration.GetSection("BankingRules")
);


// DbContext register করা (MySQL database)
// Connection string appsettings.json থেকে পড়া হচ্ছে
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("MySql");
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs));
});


// ===============================
// Authentication: Cookie based
// ===============================
builder.Services
    // Cookie authentication scheme ব্যবহার করা হবে
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)

    // Cookie configuration
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Account/Login";       // login না থাকলে এখানে redirect
        opt.LogoutPath = "/Account/Logout";     // logout URL
        opt.AccessDeniedPath = "/Account/Login"; // unauthorized হলে redirect

        opt.Cookie.HttpOnly = true;             // JS দিয়ে cookie access করা যাবে না
        opt.Cookie.SameSite = SameSiteMode.Strict; // CSRF protection
        opt.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS ছাড়া cookie যাবে না

        opt.SlidingExpiration = true;           // active থাকলে cookie time বাড়বে
        opt.ExpireTimeSpan = TimeSpan.FromMinutes(30); // session timeout
    });


// ===============================
// Authorization policy
// ===============================
builder.Services.AddAuthorization(opt =>
{
    // "AdminOnly" policy define
    // Claim: IsAdmin = true থাকতে হবে
    opt.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});


// ===============================
// Dependency Injection bindings
// ===============================

// Audit helper
builder.Services.AddScoped<AuditWriter>();

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Core banking service
builder.Services.AddScoped<IBankingService, BankingService>();

// PDF statement service
builder.Services.AddScoped<IStatementPdfService, StatementPdfService>();

// Admin service
builder.Services.AddScoped<IAdminService, AdminService>();


// App build করা (middleware pipeline তৈরি)
var app = builder.Build();


// =====================================================
// Seed Admin User on Startup
// =====================================================
//
// App চালু হওয়ার সময়:
// - Database migrate হবে
// - Admin user না থাকলে create হবে
//

using (var scope = app.Services.CreateScope())
{
    // Scoped service provider
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Pending migration থাকলে apply করবে
    await db.Database.MigrateAsync();

    // AdminSeed section থেকে config পড়া
    var adminCfg = app.Configuration.GetSection("AdminSeed");
    var email = adminCfg["Email"]!;
    var pass = adminCfg["Password"]!;
    var name = adminCfg["Name"] ?? "Admin";
    var city = adminCfg["City"] ?? "Dhaka";

    // একই email এর admin আগে থেকেই আছে কিনা check
    if (!await db.Customers.AnyAsync(c => c.Email == email.ToLower()))
    {
        // unique 5-digit account number generate
        string acc;
        int guard = 0;
        do
        {
            acc = Random.Shared.Next(10000, 100000).ToString();
            guard++;

            // infinite loop protection
            if (guard > 50)
                throw new Exception("Could not seed admin account number.");

        } while (await db.Customers.AnyAsync(x => x.AccountNumber == acc));

        // Admin customer object তৈরি
        var admin = new Customer
        {
            Name = name,
            Email = email.ToLower(),
            City = city,
            AccountNumber = acc,
            PasswordHash = PasswordHasher.Hash(pass), // secure password hash
            IsAdmin = true,                           // admin role
            Account = new Account { Balance = 0m }    // empty account
        };

        // DB তে admin save
        db.Customers.Add(admin);
        await db.SaveChangesAsync();

        // Audit log add
        db.AuditLogs.Add(new AuditLog
        {
            Action = "SYSTEM_SEED_ADMIN",
            ActorAccountNumber = "SYSTEM",
            TargetAccountNumber = acc,
            Message = $"Admin seeded: {email}"
        });

        await db.SaveChangesAsync();

        // Console এ admin account number দেখানো
        Console.WriteLine($"Seeded Admin AccountNumber: {acc}");
    }
}


// =====================================================
// Error handling (Production only)
// =====================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // friendly error page
    app.UseHsts();                          // strict HTTPS
}


// HTTPS redirect enable
app.UseHttpsRedirection();

// wwwroot static files (css, js, images)
app.UseStaticFiles();


// ===============================
// Security headers middleware
// ===============================
app.Use(async (ctx, next) =>
{
    // MIME sniffing disable
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";

    // Clickjacking protection
    ctx.Response.Headers["X-Frame-Options"] = "DENY";

    // Referrer privacy
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";

    // Content Security Policy (basic)
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'";

    await next();
});


// Routing middleware
app.UseRouting();

// Authentication middleware (cookie read)
app.UseAuthentication();

// Authorization middleware (policy check)
app.UseAuthorization();


// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Banking}/{action=Dashboard}/{id?}"
);


// Application start
app.Run();