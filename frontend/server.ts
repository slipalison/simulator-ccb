// ---------------------------------------------------------------------------
// API Proxy — forwards /api/* requests to the .NET backend
// ---------------------------------------------------------------------------
// Vinxi http router handler using h3. All requests under /api/* are
// proxied to the backend service running in the Docker network.
// ---------------------------------------------------------------------------

import { defineEventHandler, sendProxy } from "h3";

const BACKEND_URL = "http://api:8080";

export default defineEventHandler(async (event) => {
  const path = event.path ?? "/";
  const targetUrl = `${BACKEND_URL}/api${path}`;

  // sendProxy automatically handles:
  // - Cookie forwarding (browser → backend)
  // - Set-Cookie forwarding (backend → browser)
  // - All headers and body
  return sendProxy(event, targetUrl, {
    fetch: globalThis.fetch,
  });
});
