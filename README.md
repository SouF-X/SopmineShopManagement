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
- `tests` — .NET domain and infrastructure tests.

## Prerequisites

- .NET 10 SDK
- Node.js 20 or newer
- A local SQL Server instance for API development

The API applies Entity Framework Core migrations when it starts. Keep local connection strings, JWT secrets, admin passwords, and the OpenAI API key used for invoice scanning in ignored development configuration or environment variables. Never commit real credentials.

Invoice scanning uses OpenAI vision models. Set `OPENAI_API_KEY` in the process environment, or set `OpenAI__ApiKey` in the ignored `src/SopmineWorkshop.API/appsettings.Development.json` file.

## Local configuration

Create the ignored file `src/SopmineWorkshop.API/appsettings.Development.json`, or use environment variables/user secrets for local values. The committed files `src/SopmineWorkshop.API/.env.example` and `src/SopmineWorkshop.API/appsettings.Production.example.json` contain names and placeholders only.

When running in Development, the built-in seed configuration creates the initial admin account automatically:

- Email: `sopmineshop@gmail.com`
- Password: `SopmineShop2026!`

Change this password after the first login. Production refuses to start if this development password or any placeholder password is still configured; provide a different `DefaultAdmin__Password` through the production environment.

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

## Deploy with Docker

The production image contains the API and the frontend together; Node.js is only needed for the optional local frontend server. The container listens on port `8080` and requires a reachable SQL Server database plus production secrets.

Build the image from the repository root:

```bash
docker build -t sopmine-workshop .
```

Create a deployment environment file from `src/SopmineWorkshop.API/.env.example`, replace every `YOUR_` and `REPLACE_WITH_` value, and keep that file outside Git. Run the image with:

```bash
docker run --name sopmine-workshop --env-file ./src/SopmineWorkshop.API/.env -p 8080:8080 sopmine-workshop
```

The public reverse proxy should forward HTTPS traffic to container port `8080`. The app health endpoint is `/health`, and the frontend is served from `/`. Set `Cors__AllowedOrigins__0` only when the frontend is hosted on a separate HTTPS origin; leave it unset for the bundled same-origin frontend.

For a direct local .NET run, configure the ignored `src/SopmineWorkshop.API/appsettings.Development.json` or set the same variables in the shell before running `dotnet run`. Production configuration rejects placeholder JWT secrets, local/integrated SQL Server connections, and non-HTTPS CORS origins at startup.

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

## Repository rules

- Do not commit `.env` files, local development settings, database files, logs, build output, or credentials.
- Add database schema changes through Entity Framework Core migrations.
- Keep application behavior changes separate from repository and documentation cleanup.
- Review `git status` and `git diff` before staging a release.
