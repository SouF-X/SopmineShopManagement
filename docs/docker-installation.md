# Portable Docker installation

This is the host-independent installation path for Sopmine Shop Management. It runs the bundled frontend and API in one application container and SQL Server Express in a separate private container. Customer-specific domains, tunnels, and proxies are added afterward without changing application source code.

## Requirements

- An x64 Windows or Linux computer capable of running Linux containers.
- Docker Desktop or Docker Engine with Docker Compose.
- At least 4 GB of free memory for Docker; more is recommended for the host computer.
- Enough disk space for Docker images, the SQL data volume, and backups.
- A computer configured not to sleep while the application is expected to be available.

Docker users do not need to install the .NET SDK, Node.js, or SQL Server separately.

## 1. Create private configuration

From the repository root, copy the tracked example to the ignored `.env` file:

```powershell
Copy-Item .\.env.docker.example .\.env
```

On Linux or macOS:

```bash
cp .env.docker.example .env
```

Open `.env` and replace every value beginning with `REPLACE_`.

- `SQL_SA_PASSWORD`: use at least 16 characters with uppercase, lowercase, numbers, and symbols.
- `JWT_SECRET`: use a random value of at least 32 bytes. Do not reuse the SQL or administrator password.
- `DEFAULT_ADMIN_EMAIL`: the real email used for the first login.
- `DEFAULT_ADMIN_PASSWORD`: at least 10 characters with uppercase, lowercase, a number, a symbol, and at least four unique characters.
- `GEMINI_API_KEY`: optional until AI invoice extraction is used.

Generate a strong JWT secret in PowerShell:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

Paste the generated value into the private `.env` file. Never commit `.env`.

## 2. Start a fresh installation

Make sure Docker is using Linux containers, then run:

```powershell
docker compose up -d --build
docker compose ps
```

The first startup downloads SQL Server Express, builds the application image, creates persistent volumes, applies EF Core migrations, creates roles, and creates the initial administrator. SQL startup can take one or two minutes on its first run.

Check the services and application:

```powershell
docker compose ps
docker compose logs --tail 100 app
Invoke-WebRequest http://127.0.0.1:8080/health -UseBasicParsing
Invoke-RestMethod http://127.0.0.1:8080/api/status
```

Open `http://127.0.0.1:8080/` and sign in with `DEFAULT_ADMIN_EMAIL` and `DEFAULT_ADMIN_PASSWORD` from `.env`.

The application listens only on `127.0.0.1` by default. SQL Server has no published host port.

## Normal lifecycle

View status and recent logs:

```powershell
docker compose ps
docker compose logs --tail 100
docker compose logs --tail 100 app
docker compose logs --tail 100 db
```

Restart without deleting data:

```powershell
docker compose restart
```

Stop and remove containers while preserving database and key volumes:

```powershell
docker compose down
```

Start the existing installation again:

```powershell
docker compose up -d
```

Configure Docker to start with the operating system. On an owner's PC, also disable sleep/hibernation and confirm there is enough free disk space for SQL data and backups.

## Update the application safely

Create and verify a database backup first. Then update the source/release files and run:

```powershell
docker compose up -d --build
docker compose ps
Invoke-WebRequest http://127.0.0.1:8080/health -UseBasicParsing
```

Compose replaces the application container but preserves the named SQL and data-protection volumes. Startup applies new EF Core migrations automatically.

Changing `DEFAULT_ADMIN_PASSWORD` in `.env` does not reset an administrator that already exists. Change existing passwords through the application.

## Back up SQL Server

The following PowerShell commands use the default database name `SopmineShop`. If `SQL_DATABASE` was changed, replace `[SopmineShop]` in the SQL commands with that configured database name.

```powershell
New-Item -ItemType Directory -Path .\backups -Force | Out-Null
$BackupName = "sopmine-$(Get-Date -Format 'yyyyMMdd-HHmmss').bak"
docker compose exec -T db mkdir -p /var/opt/mssql/backup
docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "BACKUP DATABASE [SopmineShop] TO DISK = N'/var/opt/mssql/backup/$BackupName' WITH INIT, CHECKSUM"
docker compose cp "db:/var/opt/mssql/backup/$BackupName" ".\backups\$BackupName"
Get-Item ".\backups\$BackupName"
```

The `.bak` file is stored in the ignored `backups` directory. Copy verified backups to another physical device or secure off-machine location. A backup left only on the server PC does not protect against disk failure or theft.

If `/opt/mssql-tools18/bin/sqlcmd` is unavailable in a future SQL image, inspect the container and use `/opt/mssql-tools/bin/sqlcmd` instead.

## Restore a backup

Restoring replaces the current database. Stop the application and verify the selected backup filename before running these commands:

```powershell
$BackupFile = Resolve-Path .\backups\sopmine-YYYYMMDD-HHMMSS.bak
$BackupName = Split-Path $BackupFile -Leaf
docker compose cp $BackupFile "db:/var/opt/mssql/backup/$BackupName"
docker compose stop app
docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "ALTER DATABASE [SopmineShop] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [SopmineShop] FROM DISK = N'/var/opt/mssql/backup/$BackupName' WITH REPLACE; ALTER DATABASE [SopmineShop] SET MULTI_USER"
docker compose start app
Invoke-WebRequest http://127.0.0.1:8080/health -UseBasicParsing
```

Replace the example backup filename with the exact file being restored. Rehearse restore procedures before relying on them for a real business.

## Hosting overlays

The base installation deliberately contains no public ingress. After local verification, a hosting layer can forward traffic to `http://127.0.0.1:8080`:

- Cloudflare Tunnel for an outbound-only domain connection;
- IIS on Windows;
- Caddy or Nginx on a server;
- a container platform's HTTPS ingress.

Domain names, TLS, proxy trust, and Cloudflare Tunnel tokens belong to host configuration, not source code or the base application image.

## Troubleshooting

If the database remains unhealthy:

```powershell
docker compose ps
docker compose logs --tail 200 db
```

Common causes are an unchanged placeholder SQL password, a password that does not meet SQL Server complexity requirements, insufficient Docker memory, or insufficient disk space.

If the application exits:

```powershell
docker compose logs --tail 200 app
```

Production startup intentionally rejects placeholder JWT/admin values, invalid administrator passwords or roles, and invalid connection configuration. Correct `.env`, then run `docker compose up -d` again.

If port `8080` is already in use, change `APP_PORT` in `.env` and use that port in the health URL.

## Permanently delete all application data

**Danger: Permanently delete all application data only when a complete reset is explicitly intended. The following command deletes the SQL database and data-protection keys and cannot be undone without a backup.**

```powershell
docker compose down --volumes
```

Normal stopping, restarting, and updating must not use `--volumes`.
