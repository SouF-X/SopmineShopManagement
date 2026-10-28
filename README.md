# Sopmine Shop Management System

Sopmine is a custom full-stack management system for a sanitaryware business, built from requirements gathered directly from the shop owner.

## What problem does it solve?

The business previously relied heavily on paper records to manage products, customers, suppliers, purchases, sales, invoices, payments, debts, and stock. This made daily operations slow and information difficult to find. Entering a supplier purchase invoice manually could take between 5 and 20 minutes, while finding an old customer invoice could take approximately 10 to 15 minutes.

Sopmine centralizes these operations in one searchable digital platform. Customer invoices can be found in approximately five seconds, while AI-assisted supplier-invoice scanning can process and enter an invoice in around ten seconds. These figures are workflow estimates from the project context, not guarantees for every deployment.

The system also updates stock automatically after purchases and sales, provides low-stock alerts, tracks paid, partially paid, and unpaid documents, manages customer and supplier debts, and generates professional invoices and delivery notes.

By replacing repetitive manual data entry with digital records and AI-assisted invoice scanning, the solution is intended to reduce the estimated monthly data-entry cost from approximately $60–$100 to around $5–$10, depending on usage and operating costs.

## Project layout

- `src/SopmineWorkshop.API` — ASP.NET Core .NET 10 API and authentication entrypoint.
- `src/SopmineWorkshop.Application` — application features, commands, queries, validation, and DTOs.
- `src/SopmineWorkshop.Domain` — business entities and domain rules.
- `src/SopmineWorkshop.Infrastructure` — Entity Framework Core, SQL Server persistence, migrations, identity, and invoice extraction integrations.
- `src/SopmineWorkshop.Contracts` — request and shared contract types.
- `Frontend` — static HTML, CSS, and vanilla JavaScript pages served by the local Node server or published with the API.
- `tests` — .NET API, domain, infrastructure, frontend, and packaging tests.

## Portable Docker installation

The recommended installation uses Docker Compose to run the bundled application and a private persistent SQL Server Express container:

```powershell
Copy-Item .\.env.docker.example .\.env
# Replace every REPLACE_ value in .env.
docker compose up -d --build
```

Open `http://127.0.0.1:8080/` after both services are healthy. Docker users do not need to install the .NET SDK, Node.js, or SQL Server separately.

See the complete [Docker installation, update, backup, restore, and troubleshooting guide](docs/docker-installation.md).

See [recovered V3 release notes](docs/recovered-release.md) for the restored VPS fixes, migration compatibility, and verification limits.

## Source-development prerequisites

- .NET 10 SDK
- Node.js 20 or newer
- A local SQL Server instance for API development

The API applies Entity Framework Core migrations when it starts. Keep local connection strings, JWT secrets, admin passwords, and AI provider keys in ignored development configuration or environment variables. Never commit real credentials.

## Local configuration

Create the ignored file `src/SopmineWorkshop.API/appsettings.Development.json`, or use environment variables/user secrets for local values. The committed files `src/SopmineWorkshop.API/.env.example` and `src/SopmineWorkshop.API/appsettings.Production.example.json` contain names and placeholders only.

The local API defaults used by the start script are:

- API: `http://127.0.0.1:5269`
- Frontend: `http://127.0.0.1:5510`

## Run locally

The simplest local workflow starts both services:

```powershell
.\start-project.ps1
```

To start without opening a browser:

```powershell
.\start-project.ps1 -NoBrowser
```

Open `http://127.0.0.1:5510/`. The API health endpoint is available at `http://127.0.0.1:5269/health`.

To start only the API:

```powershell
dotnet run --project .\src\SopmineWorkshop.API\SopmineWorkshop.API.csproj --no-launch-profile
```

## Validate the repository

Restore, build, and run all .NET tests with:

```powershell
dotnet restore .\SopmineWorkshop.slnx
dotnet build .\SopmineWorkshop.slnx --configuration Release --no-restore
dotnet test .\SopmineWorkshop.slnx --configuration Release --no-restore
```

The frontend has no package manifest or runtime dependency installation step. Its JavaScript files are validated directly with Node.js, and the same checks run in GitHub Actions.

Run the frontend regression tests with:

```powershell
node --test .\tests\frontend-theme-init.test.mjs
```

## Repository rules

- Do not commit `.env` files, local development settings, database files, logs, build output, or credentials.
- Add database schema changes through Entity Framework Core migrations.
- Keep application behavior changes separate from repository and documentation cleanup.
- Review `git status` and `git diff` before staging a release.
