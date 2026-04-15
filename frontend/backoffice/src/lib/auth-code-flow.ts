import { randomBytes, createHash } from "node:crypto";

export function generateCodeVerifier(): string {
  return randomBytes(32).toString("base64url");
}

export function generateCodeChallenge(verifier: string): string {
  return createHash("sha256").update(verifier).digest("base64url");
}

export interface BuildAuthUrlParams {
  clientId: string;
  redirectUri: string;
  keycloakUrl: string;
  realm: string;
  state: string;
  codeChallenge: string;
}

export function buildAuthorizationUrl(params: BuildAuthUrlParams): string {
  const { clientId, redirectUri, keycloakUrl, realm, state, codeChallenge } = params;
  const authEndpoint = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/auth`;
  const searchParams = new URLSearchParams({
    response_type: "code",
    client_id: clientId,
    redirect_uri: redirectUri,
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
    state,
    scope: "openid email profile offline_access",
  });
  return `${authEndpoint}?${searchParams.toString()}`;
}

export interface ExchangeCodeParams {
  code: string;
  codeVerifier: string;
  clientId: string;
  clientSecret: string;
  redirectUri: string;
  keycloakUrl: string;
  realm: string;
}

export interface TokenResponse {
  access_token: string;
  refresh_token: string;
  id_token: string;
  expires_in: number;
  refresh_expires_in: number;
  token_type: string;
}

export async function exchangeCodeForTokens(params: ExchangeCodeParams): Promise<TokenResponse> {
  const { code, codeVerifier, clientId, clientSecret, redirectUri, keycloakUrl, realm } = params;
  const tokenEndpoint = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`;

  const body = new URLSearchParams({
    grant_type: "authorization_code",
    code,
    redirect_uri: redirectUri,
    client_id: clientId,
    client_secret: clientSecret,
    code_verifier: codeVerifier,
  });

  const response = await fetch(tokenEndpoint, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Token exchange failed: ${response.status} ${error}`);
  }

  return response.json() as Promise<TokenResponse>;
}

export interface RefreshTokenParams {
  refreshToken: string;
  clientId: string;
  clientSecret: string;
  keycloakUrl: string;
  realm: string;
}

export async function refreshAccessToken(params: RefreshTokenParams): Promise<TokenResponse> {
  const { refreshToken, clientId, clientSecret, keycloakUrl, realm } = params;
  const tokenEndpoint = `${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`;

  const body = new URLSearchParams({
    grant_type: "refresh_token",
    refresh_token: refreshToken,
    client_id: clientId,
    client_secret: clientSecret,
  });

  const response = await fetch(tokenEndpoint, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Token refresh failed: ${response.status} ${error}`);
  }

  return response.json() as Promise<TokenResponse>;
}

export function decodeJwtPayload(token: string): Record<string, unknown> {
  const parts = token.split(".");
  if (parts.length !== 3) {
    throw new Error("Invalid JWT format");
  }
  const payload = Buffer.from(parts[1], "base64url").toString("utf-8");
  return JSON.parse(payload) as Record<string, unknown>;
}
