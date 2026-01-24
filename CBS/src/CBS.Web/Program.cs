using CBS.Application.Interfaces;
using CBS.Infrastructure.AddToRoute;
using CBS.Infrastructure.Services;
using Domain.Entities;


var builder = WebApplication.CreateBuilder(args);


// Dependancy Injection from CBS.Infrastructure.AddToRoute for MySQL and Migration
builder.Services.AddInfrastructure(builder.Configuration);

// Interface এবং এর Implementation ক্লাস রেজিস্টার করা
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();



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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
