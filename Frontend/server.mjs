import { createReadStream, existsSync, readFileSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { networkInterfaces } from "node:os";
import { extname, join, normalize } from "node:path";
import { fileURLToPath } from "node:url";
import { legacyRedirect } from "./route-redirects.mjs";

const root = fileURLToPath(new URL(".", import.meta.url));
const port = Number(process.env.PORT || 5510);
const apiPort = Number(process.env.API_PORT || 5269);
const host = process.env.HOST || "0.0.0.0";
const lanIp =
  process.env.LAN_IP ||
  Object.values(networkInterfaces())
    .flat()
    .find((address) => address?.family === "IPv4" && !address.internal)?.address ||
  "localhost";
const apiOrigin = process.env.API_ORIGIN || `http://127.0.0.1:${apiPort}`;

const contentTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".png": "image/png",
  ".svg": "image/svg+xml",
  ".webp": "image/webp",
  ".woff2": "font/woff2",
};

function resolvePath(url) {
  const pathname = decodeURIComponent(new URL(url, `http://localhost:${port}`).pathname);
  const requestedPath = normalize(join(root, pathname));

  if (!requestedPath.startsWith(root)) {
    return null;
  }

  if (!existsSync(requestedPath)) {
    return null;
  }

  return statSync(requestedPath).isDirectory()
    ? join(requestedPath, "index.html")
    : requestedPath;
}

createServer((request, response) => {
  const redirect = legacyRedirect(request.url ?? "/");
  if (redirect) {
    response.writeHead(302, { Location: redirect, "Cache-Control": "no-store" });
    response.end();
    return;
  }

  const filePath = resolvePath(request.url ?? "/") || join(root, "index.html");
  const type = contentTypes[extname(filePath)] || "application/octet-stream";

  response.writeHead(200, {
    "Content-Type": type,
    "Cache-Control": "no-store, max-age=0",
  });

  if (filePath.endsWith(join("shared", "js", "runtime-config.js"))) {
    response.end(
      readFileSync(filePath, "utf8").replace(
        'apiOrigin: "",',
        `apiOrigin: ${JSON.stringify(apiOrigin)},`,
      ),
    );
    return;
  }

  createReadStream(filePath).pipe(response);
}).listen(port, host, () => {
  console.log(`Sopmine Frontend running at http://${lanIp}:${port}/`);
  console.log(`API origin rewritten to ${apiOrigin}`);
});
