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

## Build

```powershell
dotnet build Khotwa.slnx
```

Local development settings, secrets, database files, build output, and runtime uploads are excluded from Git.
