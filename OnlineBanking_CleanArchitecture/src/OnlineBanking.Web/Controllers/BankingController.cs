using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OnlineBanking.Application.Contracts;
using OnlineBanking.Application.Options;
using OnlineBanking.Web.Models;

namespace OnlineBanking.Web.Controllers
{
    [Authorize] // authenticated user ছাড়া controller access করা যাবে না
    public sealed class BankingController : Controller
    {
        private readonly IBankingService _banking;    // deposit/withdraw/transfer/business logic
        private readonly IStatementPdfService _pdf;   // PDF generate service
        private readonly BankingRulesOptions _rules;  // config rules snapshot

        // DI: services + options inject
        public BankingController(IBankingService banking, IStatementPdfService pdf, IOptions<BankingRulesOptions> rules)
        {
            _banking = banking;
            _pdf = pdf;
            _rules = rules.Value; // options থেকে actual rules object নেওয়া
        }

        // Cookie claims থেকে CustomerId বের করা
        // login সময় claim সেট করা হয়েছিল
        private Guid CustomerId => Guid.Parse(User.Claims.First(x => x.Type == "CustomerId").Value);

        // client info collect (audit log এ যাবে)
        private (string ip, string ua) ClientInfo()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ua = Request.Headers.UserAgent.ToString();
            return (ip, ua);
        }

        // Dashboard page (GET)
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // rules view এ পাঠানো (minimum balance, daily limit দেখাতে)
            ViewBag.Rules = _rules;

            // profile info view এ পাঠানো (name, acc, balance)
            ViewBag.Profile = await _banking.GetProfileAsync(CustomerId);

            // last 10 transactions view এ পাঠানো
            ViewBag.Last10 = await _banking.GetLastTransactionsAsync(CustomerId, 10);

            return View(); // Dashboard.cshtml render
        }

        // Deposit action (POST)
        [HttpPost]
        public async Task<IActionResult> Deposit(AmountVm vm)
        {
            var (ip, ua) = ClientInfo(); // audit info

            // input invalid হলে message set করে dashboard এ redirect
            if (!ModelState.IsValid)
            {
                TempData["Msg"] = "Invalid amount.";
                return RedirectToAction(nameof(Dashboard));
            }

            // deposit service call
            var res = await _banking.DepositAsync(CustomerId, vm.Amount, ip, ua);

            // feedback message store (one request)
            TempData["Msg"] = res.Message;

            return RedirectToAction(nameof(Dashboard));
        }

        // Withdraw action (POST)
        [HttpPost]
        public async Task<IActionResult> Withdraw(AmountVm vm)
        {
            var (ip, ua) = ClientInfo();

            if (!ModelState.IsValid)
            {
                TempData["Msg"] = "Invalid amount.";
                return RedirectToAction(nameof(Dashboard));
            }

            // withdraw service call (min balance rule check হয় Infrastructure/Service এ)
            var res = await _banking.WithdrawAsync(CustomerId, vm.Amount, ip, ua);

            TempData["Msg"] = res.Message;

            return RedirectToAction(nameof(Dashboard));
        }

        // Transfer action (POST)
        [HttpPost]
        public async Task<IActionResult> Transfer(TransferVm vm)
        {
            var (ip, ua) = ClientInfo();

            // input invalid হলে
            if (!ModelState.IsValid)
            {
                TempData["Msg"] = "Invalid transfer input.";
                return RedirectToAction(nameof(Dashboard));
            }

            // transfer service call (daily limit + min balance + audit etc.)
            var res = await _banking.TransferAsync(CustomerId, vm.ToAccountNumber, vm.Amount, ip, ua);

            TempData["Msg"] = res.Message;

            return RedirectToAction(nameof(Dashboard));
        }

        // Mini statement PDF download (GET)
        [HttpGet]
        public async Task<IActionResult> MiniStatementPdf()
        {
            // profile fetch
            var profile = await _banking.GetProfileAsync(CustomerId);

            // last10 fetch
            var last10 = await _banking.GetLastTransactionsAsync(CustomerId, 10);

            // PDF bytes generate
            var pdfBytes = _pdf.BuildMiniStatementPdf(
                currency: _rules.CurrencySymbol,      // ৳
                tzId: _rules.TimeZoneId,              // Asia/Dhaka
                accountNumber: profile.AccountNumber, // logged-in account
                customerName: profile.CustomerName,   // name
                currentBalance: profile.Balance,      // current balance
                last10: last10                        // transactions
            );

            // HTTP file response: content-type application/pdf
            // filename dynamic based on account number
            return File(pdfBytes, "application/pdf", $"mini-statement-{profile.AccountNumber}.pdf");
        }
    }
}
