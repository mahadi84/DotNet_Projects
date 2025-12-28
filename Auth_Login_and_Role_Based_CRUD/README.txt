# LoginApp - Secure Employee Management (MVC)

## Overview

The `EmployeeController` manages employee records with a strong emphasis on Data Privacy and User Ownership. 
It ensures that standard users can only interact with their own data, while providing advanced features like server-side pagination, multi-column sorting, and real-time searching.

---

## Key Features

### 1. Ownership-Based Security (Horizontal Privacy)

- Data Isolation: Standard users are strictly restricted to viewing, editing, and deleting only the records they created. This is enforced by filtering all queries using the `ClaimTypes.NameIdentifier` from the user's authentication cookie.
- Admin Oversight: Users with the "Admin" role can bypass ownership filters to manage all employee records across the system.

### 2. Advanced Data Grid (Index)

- Server-Side Pagination: Efficiently handles large datasets by only fetching 5 records per page (configurable via `PageSize`).
- Dynamic Sorting: Users can sort by Name or Email in both ascending and descending order.
- Persistent Filtering: Maintains search strings and sort orders across page navigations using `ViewData`.

### 3. Robust CRUD Operations

- Validation: Implements server-side `ModelState` validation and duplicate email checks.
- PRG Pattern: Follows the Post-Redirect-Get pattern using `TempData` to prevent form resubmission errors and provide user feedback.
- Concurrency Handling: Includes `DbUpdateConcurrencyException` handling during updates to ensure data integrity.

---

## Technology Stack & Packages

### Core Technologies

- ASP.NET Core MVC: For the web interface and controller logic.
- Entity Framework Core: For database communication.
- ASP.NET Core Identity: For authentication and role-based claims.

### Nuget Packages Used

- Microsoft.EntityFrameworkCore.SqlServer: SQL Server database provider.
- Microsoft.AspNetCore.Authorization: For securing the controller with the `[Authorize]` attribute.
- Microsoft.EntityFrameworkCore.Tools: For database migrations and management.

---

## Logic & Flow Diagram

The following diagram illustrates how the `Index` action filters data based on the logged-in user's role:

---

## Controller Endpoints (Routes)

| Action | Method | Security | Description |
| --- | --- | --- | --- |
| `Index` | GET | Authorized | Lists employees (Filtered by User or Admin role). |
| `CreateForm` | GET | Authorized | Displays the blank employee creation form. |
| `CreateEmployee` | POST | Authorized | Validates and saves new employee linked to the User's ID. |
| `Edit` | GET/POST | Owner/Admin | Securely modifies existing employee records. |
| `DeleteConfirmed` | POST | Owner/Admin | Removes an employee after verifying ownership. |

---

## Implementation Notes

- PaginatedList<T>: A custom helper class is used to wrap `IQueryable` results for cleaner pagination logic in the View.
- ValidationAntiForgeryToken: Every POST action is protected against Cross-Site Request Forgery (CSRF).

---

## Developer

Developed by: Muhammad Mahadi Hasan 
Project: Login & Employee Tracking System
 Year: 2025




.
