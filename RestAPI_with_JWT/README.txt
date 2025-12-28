
# RestAPI - Secure Employee Management System

## Overview

The `EmployeeController` provides a fully secured RESTful interface for managing employee records. 
This module implements industry-standard security practices, including JWT token validation, role-based authorization, and the Repository Pattern for data abstraction.

---

## Technical Features

### 1. Advanced Security & Authorization

- JWT Authentication: All endpoints are protected with the `[Authorize]` attribute. Users must provide a valid Bearer token.
- Role-Based Access Control (RBAC): - Admin: Full access (Create, Read, Update, Delete).
- User: Limited access (Read and Search only).


- Identity Integration: Uses `System.Security.Claims` to extract the `NameIdentifier` from the logged-in user's token to link records to specific users.

### 2. Architecture & Design Patterns

- Repository Pattern: Decouples the controller from the database logic using `IEmpRepository`, making the code testable and maintainable.
- DTO Mapping: Uses a dedicated Mapper pattern (`ToEmpReadDto`, `ToEmp`) to ensure internal database models are never exposed directly to the client.
- Data Validation: Implements asynchronous email duplicate checks and `ModelState` validation.

### 3. Rich API Functionality

- Search & Filtering: Supports advanced queries (filtering and sorting) via a `QueryObject`.
- Resource Management: Standardized CRUD operations with appropriate HTTP status codes (201 Created, 200 Ok, 401 Unauthorized, 403 Forbidden, 404 Not Found).

---

## Technology Stack & Packages

### Core Technologies

- ASP.NET Core Web API: Framework for building the RESTful services.
- Entity Framework Core: Object-Relational Mapper (ORM) for SQL interactions.
- JWT (JSON Web Tokens): For secure, stateless authentication.

### Nuget Packages Used

- Microsoft.AspNetCore.Authentication.JwtBearer: To enable and validate JWT tokens.
- Microsoft.EntityFrameworkCore.SqlServer: Database provider for SQL Server.
- Microsoft.EntityFrameworkCore.Design: Tools for migrations and database modeling.
- AutoMapper (Optional): If utilized in the `RestAPI.Mappers` namespace for object-to-object mapping.

---

## API Documentation (Endpoints)

| Method | Endpoint | Authorization | Description |
| --- | --- | --- | --- |
| GET | `/api/Employee` | Admin, User | Get all employees (converted to DTOs). |
| GET | `/api/Employee/{id}` | Any Authorized | Get details of a specific employee. |
| GET | `/api/Employee/search` | Any Authorized | Search/Filter employees using query parameters. |
| POST | `/api/Employee` | Admin Only | Create a new employee (linked to Creator ID). |
| PUT | `/api/Employee/{id}` | Any Authorized | Update employee details (Name, Email, etc.). |
| DELETE | `/api/Employee/{id}` | Admin Only | Permanently remove an employee record. |

---

## Developer

Developed by: Muhammad Mahadi Hasan
Project: Secure RestAPI System
Year: 2025







.
