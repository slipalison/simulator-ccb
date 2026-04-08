// ---------------------------------------------------------------------------
// API Proxy — forwards /api/* requests to the .NET backend
// ---------------------------------------------------------------------------
// Vinxi http router handler using h3. All requests under /api/* are
// proxied to the backend service running in the Docker network.
// ---------------------------------------------------------------------------

import { defineEventHandler, getHeaders, readRawBody } from "h3";

const BACKEND_URL = "http://api:8080";

export default defineEventHandler(async (event) => {
  const method = event.node.req.method ?? "GET";
  const path = event.path ?? "/";

  // The Vinxi router has base: "/api", so path arrives as "/registration".
  // The backend expects the full "/api/registration" path.
  const targetUrl = `${BACKEND_URL}/api${path}`;

  const hasBody = !["GET", "HEAD", "OPTIONS"].includes(method);

  // Build headers, forwarding cookies from browser
  const headers = getHeaders(event);
  const fetchHeaders: Record<string, string> = {};
  for (const [key, value] of Object.entries(headers)) {
    if (key.toLowerCase() === "host") continue;
    if (key.toLowerCase() === "content-length") continue;
    if (typeof value === "string") {
      fetchHeaders[key] = value;
    }
  }

  const init: RequestInit & { duplex?: string } = {
    method,
    headers: fetchHeaders,
    duplex: hasBody ? "half" : undefined,
  };

  if (hasBody) {
    init.body = await readRawBody(event, false);
  }

  const proxyRes = await fetch(targetUrl, init);

  // Forward Set-Cookie headers from backend to browser
  const setCookieHeader = proxyRes.headers.get("set-cookie");
  if (setCookieHeader) {
    // Split multiple cookies if present
    const cookies = setCookieHeader.split(/,(?=\s*\w+=)/);
    for (const cookie of cookies) {
      event.node.res.appendHeader("Set-Cookie", cookie);
    }
  }

  // Copy all other headers from backend response
  for (const [key, value] of proxyRes.headers.entries()) {
    if (key.toLowerCase() === "set-cookie") continue;
    if (key.toLowerCase() === "content-length") continue;
    if (key.toLowerCase() === "transfer-encoding") continue;
    event.node.res.setHeader(key, value);
  }

  // Set status code
  event.node.res.statusCode = proxyRes.status;

  // Return body
  const body = await proxyRes.arrayBuffer();
  return Buffer.from(body);
});
