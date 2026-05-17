/**
 * PKCE utilities for Authorization Code Flow.
 * Used server-side only (Vinxi h3 auth-server) — Node.js crypto available.
 */

import { webcrypto } from "node:crypto";

const crypto = webcrypto as unknown as Crypto;

export function generateCodeVerifier(): string {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return base64UrlEncode(array);
}

export async function generateCodeChallenge(verifier: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(verifier);
  const hash = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(new Uint8Array(hash));
}

export function buildAuthorizationUrl(params: {
  keycloakUrl: string;
  realm: string;
  clientId: string;
  redirectUri: string;
  codeChallenge: string;
  state: string;
}): string {
  const { keycloakUrl, realm, clientId, redirectUri, codeChallenge, state } = params;
  const baseUrl = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/auth`;
  const search = new URLSearchParams({
    client_id: clientId,
    redirect_uri: redirectUri,
    response_type: "code",
    scope: "openid profile email",
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
    state,
  });
  return `${baseUrl}?${search.toString()}`;
}

export async function exchangeCodeForTokens(params: {
  keycloakUrl: string;
  realm: string;
  clientId: string;
  clientSecret: string;
  code: string;
  codeVerifier: string;
  redirectUri: string;
}): Promise<{ accessToken: string; refreshToken: string; idToken: string | null; expiresIn: number }> {
  const { keycloakUrl, realm, clientId, clientSecret, code, codeVerifier, redirectUri } = params;
  const tokenUrl = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`;

  const response = await fetch(tokenUrl, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "authorization_code",
      client_id: clientId,
      client_secret: clientSecret,
      code,
      code_verifier: codeVerifier,
      redirect_uri: redirectUri,
    }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`Token exchange failed: ${response.status} ${body}`);
  }

  const data = (await response.json()) as Record<string, unknown>;
  // id_token is present when scope includes "openid" (T-18 / W-SEC-IT4-1 fix).
  // Captured here so auth-server.ts can store it server-side (HttpOnly cookie)
  // and forward it as id_token_hint on logout, enabling KC to skip the
  // confirmation page and redirect directly to post_logout_redirect_uri.
  // Never written to browser storage (D-12).
  const idToken = typeof data.id_token === "string" ? data.id_token : null;
  return {
    accessToken: data.access_token as string,
    refreshToken: data.refresh_token as string,
    idToken,
    expiresIn: data.expires_in as number,
  };
}

export async function refreshAccessToken(params: {
  keycloakUrl: string;
  realm: string;
  clientId: string;
  clientSecret: string;
  refreshToken: string;
}): Promise<{ accessToken: string; refreshToken: string; expiresIn: number }> {
  const { keycloakUrl, realm, clientId, clientSecret, refreshToken: rt } = params;
  const tokenUrl = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`;

  const response = await fetch(tokenUrl, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "refresh_token",
      client_id: clientId,
      client_secret: clientSecret,
      refresh_token: rt,
    }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`Token refresh failed: ${response.status} ${body}`);
  }

  const data = (await response.json()) as Record<string, unknown>;
  return {
    accessToken: data.access_token as string,
    refreshToken: (data.refresh_token as string) ?? rt,
    expiresIn: data.expires_in as number,
  };
}

function base64UrlEncode(input: Uint8Array): string {
  return btoa(String.fromCharCode(...input))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
}
