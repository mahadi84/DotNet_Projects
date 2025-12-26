


OnlineBankingMono (.NET 9 / ASP.NET Core MVC)

Used Monolithic Architecture

Requirements:
- Visual Studio 2022 with .NET 9 SDK installed (or .NET 9 SDK via Visual Studio Installer)
- MySQL running locally

Setup:
1) Open OnlineBankingMono.sln in Visual Studio 2022
2) Edit OnlineBanking/appsettings.json -> set MySQL password
3) Create DB in MySQL:
   CREATE DATABASE onlinebanking_db;

4) Restore NuGet packages (VS does automatically on build)
   NOTE: This project targets net9.0 and references EF Core 9.0.0 + Pomelo 9.0.0 +EF core tools.
   If your NuGet sources don't have these yet, you can change the versions in OnlineBanking.csproj
   to whatever 9.0.* versions are available for you.

5) Run migrations:
   - Tools -> NuGet Package Manager -> Package Manager Console
   - Select Default project: OnlineBanking
   - Run:
       Add-Migration InitialCreate
       Update-Database



Functionalities:


## Customer Registration
    register with Name, Email, City, Password →  Auto-generated 5-digit Account Number
   * Email unique 
   * Password BCrypt

## Security Features:
   * Password hashing (BCrypt)
   * CSRF protection (Auto Anti-Forgery)
   * Brute-force protection (lockout)
   * Cookie hardening                 
   * Secure headers:                  
     * X-Frame-Options                
     * X-Content-Type-Options         
     * CSP(Content Security Policy)   
   * HTTPS ready (IIS friendly)

## Customer Login + Lockout 
   * Login  Account Number + Password, lock if 5 times wrong password
      
   * Cookie-based authentication
   * Secure cookies:
     * HttpOnly     
     * SameSite = Strict
     * Secure (HTTPS friendly)

## Audit Logs (Real Banking Feature)
   -Every sensitive action record 


## Admin Functionalities:
    * All customers list view
    * Make / Remove Admin
    * Lock customer account (manual)
    * Unlock customer account
    * Reset customer password
    * View full Audit Logs


## Withdraw Money
   * Balance sufficient -no  withdraw 
   * Minimum Balance Rule enforced
   * Transaction record
   * Audit log

 ## Deposit Money
   * Positive amount check
   * Balance +
   * Transaction table-record
   * Audit log

## Transfer Money
   * on account to other account transfer
   * Same account-transfer will block
   * Daily Transfer Limit enforced
   * Minimum balance after transfer enforce
   * Sender → TransferOut
   * Receiver → TransferIn
   * Double transaction entry (ledger style)
   * Audit log


## PDF Mini Statement
   * Downloadable PDF
   * Last 10 transactions
   * Customer name, account number
   * Current balance
   * Asia/Dhaka time conversion
   * Currency symbol configurable (৳ / € / $)

> Note: It is production-ready starter। 
For live it require MFA/OTP, KYC/AML, device fingerprinting, fraud scoring, WAF, HSM, key rotation.




Muhammad Mahadi Hasan
Mob: 01616-829529
Email: mahadi.engr@gmail.com






.