import { execFileSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import assert from "node:assert/strict";
import test from "node:test";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");

function readRequired(relativePath) {
  const path = join(root, relativePath);
  assert.equal(existsSync(path), true, `${relativePath} must exist`);
  return readFileSync(path, "utf8");
}

const trackedFiles = execFileSync("git", ["ls-files"], {
  cwd: root,
  encoding: "utf8",
})
  .split(/\r?\n/u)
  .filter(Boolean);

test("compose keeps SQL private and the application local by default", () => {
  const compose = readRequired("compose.yml");
  const dbStart = compose.indexOf("  db:");
  const appStart = compose.indexOf("  app:");
  const dbServiceBlock = compose.slice(dbStart, appStart);

  assert.ok(dbStart >= 0, "db service must exist");
  assert.ok(appStart > dbStart, "app service must follow db service");
  assert.match(dbServiceBlock, /mcr\.microsoft\.com\/mssql\/server:2022-latest/u);
  assert.match(dbServiceBlock, /sopmine_sql_data:\/var\/opt\/mssql/u);
  assert.doesNotMatch(dbServiceBlock, /^\s+ports:/mu);
  assert.match(compose, /APP_BIND_ADDRESS:-127\.0\.0\.1/u);
  assert.match(compose, /APP_PORT:-8080/u);
  assert.doesNotMatch(compose, /^\s+(cloudflared|caddy|nginx):/mu);
});

test("compose waits for SQL readiness and preserves runtime state", () => {
  const compose = readRequired("compose.yml");

  assert.match(compose, /healthcheck:/u);
  assert.match(compose, /condition:\s*service_healthy/u);
  assert.match(compose, /restart:\s*unless-stopped/gu);
  assert.match(compose, /sopmine_data_protection_keys/u);
  assert.match(compose, /read_only:\s*true/u);
  assert.match(compose, /tmpfs:/u);
});

test("compose maps every required application setting", () => {
  const compose = readRequired("compose.yml");

  for (const setting of [
    "ConnectionStrings__DefaultConnection",
    "JwtSettings__Issuer",
    "JwtSettings__Audience",
    "JwtSettings__Secret",
    "DefaultAdmin__Email",
    "DefaultAdmin__Password",
    "DefaultAdmin__Role",
    "Gemini__ApiKey",
    "Gemini__Model",
  ]) {
    assert.match(compose, new RegExp(`${setting}:`, "u"));
  }
});

test("application image publishes once and runs as non-root", () => {
  const dockerfile = readRequired("Dockerfile");

  assert.match(dockerfile, /dotnet publish[\s\S]*--no-restore/u);
  assert.match(dockerfile, /DataProtection-Keys/u);
  assert.match(dockerfile, /USER \$APP_UID/u);
  assert.match(dockerfile, /ENTRYPOINT \["dotnet", "SopmineWorkshop\.API\.dll"\]/u);
});

test("example configuration contains placeholders but no tracked secrets", () => {
  const exampleEnvironment = readRequired(".env.docker.example");

  assert.match(exampleEnvironment, /^SQL_SA_PASSWORD=REPLACE_/mu);
  assert.match(exampleEnvironment, /^JWT_SECRET=REPLACE_/mu);
  assert.match(exampleEnvironment, /^DEFAULT_ADMIN_EMAIL=REPLACE_/mu);
  assert.match(exampleEnvironment, /^DEFAULT_ADMIN_PASSWORD=REPLACE_/mu);
  assert.equal(trackedFiles.includes(".env"), false);
  assert.equal(trackedFiles.includes("src/SopmineWorkshop.API/appsettings.Development.json"), false);
});

test("Docker guide covers the complete safe lifecycle", () => {
  const guide = readRequired("docs/docker-installation.md");

  for (const requiredText of [
    "docker compose up -d --build",
    "docker compose ps",
    "docker compose logs",
    "docker compose down",
    "BACKUP DATABASE",
    "RESTORE DATABASE",
    "/health",
    "Cloudflare Tunnel",
  ]) {
    assert.match(guide, new RegExp(requiredText.replace(/[.*+?^${}()|[\]\\]/gu, "\\$&"), "u"));
  }

  const destructiveWarning = guide.indexOf("Permanently delete all application data");
  const destructiveCommand = guide.indexOf("docker compose down --volumes");
  assert.ok(destructiveWarning >= 0, "destructive-data warning must exist");
  assert.ok(
    destructiveCommand > destructiveWarning,
    "the destructive command must appear after its warning",
  );
});
