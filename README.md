# AcxiomCRM — Enterprise Customer Relationship Management System

AcxiomCRM is an enterprise-grade Customer Relationship Management (CRM) application built on **ASP.NET Core (.NET 10)**, **Entity Framework Core 10.0.12**, and **SQL Server LocalDB**. It features strict Role-Based Access Control (RBAC), transactional lead conversion, automated audit trailing, live Chart.js analytics, streaming CSV exports, and secured RESTful APIs.

---

## 🏛️ Architecture & Tech Stack

- **Framework**: .NET 10.0 (ASP.NET Core MVC + Web API)
- **Database**: SQL Server LocalDB (`AcxiomCRM_Dev`) via Entity Framework Core 10.0.12
- **Authentication & Security**: ASP.NET Core Identity with PBKDF2 hashing, cookie authentication, anti-forgery tokens, and immutable audit logging
- **Role-Based Access Control (RBAC)**:
  - `Admin`: Full system control, user provisioning, role assignments, password resets, and security audit log inspection.
  - `Manager`: Comprehensive team visibility, all accounts/leads/opportunities, reports, and team-wide pipeline metrics.
  - `SalesExecutive`: Scoped access strictly to assigned accounts, owned leads, individual deals, and scheduled follow-ups.
- **Frontend / UI**: Bootstrap 5, Bootstrap Icons, responsive tables, modal dialogs, and interactive **Chart.js 4.4.1** visualizations.

---

## 🔑 Default Administrator Credentials

| Parameter | Configuration Value |
|---|---|
| **Email / Username** | `admin@acxiomcrm.local` |
| **Password** | `Admin@Acxiom2026!` |
| **Role** | `Admin` |
| **Config Location** | `appsettings.Development.json` |

*Note: Roles and initial administrator account are safely seeded upon application startup if not already present via `DbInitializer.cs`.*

---

## 📦 Core CRM Functional Modules

### 1. Executive Dashboard (`/`)
- Real-time KPI indicator cards: Total Customers, Active Leads, Total Pipeline Value, Weighted Pipeline Forecast, Won Deals, and Overdue Activities banner.
- Interactive Chart.js charts:
  - Opportunities by Stage (Bar chart)
  - Leads by Qualification Status (Doughnut chart)
  - Monthly Pipeline Volume Trend (Smooth line chart)
- Timeframe filter: All Time, Today, This Week, This Month, This Quarter, This Year.
- Upcoming Activities Feed with direct links to associated accounts and deals.

### 2. Customer Accounts (`/Customer`)
- Complete account directory with search, status filtering, and pagination.
- Duplicate email validation and relationship tracking.
- Account detail page with associated opportunities and activity history.
- Scoped data access: Sales executives view only assigned customers; Managers and Admins view all accounts.

### 3. Lead Management & Conversion (`/Lead`)
- Lead capture with attribution source (`Web`, `Referral`, `Advertisement`, `SocialMedia`, `Other`).
- Status progression (`New`, `Contacted`, `Qualified`, `Unqualified`, `Lost`, `Converted`).
- Priority ratings (`Low`, `Medium`, `High`).
- **Transactional Lead Conversion (`/Lead/Convert/{id}`)**: Converts qualified leads into customer accounts and optionally spins off an initial opportunity in a single atomic database transaction.

### 4. Sales Pipeline & Opportunities (`/Opportunity`)
- Visual deal tracker across stages: `Qualification` (10%), `Proposal` (40%), `Negotiation` (75%), `Won` (100%), and `Lost` (0%).
- Real-time computation of **Weighted Value** (`Amount * Probability / 100`).
- Business rule validations: Amount > $0, probability 0-100%, and future close date enforcement.

### 5. Follow-Ups & Activities (`/FollowUp`)
- Polymorphic associations linking activities to Customers, Leads, or Opportunities.
- Activity types: `Call`, `Meeting`, `Email`, `Task`.
- Overdue tracking and visual highlight tags.
- Modal-based one-click task completion and rescheduling.

### 6. Reports & Data Export Center (`/Report`)
- Six distinct operational reports:
  1. Customer Accounts Summary
  2. Lead Qualification & Acquisition
  3. Opportunity Pipeline & Weighted Forecasting
  4. Activity & Touchpoint History
  5. Lead Conversion Efficiency
  6. User & Security Activity Audit
- Live HTML tabular preview (`/Report/ViewReport`).
- High-performance, streaming CSV download endpoints (`/Report/ExportCsv`).

### 7. User & Role Administration (`/UserManagement`)
- Exclusive to `Admin` role.
- Provision new CRM users with initial credentials and assigned roles.
- Toggle active/inactive account status.
- Direct administrator password reset (`/UserManagement/ResetPassword`).
- Unlock accounts locked due to repeated failed logins.
- **Critical Last-Admin Protection**: Built-in guard preventing deactivation or demotion of the sole remaining active administrator.

### 8. Immutable Security Audit Trail (`/AuditLog`)
- Exclusive to `Admin` role.
- Comprehensive tracking of write operations (Create, Update, Delete, Convert, Status Change, Reset Password).
- Stores UTC timestamp, initiating user, entity type, record ID, IP address, and change details.

---

## 🌐 Secured REST API Endpoints

All API endpoints require authentication and return standard HTTP response codes (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`).

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/dashboard/kpis` | Real-time KPI summary payload |
| `GET` | `/api/dashboard/charts` | Chart.js dataset payload |
| `GET` | `/api/customers` | Paginated customer list with filters |
| `GET` | `/api/customers/{id}` | Customer account details |
| `POST` | `/api/customers` | Create new customer account |
| `PUT` | `/api/customers/{id}` | Update customer record |
| `DELETE` | `/api/customers/{id}` | Deactivate customer record |
| `GET` | `/api/leads` | Paginated lead list with filters |
| `POST` | `/api/leads` | Create new lead record |
| `POST` | `/api/leads/{id}/convert` | Transactionally convert lead to customer & deal |
| `GET` | `/api/opportunities` | Pipeline opportunities with stage filters |
| `POST` | `/api/opportunities` | Create sales opportunity |
| `GET` | `/api/followups` | Follow-up activities listing |
| `POST` | `/api/followups` | Schedule new activity |
| `POST` | `/api/followups/{id}/complete`| Mark activity as completed |
| `POST` | `/api/followups/{id}/reschedule`| Reschedule activity date |

---

## 🚀 Running the Project Locally

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- SQL Server LocalDB (included with Visual Studio or SQL Server Express)

### Commands

1. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

2. **Apply database migrations**:
   ```bash
   dotnet ef database update
   ```

3. **Run application**:
   ```bash
   dotnet run
   ```

4. **Access the application**:
   Open browser at `http://localhost:5224` (or `https://localhost:7216`).
   Log in with:
   - **Email**: `admin@acxiomcrm.local`
   - **Password**: `Admin@Acxiom2026!`
