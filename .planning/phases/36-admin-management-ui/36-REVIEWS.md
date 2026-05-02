---
phase: 36
reviewers: [claude]
reviewed_at: 2026-04-25T00:00:00Z
plans_reviewed: [36-01-PLAN.md, 36-02-PLAN.md, 36-03-PLAN.md, 36-04-PLAN.md]
skipped:
  gemini: "No API key configured"
  codex: "CLI not found on PATH"
  qwen: "402 insufficient credits (OpenRouter)"
  cursor: "CLI not found on PATH"
  opencode: "Self-cli — skipped for independence"
---

# Cross-AI Plan Review — Phase 36

## Claude Review

### Summary

The 4 plans cover the complete UI cycle with adequate separation of responsibilities: contracts/infra → table/dropdown → dialogs → page integration. The overall architecture is solid — discriminated union for dialog state, self-detection via JWT sub, API-call-before-open for temporary password, and auto-refetch post-action are all defensive and correct choices. The main risk lies in error typing gaps, a leak of unknown states in badges, and the loading experience during password reset.

### Strengths

- Zod schema with real constraints (min2/max100, RFC email) avoids duplicate validation in dialogs
- Adding `adminId` to `AdminAuthContext` via `/auth/me` is the correct location — avoids JWT parsing in components
- Honest threat model about `adminId` not being a secret
- 5 explicit visual states (skeleton, refetch opacity-60, error+retry, empty, data) covers the full cycle
- `pointer-events-none` during refetch prevents double-submit
- `scope="col"` in table is a relevant accessibility correction
- `ResetPasswordDialog` with readOnly monospace + copy button + discard alert covers D-04 and D-05 completely
- `DeactivateAdminDialog` with `variant=destructive` signals irreversibility without additional text
- Discriminated union for `DialogState` eliminates boolean proliferation
- `handleOpenResetPassword` calls API before opening dialog — avoids expired password appearing in modal
- Explicit note about "AdminSearchBar has internal debounce → no duplication" prevents a classic bug

### Concerns

- **HIGH** — Error response types not defined. `updateAdministrator`, `toggleAdministratorStatus` need to throw (or return) typed errors so Plan 36-04 can distinguish `409 CONFLICT` (duplicate email / last admin) from `400`, `401`, `403`. Without this, `handleDeactivate` logic is fragile.
- **HIGH** — Badges `Pendente` and `Definida` do not exist in requirements MGMT-01..06 or decisions D-01..D-15. Backend Phase 35 only operates with `enabled: true/false`. These statuses are phantom scope — either came from another feature or represent an undocumented mapping. Need removal or explicit documentation.
- **HIGH** — `handleOpenResetPassword` calls API before opening dialog but no loading state mentioned during that call. User clicks "Resetar senha" and UI appears static for an indeterminate time. Need loading state on dropdown item or button.
- **HIGH** — `handleDeactivate: 400/409 → specific toast for SEC-05` — plan is not definitive about which code the backend returns for "last admin". If Phase 35 uses `400` with body `{ code: "LAST_ADMIN" }` and frontend only tests HTTP status, any other `400` shows wrong message. Needs matching by `code` in body.
- **MEDIUM** — `/auth/me` assumed existing but no explicit reference to Phase 35 contract. If returned field is `id` not `sub`, D-08 comparison breaks silently.
- **MEDIUM** — `useAdminAuth().admin.adminId === admin.id` comparison is sensitive to format (UUID with/without hyphens, case). If JWT `sub` and endpoint `id` come in different formats, self-detection fails silently and SEC-01 is not enforced on client.
- **MEDIUM** — No explicit interface between Plan 36-04 and EditAdminDialog for re-thrown errors. When `handleSaveEdit` re-throws, how does the dialog receive and display the error? No `serverError?: string` prop defined.
- **MEDIUM** — No `401` handling mentioned during operations (5 min token lifespan). `BearerBackoffice` interceptor needs to be confirmed for all 4 calls — or plan needs to confirm Phase 33/34 already covers this.
- **MEDIUM** — Auto-refetch after successful action (D-15): if deactivated admin was on last page with only 1 item, refetch returns empty page. No logic for "go back one page" in this scenario.
- **MEDIUM** — None of the 4 dialogs have explicit `autofocus`. RHF focuses on first field with error, but on initial opening focus may stay on trigger button, breaking keyboard accessibility.
- **LOW** — No `AbortController` or timeout handling for API calls. With debounce on filters and 5 min token lifespan, race conditions are possible.
- **LOW** — No `aria-label` or `aria-haspopup` on dropdown `⋯` button. Screen reader users won't know what the button does.
- **LOW** — `ResetPasswordDialog` no `aria-live="polite"` for "Copiado!" copy feedback.
- **LOW** — No `ErrorBoundary` mentioned for page component. Unexpected render errors will crash the entire admin page.

