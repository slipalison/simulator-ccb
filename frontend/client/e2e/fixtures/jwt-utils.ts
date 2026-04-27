import { decodeJwt } from 'jose';
import type { Page } from '@playwright/test';

/**
 * Decoded JWT token structure with common Keycloak claims.
 */
export interface DecodedToken {
  sub: string;
  email: string;
  groups?: string[];
  realm_access?: { roles: string[] };
  company_id?: string;
  [key: string]: unknown;
}

/**
 * Decode a JWT access token without verification (for E2E test inspection only).
 * Uses jose.decodeJwt which handles Base64URL padding correctly.
 */
export function decodeAccessToken(token: string): DecodedToken {
  return decodeJwt(token) as DecodedToken;
}

/**
 * Extract the access token from Playwright's browser context cookies.
 * The client app stores tokens in httpOnly cookies named `client_access_token`.
 */
export async function getAccessTokenFromCookies(page: Page): Promise<string | null> {
  const cookies = await page.context().cookies(['http://localhost:5173']);
  const accessTokenCookie = cookies.find((c) => c.name === 'client_access_token');
  return accessTokenCookie?.value ?? null;
}