
using System;

namespace SwiftXmlToExcel.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string BatchId { get; set; } 
        public DateTime UploadDate { get; set; }
        public string BICFI { get; set; }
        public string Ref_No { get; set; }
        public decimal I_Amount { get; set; }
        public string D_Name { get; set; }
        public string C_Name { get; set; }
        public string C_Account { get; set; }
        public string R_Info_Unstructured { get; set; }
        public DateTime Settlement_Date { get; set; }
        public DateTime Transaction_Date { get; set; }
    }
}
