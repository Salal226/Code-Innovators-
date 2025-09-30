IT Asset Manager

Track hardware assets, people, locations, software products, license seats and warranty dates — all in one place.

Built by Code Innovators (CPA Team)
Allison Daniel · Salal Al-Salami · Kazi Nafiur Rashid Alvi · Evan Arnold · Sabin Baral · Bipin Basnet · Shanita Boonratanayothin · Nishan Chokhal · Giriraj Dahal

Table of contents

Features

Tech stack

Screenshots

Getting started

Prereqs

Local setup

Database setup (SQL Server)

Upgrading from MySQL (one-time steps)

Running the app

How to use

Data model (quick ref)

Development workflow

Troubleshooting

Roadmap

License

Features

Dashboard: totals, in-repair, warranties expiring ≤ 30 days, software count, total license seats & seats in use

Assets: model/serial, status, purchase details, WarrantyExpiry; link to Person + Location

People: name, email, phone; computed FullName for UI

Locations: building, room, (optional) lab number

Software: products + one or more licenses (seats purchased, expiry, key)

Assignments: assign license seats to people/assets; see usage vs. capacity

Clean Bootstrap UI + gradient theme

Tech stack

.NET: ASP.NET Core 9.0 (MVC, Razor Views)

ORM: Entity Framework Core 9

Database: SQL Server (LocalDB/Express/Developer/Production) via Microsoft.EntityFrameworkCore.SqlServer

UI: Bootstrap 5, custom CSS (wwwroot/css/site.css)

Auth: ASP.NET Identity scaffolding (optional)

Screenshots

(Place images under wwwroot/img/screenshots/ and link them.)

Getting started
Prereqs

Visual Studio 2022 (or VS Code) with .NET 9 SDK

SQL Server (any of): LocalDB (ships with VS), Express, Developer, or full SQL Server

Optional: SQL Server Management Studio (SSMS)

Local setup
git clone https://github.com/<org-or-username>/Code-Innovators-.git
cd Code-Innovators-/ITAssetManager.Web


Update connection string in appsettings.Development.json:

Option A — LocalDB (easiest on VS2022):

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ITAssetManager;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}





Ensure the DI registration uses SqlServer (already set in Program.cs):

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

Database setup (SQL Server)

Create/update schema with EF Core migrations:

dotnet ef database update


When model changes later:

dotnet ef migrations add <MeaningfulName>
dotnet ef database update

Upgrading from MySQL (one-time steps)

If you cloned an older version that used MySQL:

Remove MySQL packages

Uninstall Pomelo.EntityFrameworkCore.MySql and MySqlConnector.

Add SQL Server provider

dotnet add package Microsoft.EntityFrameworkCore.SqlServer


Change DI to UseSqlServer (see snippet above) and update connection string.

Create fresh SQL Server migration
If the old MySQL migration history conflicts, reset migrations (dev only):

# Caution: drops your dev DB
dotnet ef database drop -f
# Remove old Migrations folder if needed (git commit the change)
dotnet ef migrations add InitialSqlServer
dotnet ef database update

Running the app

Visual Studio: Press F5 → https://localhost:<port>

CLI:

dotnet run --project ITAssetManager.Web

How to use

Add People (name/email/phone) and Locations (building/room).

Create Assets; link to person + location; set Warranty Expiry.

Add Software Products; then add one or more Licenses (seats purchased, expiry, key).

Create Assignments to consume seats; dashboard shows totals & usage.

Data model (quick ref)

Asset: Id, AssetTag, Name, Brand, Model, SerialNumber, Category, PurchaseCost, PurchaseDate, Status, WarrantyExpiry, PersonId?, LocationId?

UI alias: WarrantyEnd → maps to WarrantyExpiry (DB field).

Person: Id, FirstName?, LastName?, Email?, PhoneNumber?

[NotMapped] FullName (UI only; don’t use in EF-translated queries).

Location: Id, Building, Room, LabNumber?

SoftwareProduct: Id, Name, Vendor?, Version?, UnitCost?, Publisher?, Category?; navs: Licenses, Assignments

SoftwareLicense: Id, SoftwareProductId, SeatsPurchased, LicenseKey?, PurchaseDate?, ExpiryDate?

LicenseAssignment: Id, SoftwareProductId, SoftwareLicenseId?, PersonId?, AssetId?, IsActive, AssignedOn?

Development workflow

Feature branches → PR into main.

Add EF migration with any model change.

Use mapped props in EF queries (e.g., don’t OrderBy(p => p.FullName); do OrderBy(p => p.LastName).ThenBy(p => p.FirstName) or AsEnumerable() first).

Use WarrantyExpiry for queries; WarrantyEnd is a UI alias only.

Troubleshooting

“Cannot translate member ‘FullName’”
It’s [NotMapped]. Use mapped fields or switch to client eval: .AsEnumerable().OrderBy(p => p.FullName).

Pending model changes / migration errors
Run:

dotnet ef migrations add SyncModel
dotnet ef database update


For a hard reset in dev:

dotnet ef database drop -f
rd /s /q Migrations   # Windows (or: rm -rf Migrations)
dotnet ef migrations add InitialSqlServer
dotnet ef database update


Cannot connect to SQL Server

Verify server name & auth mode

For LocalDB: Server=(localdb)\MSSQLLocalDB

Add TrustServerCertificate=True when using dev certs.

Roadmap

Role-based auth (Admin/Viewer)

CSV import for assets/people

Email reminders for expiring warranties/licenses

Change history / audit trail

Minimal APIs for integrations

License

MIT — see LICENSE.
© 2025 Code Innovators

Maintainers

Allison Daniel · Salal Al-Salami · Kazi Nafiur Rashid Alvi · Evan Arnold · Sabin Baral · Bipin Basnet · Shanita Boonratanayothin · Nishan Chokhal · Giriraj Dahal



---

## Migration Note — Why we moved from MySQL to SQL Server (VS2022)

We initially built IT Asset Manager on **MySQL**, but standardized on **SQL Server** for a smoother developer experience in **Visual Studio 2022**. Key benefits:

- **LocalDB convenience:** `(localdb)\MSSQLLocalDB` works out-of-the-box in VS2022 (no manual service setup).
- **First-party EF Core provider:** `Microsoft.EntityFrameworkCore.SqlServer` = better LINQ translation & fewer provider quirks.
- **Built-in VS tooling:** Server Explorer, designers, profilers, and data viewers target SQL Server natively.
- **Reliable migrations:** More consistent EF Core migrations and clearer server-side translation behavior.
- **Simple auth & deployment:** Windows/Integrated auth for dev; easy move to SQL Auth/Azure SQL in test/prod.
- **Ecosystem tools:** SSMS/DACPAC make schema diffing, backups, and releases straightforward.

