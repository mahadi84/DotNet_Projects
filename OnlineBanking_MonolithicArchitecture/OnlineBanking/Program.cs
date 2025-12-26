using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OnlineBanking.Contracts;
using OnlineBanking.Data;
using OnlineBanking.Entities;
using OnlineBanking.Options;
using OnlineBanking.Security;
using OnlineBanking.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + global antiforgery validation
builder.Services.AddControllersWithViews(options =>
{
    // Validates antiforgery token automatically for unsafe HTTP methods (POST/PUT/DELETE)
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.Configure<BankingRulesOptions>(builder.Configuration.GetSection("BankingRules"));

// EF Core + MySQL
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("MySql");
    opt.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// Cookie authentication (secure defaults)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Account/Login";
        opt.LogoutPath = "/Account/Logout";
        opt.AccessDeniedPath = "/Account/Login";

        opt.Cookie.HttpOnly = true;                          // prevents JS access
        opt.Cookie.SameSite = SameSiteMode.Strict;           // mitigates CSRF
        opt.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS only

        opt.SlidingExpiration = true;
        opt.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization(opt =>
{
    // Admin-only policy uses claim "IsAdmin" = "true"
    opt.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

// DI registrations
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBankingService, BankingService>();
builder.Services.AddScoped<IStatementPdfService, StatementPdfService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// Create DB + apply migrations + seed admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var adminCfg = app.Configuration.GetSection("AdminSeed");
    var email = adminCfg["Email"]!;
    var pass = adminCfg["Password"]!;
    var name = adminCfg["Name"] ?? "Admin";
    var city = adminCfg["City"] ?? "Dhaka";

    var normEmail = email.Trim().ToLowerInvariant();

    if (!await db.Customers.AnyAsync(c => c.Email == normEmail))
    {
        // generate unique 5-digit account
        string acc;
        int guard = 0;
        do
        {
            acc = Random.Shared.Next(10000, 100000).ToString();
            guard++;
            if (guard > 50) throw new Exception("Could not seed admin account number.");
        }
        while (await db.Customers.AnyAsync(x => x.AccountNumber == acc));

        var admin = new Customer
        {
            Name = name,
            Email = normEmail,
            City = city,
            AccountNumber = acc,
            PasswordHash = PasswordHasher.Hash(pass),
            IsAdmin = true,
            Account = new Account { Balance = 0m }
        };

        db.Customers.Add(admin);
        await db.SaveChangesAsync();

        db.AuditLogs.Add(new AuditLog
        {
            Action = "SYSTEM_SEED_ADMIN",
            ActorAccountNumber = "SYSTEM",
            TargetAccountNumber = acc,
            Message = $"Admin seeded: {normEmail}"
        });
        await db.SaveChangesAsync();

        Console.WriteLine($"Seeded Admin AccountNumber: {acc}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Basic security headers middleware
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net";
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Banking}/{action=Dashboard}/{id?}");

app.Run();
