using CBS.Application.Interfaces;
using CBS.Infrastructure.AddToRoute;
using CBS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// Registering services to the DI container
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHttpContextAccessor();

// ✅ Authentication + Authorization Setup (only once, before Build)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => // Configuring cookie authentication
    {
        options.LoginPath = "/Login/Login"; // Redirect to Login if user is unauthenticated
        options.LogoutPath = "/Login/Logout"; // Path for logout
        options.AccessDeniedPath = "/Login/AccessDenied"; // Path if access is denied
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Set cookie expiry time (8 hours)
    });

builder.Services.AddAuthorization(); // Setting up role-based access control (RBAC)

QuestPDF.Settings.License = LicenseType.Community; // Setting QuestPDF license to Community Edition

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Error handling and custom redirection
if (!app.Environment.IsDevelopment())
{
    // Global error handler for all errors except 404
    app.UseExceptionHandler("/Home/Error");

    // Handling 404 errors (Page Not Found) and redirecting to custom 404 error page
    app.UseStatusCodePagesWithRedirects("/Home/Error404"); // Redirect 404 errors to /Home/Error404

    app.UseHsts(); // Activates HSTS (Secure HTTP connections only)
}

app.UseHttpsRedirection(); // Redirect all HTTP requests to HTTPS
app.UseStaticFiles(); // Serve static files (like CSS, JS, Images)

app.UseRouting(); // Configure routing for controllers and actions

// ✅ Authentication and Authorization middleware must come here in the pipeline
app.UseAuthentication(); // Add authentication (cookies) middleware
app.UseAuthorization(); // Add authorization middleware

// Default route setup - if no controller/action is specified, route to Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Default controller is "Home" and action is "Index"

app.Run(); // Run the application, starting the web app