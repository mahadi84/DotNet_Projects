using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestAPI.Data;
using RestAPI.Interface;
using RestAPI.Models;
using RestAPI.Repositories;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);




// ১. MySQL এর সাথে DbContext যোগ করা হলো
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// MySQL প্রোভাইডার কনফিগার করুন
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


// Password Hasher from ef Identity
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.IPasswordHasher<User>, Microsoft.AspNetCore.Identity.PasswordHasher<User>>();



// -------------- JWT Start -----------------
// JWT Secret Key এর মান চেক করুন এবং একটি ভেরিয়েবলে রাখুন
var jwtSecret = builder.Configuration["AppSettings:Token"];

// নিরাপত্তা নিশ্চিত করার জন্য নাল চেক করুন
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("Configuration Error: JWT Secret Key 'AppSettings:Token' is missing or empty.");
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = signingKey // <--- AppSettings:Token থেকে আসা Key ব্যবহার করা হলো
        };
    });
// -------------- JWT end  ----------------------------






// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddScoped<IEmpRepository, EmpRepository>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}



// Middleware যোগ করা (অবশ্যই Authorization এর আগে থাকতে হবে)
app.UseAuthentication();
app.UseAuthorization();




// Scalar UI যোগ করুন (Development-এর শর্ত ছাড়াই সাধারণত যোগ করা হয়)
app.MapScalarApiReference();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
