using Login.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);




// ১. MySQL এর সাথে DbContext যোগ করা হলো
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// MySQL প্রোভাইডার কনফিগার করুন
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ২. কুকি-ভিত্তিক অথেন্টিকেশন যোগ করা হলো
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Login";               // লগইন না থাকলে এই রুটে যাবে (আপনার কন্ট্রোলার রুট)  
        options.AccessDeniedPath = "/Home/AccessDenied";  // আপনি চাইলে অন্য কোনো অ্যাক্সেস ডিনাই পেজ সেট করতে পারেন
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10); 
    });




// Add services to the container.
builder.Services.AddControllersWithViews();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ৩. অথেন্টিকেশন এবং অথরাইজেশন এনাবল করা
app.UseAuthentication();
app.UseAuthorization();


// অন্যান্য Middleware যেমন app.UseAuthentication() বা app.UseAuthorization() এর পরে
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");



app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
