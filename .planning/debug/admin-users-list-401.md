# Debug: admin-users-list-401

**Date:** 2026-04-10
**Status:** Investigating

## Symptoms

**expected:**
- Admin logged in → navigates to /admin/users → sees paginated user list

**actual:**
- `GET http://localhost:5174/api/admin/users?page=1&pageSize=20` → **401 (Unauthorized)**

**errors:**
- 401 (Unauthorized) on listUsers endpoint

**reproduction:**
1. Login as admin at http://localhost:5174/admin/login (succeeds — 200 OK)
2. Navigate to http://localhost:5174/admin/users → 401 on GET /api/admin/users

**timeline:**
- Started: after login was fixed (admin 403 fix)
- Admin login works (200 OK), but subsequent API calls return 401

## Areas to Investigate

1. **Cookie not being sent** — The admin cookie `adminRefreshToken` with Path=/api/admin may not be reaching the backend
2. **Cookie path mismatch** — Cookie path /api/admin vs request to /api/admin/users
3. **Backend cookie reading** — Request.Cookies may not find the cookie
4. **Token refresh failure** — Cookie contains refresh token, not access token
5. **CORS preflight** — Shouldn't happen for simple GET

## Prior Context

- Admin cookie name: `adminRefreshToken`
- Cookie path: `/api/admin`
- Cookie is httpOnly, Secure=false (dev), SameSite=Strict
- Backend uses `[Authorize(Roles = "admin")]` on AdminUserController
- The cookie contains a refresh token, NOT an access token
  
## Resolution  
**Status:** ? RESOLVED 2026-04-10  
**Commit:** d635cdc (AdminSessionMiddleware) 
