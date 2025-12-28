
# WesterUnionPD - Branch Charge Processing System

## Overview

The `BranchChargeController` is a high-performance module designed to handle large CSV uploads (up to 1GB). 
It utilizes a background queueing system to ensure the web interface remains responsive during long-running data processing tasks.

---

## Technical Features

### 1. Large File Handling & Security

- Max Upload Limit: Configurable via `appsettings.json` (currently set for 1GB).
- Secure Temp Storage: Uploaded files are stored in a protected system temp directory rather than `wwwroot` to prevent unauthorized access.
- AJAX Integration: Supports `XMLHttpRequest` uploads, enabling real-time client-side progress bars.

### 2. Asynchronous Job Queueing

- Non-Blocking UI: Instead of processing data inside the HTTP request, the controller enqueues a job and immediately returns a `JobId`.
- Status Tracking: Tracks job states: `Queued`, `Processing`, and `Completed`.
- Job History: Automatically retrieves and displays the 10 most recent upload attempts.

### 3. Data Aggregation & Export

- Branch Summaries: Filters and groups data by `AbdCode`.
- Excel Generation: Uses a dedicated service (`IExcelExportService`) to transform database summaries into downloadable `.xlsx` files.

---

## Technology Stack & Packages

### Core Technologies

- ASP.NET Core 8.0/9.0 (MVC): For the web interface and API endpoints.
- Entity Framework Core: For database operations and job tracking.
- Asynchronous Programming (TAP): Extensive use of `Task`, `await`, and `CancellationToken` for scalability.

### Nuget Packages Used

- Microsoft.EntityFrameworkCore: Database ORM.
- Microsoft.EntityFrameworkCore.SqlServer: (Or your specific DB provider).
- ClosedXML / EPPlus: (Utilized within `IExcelExportService` for Excel generation).
- Microsoft.Extensions.Configuration: For reading system limits (MaxBytes).

---

## API Endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/BranchCharge/Upload` | View upload dashboard and recent job history. |
| POST | `/BranchCharge/UploadAjax` | Upload CSV via AJAX. Returns `jobId` and `redirectUrl`. |
| GET | `/BranchCharge/Result/{id}` | Real-time status page for a specific processing job. |
| GET | `/BranchCharge/Download/{id}` | Export completed job results to Excel. |

---

## Setup Requirements

1. Configuration: Ensure `Upload:MaxUploadBytes` is defined in `appsettings.json`.
2. Services: Register `IUploadJobQueue` and `IExcelExportService` in `Program.cs`.
3. Permissions: Ensure the application pool has write permissions to the system `Temp` directory.

---

## Developer

Developed by: Muhammad Mahadi Hasan
Project: WesterUnionPD (Processing Dashboard)
Year: 2025

