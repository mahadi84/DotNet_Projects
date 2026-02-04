using CBS.Application.Interfaces;
using CBS.Infrastructure.AddToRoute;
using CBS.Infrastructure.Services;
using Domain.Entities;
using QuestPDF.Infrastructure;


var builder = WebApplication.CreateBuilder(args);


// Dependancy Injection from CBS.Infrastructure.AddToRoute for MySQL and Table-Migration(dbup-mysql)
builder.Services.AddInfrastructure(builder.Configuration);

// Interface(located in app Layer), Implementation(Locted in infras. Layer)
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserService, UserService>();

//must add to use QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. 
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
