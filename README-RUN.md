# Nexus Service Marketing System (ASP.NET Core 8 MVC + SQL Server)

Telephone and internet service marketing system for Nexus Communications: Dial-Up, Broadband and Landline
connections, retail shops, orders, feasibility checks, connections, equipment stock, bills and payments.

## 1. Requirements
1. **.NET 8 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0   (check: `dotnet --version`)
2. **SQL Server** - any one of: SQL Server Express (default instance name `.\SQLEXPRESS`), SQL Server Express LocalDB / Developer / Standard.

## 2. Choose the database server (appsettings.json -> ConnectionStrings:DefaultConnection)
| Server | Connection string |
|---|---|
| SQL Server Express (Windows login) - **default in appsettings.json, nothing to change** | `Server=.\SQLEXPRESS;Database=NexusServiceMarketingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| SQL Server Express LocalDB (if you have no SQLEXPRESS instance) | `Server=(localdb)\MSSQLLocalDB;Database=NexusServiceMarketingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` |
| SQL Server with SQL login | `Server=localhost;Database=NexusServiceMarketingDb;User Id=sa;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True` |

(In JSON write each backslash twice, e.g. `.\\SQLEXPRESS`.)

## 3. Create the database (migration)
Open PowerShell / Command Prompt **in the folder that contains `NexusServiceMarketingSystem.csproj`**:

```powershell
dotnet tool install --global dotnet-ef --version 8.*     # one time only (use "update" if already installed)
dotnet restore
dotnet ef database update
```
This applies all 5 migrations (`InitialCreate`, `CustomerSelfServiceOrders`, `VendorProductPurchases`,
`CustomerDocumentFiling`, `SpecGapFixes`), creates the database `NexusServiceMarketingDb` with all 19 tables and seeds the 18 plans and
the 4 bulk-discount bands. Shortcut: double-click `Database\Run-Migration.bat`.

Visual Studio alternative: Tools > NuGet Package Manager > Package Manager Console, then `Update-Database`.

No dotnet-ef? Open `Database\NexusServiceMarketingDb-schema.sql` in SSMS / Azure Data Studio, create an empty database
named `NexusServiceMarketingDb`, select it and run the script (it is the same migration, as plain SQL).
(With `sqlcmd` instead of SSMS, add the `-I` switch so QUOTED_IDENTIFIER is ON, as SSMS does by default.)

The application also runs `Database.Migrate()` automatically on start-up, so `dotnet run` alone creates/updates the DB too.

## 4. Run
```powershell
dotnet run
```
Open the address printed in the terminal (https://localhost:7012 or http://localhost:5294).

## 5. Login credentials
| Role | Username | Password |
|---|---|---|
| Admin (created automatically) | `admin` | `Admin@123` |

Change the admin password after the first login (top menu > Change password).
Every other login is created from inside the application:
1. Admin > **Cities** (add at least one city and its 3-digit code, e.g. `001`).
2. Admin > **Retail shops** (one per city), then Admin > **Employees**: create staff with role Accounts, Technical or
   RetailStaff (a retail employee must be assigned a shop). Each employee gets a username/password.
3. Customers register themselves (Login > Register) or are created by retail staff / admin.

## 6. Workflow
Customer or retail staff places an order (order number `D/B/T` + 10 digits) -> Technical runs feasibility checks ->
Technical creates the connection (16-character account ID `D/B/T` + 3-digit city + 12-digit serial; equipment is issued from
stock) -> Accounts generates bills -> payments are recorded by retail staff or accounts -> Technical suspends connections
with overdue bills (Technical > Overdue) and reactivates them once paid.
Anyone can track an order number or account ID on the public **Track** page (amounts due are shown
only after signing in).

## 6a. Business rules (as in the specification)
- **Dial-up needs a Nexus landline.** If the customer has none, choose a *landline plan* on the dial-up order: the telephone line
  and the dial-up are applied for together, both the landline and the internet feasibility checks are run, and Technical creates
  the telephone line (with its number) and the linked dial-up connection in one step.
- **Bulk / corporate discount** (25 / 50 / 75 / 100 %) uses all connections the customer takes: connections already provided
  (not permanently closed), orders still in progress, and the new order. It applies to the advance (first plan fee) and the deposit.
- **Bills** (Accounts > Generate bill, press *Show charges* first):
  - plan fee charged once per plan validity period (a quarterly plan every 3 months, a yearly rental once a year);
  - call charges calculated from local / STD / messaging-for-mobiles minutes and the plan's per-minute rates;
  - other usage (e.g. extra internet hours) entered as an amount;
  - equipment replaced because the customer spoiled it is added automatically to the next bill (once);
  - security deposit and bulk discount on the first bill; service tax 12.24 % on the whole bill;
  - the unpaid amount of earlier bills is shown as **brought forward**, so the customer sees the total due.
- **Postpaid status**: an active connection with a bill past its due date and not fully paid appears in Technical > Overdue,
  where it is set temporarily inactive; when all its bills are paid it is listed for reactivation.

## 7. Screens by role
- **Public**: Home, Plans & prices, Track order/account, Login, Register.
- **Admin**: cities, retail shops, employees, plans (add/edit/search/delete), discount schemes, vendors, products, purchases,
  customers, orders, advanced search (ID, name, contact, type, date range), reports, feedback, customer documents.
- **Retail staff**: customers, orders, shop connections, collect payments (only bills their shop may collect), payment history, advanced search (own shop).
- **Technical**: orders & feasibility (open orders first, finished orders stay visible), connections (create / temporary / permanent inactive / change plan / replace spoiled equipment), equipment & stock, advanced search.
- **Accounts**: generate bills, record payments, advanced search.
- **Customer**: profile, orders, connections (status and amount due), bills, documents, feedback.

Customer document files (PDF/PNG/JPEG, max 5 MB) are stored in `App_Data/customer-documents` (not public) and are filed by
customer, year and city.

## Starter data
On first start (only if the Cities table is empty) the app adds 5 cities (Karachi 001, Lahore 002, Islamabad 003,
Rawalpindi 004, Faisalabad 005) and one retail shop per city, so city dropdowns are not empty. Edit them in Admin > Master data.
