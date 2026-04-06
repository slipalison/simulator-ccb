# Phase 6: Authentication API - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the analysis.

**Date:** 2026-04-06
**Phase:** 06-authentication-api
**Mode:** discuss (assumptions-based)
**Areas analyzed:** Login endpoint, JWT validation, Client lookup, Token service, AUTH-04 scope

## Assumptions Presented

### Login & Refresh Endpoints
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| POST /api/auth/login proxies ROPC to Keycloak, returns tokens | Confident | AUTH-02, ROADMAP Phase 6 goal |
| POST /api/auth/refresh forwards refresh_token to Keycloak | Confident | AUTH-04, ROADMAP success criteria |
| ValidateAudience = false for JwtBearer | Likely | Keycloak ROPC token aud includes "account", not API |

### Client Lookup for /api/clients/me
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| Lookup by email claim (no KeycloakUserId in aggregate) | Likely | Client.cs has no KC user ID field; email = KC username |
| Requires adding GetByEmailAsync to IClientRepository | Confident | ClientRepository.cs — method not present |
| No EF Core migration needed | Confident | Email already stored in Clients table |

### AUTH-04 Scope
| Assumption | Confidence | Evidence |
|------------|-----------|----------|
| Backend only exposes /api/auth/refresh endpoint | Confident | ROADMAP says "backend (or frontend token logic)" |
| Auto-detection of near-expiry is frontend concern (Phase 9) | Confident | ROADMAP phrasing; Phase 9 = Login UI |

## Corrections Made

### Client Lookup for /api/clients/me
- **Original assumption:** Email claim lookup recommended
- **User confirmed:** Email claim lookup (no domain change needed)

### AUTH-04 Scope
- **Original assumption:** Backend exposes endpoint only; detection is frontend
- **User confirmed:** Só expor o endpoint — frontend detecta nearness

## No Corrections — all assumptions confirmed.
