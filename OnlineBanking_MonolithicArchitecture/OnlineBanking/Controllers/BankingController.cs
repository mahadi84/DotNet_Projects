using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OnlineBanking.Contracts;
using OnlineBanking.Options;
using OnlineBanking.Models;

namespace OnlineBanking.Controllers;

[Authorize]
public sealed class BankingController : Controller
{
    private readonly IBankingService _banking;
    private readonly IStatementPdfService _pdf;
    private readonly BankingRulesOptions _rules;

    public BankingController(IBankingService banking, IStatementPdfService pdf, IOptions<BankingRulesOptions> rules)
    {
        _banking = banking;
        _pdf = pdf;
        _rules = rules.Value;
    }

    private Guid CustomerId => Guid.Parse(User.Claims.First(x => x.Type == "CustomerId").Value);

    private (string ip, string ua) ClientInfo()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = Request.Headers.UserAgent.ToString();
        return (ip, ua);
    }

    // Shows dashboard with profile + last transactions
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        ViewBag.Rules = _rules;
        ViewBag.Profile = await _banking.GetProfileAsync(CustomerId);
        ViewBag.Last10 = await _banking.GetLastTransactionsAsync(CustomerId, 10);
        return View();
    }

    // Deposits money
    [HttpPost]
    public async Task<IActionResult> Deposit(AmountVm vm)
    {
        var (ip, ua) = ClientInfo();

        if (!ModelState.IsValid)
        {
            TempData["Msg"] = "Invalid amount.";
            return RedirectToAction(nameof(Dashboard));
        }

        var res = await _banking.DepositAsync(CustomerId, vm.Amount, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Dashboard));
    }

    // Withdraws money (minimum balance enforced in service)
    [HttpPost]
    public async Task<IActionResult> Withdraw(AmountVm vm)
    {
        var (ip, ua) = ClientInfo();

        if (!ModelState.IsValid)
        {
            TempData["Msg"] = "Invalid amount.";
            return RedirectToAction(nameof(Dashboard));
        }

        var res = await _banking.WithdrawAsync(CustomerId, vm.Amount, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Dashboard));
    }

    // Transfers money to another 5-digit account number
    [HttpPost]
    public async Task<IActionResult> Transfer(TransferVm vm)
    {
        var (ip, ua) = ClientInfo();

        if (!ModelState.IsValid)
        {
            TempData["Msg"] = "Invalid transfer input.";
            return RedirectToAction(nameof(Dashboard));
        }

        var res = await _banking.TransferAsync(CustomerId, vm.ToAccountNumber, vm.Amount, ip, ua);
        TempData["Msg"] = res.Message;
        return RedirectToAction(nameof(Dashboard));
    }

    // Generates mini statement PDF (last 10 tx)
    [HttpGet]
    public async Task<IActionResult> MiniStatementPdf()
    {
        var profile = await _banking.GetProfileAsync(CustomerId);
        var last10 = await _banking.GetLastTransactionsAsync(CustomerId, 10);

        var pdfBytes = _pdf.BuildMiniStatementPdf(
            currency: _rules.CurrencySymbol,
            tzId: _rules.TimeZoneId,
            accountNumber: profile.AccountNumber,
            customerName: profile.CustomerName,
            currentBalance: profile.Balance,
            last10: last10);

        return File(pdfBytes, "application/pdf", $"mini-statement-{profile.AccountNumber}.pdf");
    }
}
