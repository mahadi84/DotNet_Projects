using Microsoft.EntityFrameworkCore;
using System;
using WesterUnionPD.Data;
using WesterUnionPD.Middleware;
using WesterUnionPD.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();




// MySQL connection
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn));
});

// 1GB upload limit
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit =
        builder.Configuration.GetValue<long>("Upload:MaxUploadBytes");
});

// Dependency injection
builder.Services.AddScoped<ICsvAggregationService, CsvAggregationService>();
builder.Services.AddSingleton<IUploadJobQueue, UploadJobQueue>();
builder.Services.AddHostedService<UploadJobWorker>();
builder.Services.AddSingleton<IExcelExportService, ExcelExportService>();











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


// 🔐 IP restriction middleware (GLOBAL)
//app.UseMiddleware<IpWhitelistMiddleware>();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=BranchCharge}/{action=Upload}/{id?}");


app.Run();