### Suggestions

- Define `ApiError` type with `status: number` and `code?: string` and ensure all 4 functions throw this type instead of generic `Error`
- Export `AUTH_ME_RESPONSE_SCHEMA` (Zod) that validates `adminId` field and fails with clear message if absent
- Remove `Pendente`/`Definida` badges or open explicit decision (D-16) with source and mapping
- Normalize `adminId` to lowercase before comparison: `adminId.toLowerCase() === admin.id.toLowerCase()`
- Define `serverError?: string` prop on `EditAdminDialog` and render error below form before buttons
- Add `autoFocus` on first field of `EditAdminDialog` on opening
- Add `isResettingPassword: boolean` state and apply `disabled` + spinner on dropdown item during reset call
- Match "last admin" error by `error.code === 'LAST_ADMIN'` (or equivalent defined in Plan 36-01) instead of `status === 400`
- Add logic: `if (data.items.length === 0 && page > 1) setPage(p => p - 1)` after successful deactivate

### Risk Assessment

**Risk: MEDIUM**

The plans cover the happy path and most documented edge cases well. The principal risk is concentrated in three points: (1) lack of error typing in Plan 36-01 which may cause Plan 36-04 error handling to fail silently; (2) `Pendente`/`Definida` badges in Plan 36-02 indicating scope misaligned with backend; and (3) absent loading UX during pre-dialog password reset. None of these block execution, but the first two need resolution before implementation begins to avoid rework.

**Before executing, validate:**
1. Confirm with Phase 35 the exact code/body for "last admin" error.
2. Formally decide the fate of `Pendente`/`Definida` badges (remove or document).
3. Add typed `ApiError` to Plan 36-01 as prerequisite for the other 3 plans.

---

## Consensus Summary

*Only one reviewer (Claude) produced substantive output. Other CLIs were unavailable (no API key, not installed, insufficient credits). Consensus is based on Claude's review alone.*

### Agreed Strengths

- Discriminated union for dialog state — clean, type-safe pattern
- API-call-before-open for password reset — defensive and correct
- 5 explicit visual states in table — no implicit states
- Self-detection via JWT sub + disabled dropdown + tooltip — complete SEC-01 client-side UX
- Reuse of existing components (AdminSearchBar, AdminPagination, AdminStatusFilter)

### Agreed Concerns

1. **Error types undefined** — Plan 36-01 lacks typed error responses; downstream plans rely on `AdminApiError.status` which is insufficient for distinguishing semantic errors like "last admin" vs "self-edit"
2. **Pendente/Definida badges** — Appear in Plan 36-02 without requirement backing or backend contract; may be phantom scope from an earlier milestone
3. **No loading state during password reset pre-dialog call** — User sees no feedback between clicking "Resetar senha" and the dialog appearing
4. **adminId format mismatch risk** — UUID comparison may fail silently if JWT `sub` and endpoint `id` use different formats

### Divergent Views

*Single reviewer — no divergent views to report.*