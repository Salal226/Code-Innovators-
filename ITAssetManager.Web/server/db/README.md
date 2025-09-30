# IT Asset Manager — Database Setup (SQL Server LocalDB)

These steps let any teammate rebuild the **ITAssetManager** database locally using **Visual Studio 2022** + **SQL Server LocalDB**.

---

## Prerequisites
- Visual Studio 2022 (with SQL Server Data Tools)
- SQL Server LocalDB (installed with VS)
- This repository cloned locally

> The schema script lives at:  
> `ITAssetManager.Web/server/db/V1__init.sql`

---

## A) Create the database in Visual Studio
1. **View → SQL Server Object Explorer**.
2. Connect to **(localdb)\MSSQLLocalDB**.
3. Right-click **Databases → New Query** and run:

```sql
IF DB_ID('ITAssetManager') IS NULL
    CREATE DATABASE ITAssetManager;
GO
USE ITAssetManager;
GO
