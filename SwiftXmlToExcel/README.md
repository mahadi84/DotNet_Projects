 SWIFT XML to Excel Processing Tool

## Overview

This application is designed to process ISO 20022 (pacs.008) XML files or text files containing XML data.
 It extracts financial transaction details, stores them in a database with unique batch tracking, and allows users to export specific batches into professionally formatted Excel files.

---

## Key Features

### 1. File Upload & Validation

- Multi-format Support: Accepts `.xml` and `.txt` files.
- Content Sanitization: Uses Regex to remove XML declarations and wraps content in a root element for safe parsing.
- Validation: - Checks for empty files or invalid extensions.
- Validates XML structure.
- Ensures the file contains relevant transaction tags (`CdtTrfTxInf`).



### 2. Data Extraction (ISO 20022)

The controller extracts the following fields using LINQ(Language Integrated Query-is special SQL by Microsoft) to XML:

<pacs:FinInstnId><pacs:BICFI> 
<pacs:CdtTrfTxInf><pacs:PmtId><pacs:InstrId> 
<pacs:InstdAmt Ccy="USD"> 
<pacs:Dbtr><pacs:Nm> 
<pacs:Cdtr><pacs:Nm> 
<pacs:CdtrAcct><pacs:Id> 
<pacs:RmtInf><pacs:Ustrd> 

- Batch Tracking: Generates a unique `BATCH-XXXX` ID for every upload.
- Header Info: Captures the Creation Date/Time (`CreDtTm`).
- Transaction Details:
- BICFI (Financial Institution Code)
- Reference Number (`InstrId`)
- Instructed Amount (`InstdAmt`)
- Debtor & Creditor Names
- Creditor Account (IBAN or Other ID)
- Settlement Date
- Unstructured Remittance Information (`RmtInf` > `Ustrd`)



### 3. Batch Management

- Summary View: Groups transactions by Batch ID and Upload Date.
- Record Counting: Shows how many transactions were processed in each batch.
- Deletion: Allows users to remove entire batches and their associated records from the database.

### 4. Excel Export (ClosedXML)

- Generates `.xlsx` files dynamically.
- Custom Formatting: - Bold headers with light gray background.
- Automatic serial numbering (SL).
- Fixed-point decimal formatting (`0.00`) for amounts.
- Auto-adjusting column widths to fit data.



---

## Technical Stack

- Framework: ASP.NET Core MVC
- Database: Entity Framework Core (`AppDbContext`)
- XML Parsing: `System.Xml.Linq` (XDocument)
- Excel Library: `ClosedXML`
- Regex: `System.Text.RegularExpressions`

---

## How to Use

1. Navigate to the "Upload" page.
2. Select a `.txt` or `.xml` file containing SWIFT pacs.008 data.
3. Upload the file. On success, you will be redirected to the "Download List".
4. Download the Excel file for any batch by clicking the download button.
5. Manage history by deleting old batches when no longer needed.

---

## Developer

Developed by: Muhammad Mahadi Hasan 
Year: 2025





.



