
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftXmlToExcel.Data;
using SwiftXmlToExcel.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XmlToExcelApp.Controllers
{
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;

        public TransactionController(AppDbContext context)
        {
            _context = context;
        }


        [HttpGet]
        public IActionResult Upload()
        {
            //var transactions = _context.Transactions.ToList();
            return View();
        }









        // ১. ফাইল আপলোড এবং প্রসেসিং
        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No File Found");

            string xmlText;
            using (var stream = new StreamReader(file.OpenReadStream())) { xmlText = stream.ReadToEnd(); }

            xmlText = Regex.Replace(xmlText, @"<\?xml.*?\?>", "");
            xmlText = "<Root>" + xmlText + "</Root>";
            var xmlContent = XDocument.Parse(xmlText);
            XNamespace pacs = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.08";

            // ইউনিক ব্যাচ আইডি এবং আপলোড টাইম
            string batchId = "BATCH-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            DateTime now = DateTime.Now;

            // Group Header থেকে তারিখ নেওয়া
            var grpHdr = xmlContent.Descendants(pacs + "GrpHdr").FirstOrDefault();
            DateTime creDt = DateTime.TryParse(grpHdr?.Element(pacs + "CreDtTm")?.Value, out var d) ? d : now;

            var transactions = (from trans in xmlContent.Descendants(pacs + "CdtTrfTxInf")
                                select new Transaction
                                {
                                    BatchId = batchId,
                                    UploadDate = now,
                                    BICFI = trans.Descendants(pacs + "FinInstnId").Descendants(pacs + "BICFI").FirstOrDefault()?.Value,

                                    Ref_No = trans.Element(pacs + "PmtId")?.Element(pacs + "InstrId")?.Value,
                                    I_Amount = decimal.TryParse(trans.Descendants(pacs + "InstdAmt").FirstOrDefault()?.Value, out decimal amt) ? amt : 0,
                                    D_Name = trans.Descendants(pacs + "Dbtr").Descendants(pacs + "Nm").FirstOrDefault()?.Value,
                                    C_Name = trans.Descendants(pacs + "Cdtr").Descendants(pacs + "Nm").FirstOrDefault()?.Value,
                                    C_Account = trans.Descendants(pacs + "CdtrAcct").Descendants(pacs + "Id").Descendants(pacs + "IBAN").FirstOrDefault()?.Value
                                                ?? trans.Descendants(pacs + "CdtrAcct").Descendants(pacs + "Id").Descendants(pacs + "Othr").Descendants(pacs + "Id").FirstOrDefault()?.Value,
                                    Settlement_Date = DateTime.TryParse(trans.Descendants(pacs + "IntrBkSttlmDt").FirstOrDefault()?.Value, out DateTime sDate) ? sDate : now,
                                    Transaction_Date = creDt,
                                    R_Info_Unstructured = trans.Descendants(pacs + "RmtInf").Descendants(pacs + "Ustrd").FirstOrDefault()?.Value
                                }).ToList();

            _context.Transactions.AddRange(transactions);
            _context.SaveChanges();

            return RedirectToAction("DownloadList");
        }

       
        
        
        
        
        
        
        
        
        
        
        // ২. ডাউনলোড লিস্ট ভিউ (ব্যাচ অনুযায়ী সামারি)
        public IActionResult DownloadList()
        {
            var summary = _context.Transactions
                .GroupBy(t => new { t.BatchId, t.UploadDate })
                .Select(g => new {
                    BatchId = g.Key.BatchId,
                    UploadTime = g.Key.UploadDate,
                    RecordCount = g.Count()
                })
                .OrderByDescending(x => x.UploadTime)
                .ToList();

            return View(summary);
        }















        // ৩. নির্দিষ্ট ব্যাচ ডাউনলোড করা
        public IActionResult DownloadBatch(string batchId)
        {
            var data = _context.Transactions.Where(t => t.BatchId == batchId).ToList();
            if (!data.Any()) return NotFound();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Transactions");

                // ১. হেডার আপডেট করা হয়েছে (প্রথম কলাম SL)
                string[] headers = { "SL", "BICFI", "Ref_No", "Amount", "Debtor", "Creditor", "Account", "Settle_Date" };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // ২. ডেটা লুপ এবং সিরিয়াল নম্বর যোগ করা
                for (int i = 0; i < data.Count; i++)
                {
                    int r = i + 2; // দ্বিতীয় রো থেকে ডেটা শুরু

                    worksheet.Cell(r, 1).Value = i + 1;             // Serial Number (1, 2, 3...)
                    worksheet.Cell(r, 2).Value = data[i].BICFI;
                    worksheet.Cell(r, 3).Value = data[i].Ref_No;

                    //worksheet.Cell(r, 4).Value = data[i].I_Amount;

                    // অ্যামাউন্ট সেট করা
                    var amountCell = worksheet.Cell(r, 4);
                    amountCell.Value = data[i].I_Amount;
                    // ২ দশমিক ঘর পর্যন্ত ফিক্সড ফরম্যাট সেট করা (যেমন: .80 বা 6.00 দেখাবে)
                    amountCell.Style.NumberFormat.Format = "0.00";

                    worksheet.Cell(r, 5).Value = data[i].D_Name;
                    worksheet.Cell(r, 6).Value = data[i].C_Name;
                    worksheet.Cell(r, 7).Value = data[i].C_Account;
                    worksheet.Cell(r, 8).Value = data[i].Settlement_Date.ToShortDateString();
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{batchId}.xlsx");
                }
            }
        }













        // ৪. ব্যাচ ডিলিট করা
        [HttpPost]
        public IActionResult DeleteBatch(string batchId)
        {
            var records = _context.Transactions.Where(t => t.BatchId == batchId);
            _context.Transactions.RemoveRange(records);
            _context.SaveChanges();
            return RedirectToAction("DownloadList");
        }
    








}
}
