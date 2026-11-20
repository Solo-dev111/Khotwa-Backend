# Khotwa Backend

ASP.NET Core API for the Khotwa course platform, using .NET 10 and SQL Server.

## Local development

Install the .NET 10 SDK and configure your SQL Server connection:

```powershell
dotnet restore
dotnet user-secrets set "ConnectionStrings:KhotwaConnection" "Server=YOUR_SERVER;Database=Khotwa;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_OF_AT_LEAST_32_BYTES"
dotnet run --launch-profile https
```

Apply the Entity Framework Core migrations to your development database before using the API:

```powershell
dotnet ef database update
```

The migration command requires the .NET 10 `dotnet-ef` tool.
Swagger is available at `https://localhost:7194/swagger`.
The Angular frontend is hosted in the separate Khotwa-Frontend repository and runs locally at `http://localhost:4200`.

## First administrator (local development)

After applying migrations to a fresh database, open the Angular registration page.
If the Users table is empty, the Admin option is available and selected by default.
Create the administrator **before** registering students or instructors.
Once any account exists, public administrator signup is blocked by the API,
including direct requests to `POST /api/Auth/register/admin`.
Existing administrators can still manage users through the admin-only endpoints.

This bootstrap endpoint is enabled only in the Development environment. Do not
expose a development API with an empty database to untrusted networks. Production
administrators must be provisioned through a trusted deployment process.

To test on a fresh database without deleting your existing data, configure a separate
development database, apply the existing migrations, and verify the following:

1. `GET /api/Auth/registration-status` returns `canRegisterAdmin: true`.
2. Register the first administrator from Angular and verify admin navigation.
3. The status becomes false; a second direct admin signup returns HTTP 403.
4. Student and instructor registration continue to work normally.

## Build

```powershell
dotnet build Khotwa.slnx
```

Local development settings, secrets, database files, build output, and runtime uploads are excluded from Git.
