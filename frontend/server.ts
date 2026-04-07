// ---------------------------------------------------------------------------
// API Proxy — forwards /api/* requests to the .NET backend
// ---------------------------------------------------------------------------
// Vinxi http router handler using h3. All requests under /api/* are
// proxied to the backend service running in the Docker network.
// ---------------------------------------------------------------------------

import { defineEventHandler, sendWebResponse, readRawBody } from "h3";

const BACKEND_URL = "http://api:8080";

export default defineEventHandler(async (event) => {
  const method = event.method ?? "GET";
  const path = event.path ?? "/";

  // The Vinxi router has base: "/api", so path arrives as "/registration".
  // The backend expects the full "/api/registration" path.
  const targetUrl = `${BACKEND_URL}/api${path}`;

  const hasBody = !["GET", "HEAD"].includes(method);

  // Node.js 22 fetch requires duplex: "half" for body requests
  const init: RequestInit & { duplex?: string } = {
    method,
    headers: event.headers as HeadersInit,
    duplex: hasBody ? "half" : undefined,
  };

  if (hasBody) {
    // Read raw body as string/buffer — no JSON parse, forward as-is
    init.body = await readRawBody(event, false);
  }

  const proxyRes = await fetch(targetUrl, init);

  // Return the backend response as-is (status, headers, body)
  return sendWebResponse(event, proxyRes);
});
