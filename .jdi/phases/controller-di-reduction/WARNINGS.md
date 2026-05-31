# Phase 55 — controller-di-reduction — WARNINGS

## W-AUTH-NONCQRS (justified gate exception)

**AuthController: `ForgotPassword` + `ResetPassword` use [FromServices] ICommandHandler directly.**

These two actions inject `ICommandHandler<ForgotPasswordCommand, Unit>` and
`ICommandHandler<ResetPasswordCommand, Unit>` via `[FromServices]` on the action parameter,
not through `ICommandDispatcher`. Reason: these handlers were already `[FromServices]` in
Phase 54 (pre-existing pattern). Moving them to the dispatcher would require the dispatcher
to dispatch by `ICommandHandler<T, Unit>` where `Unit` is ambiguous when multiple handlers
return `Unit`. The risk of routing the wrong handler exceeds the cosmetic benefit.
Gate: ctor has 3 deps (ICommandDispatcher + IValidationRunner + ILogger) — PASS.

## W-PERMISSIONS-NODISPATCHER (justified, no exception needed)

**PermissionsController: 1 dep (ICurrentCompanyPermissionsService) — no dispatcher.**

PermissionsController never had CQRS handlers injected. It reads permissions from
`ICurrentCompanyPermissionsService` set by `ClientClaimsMiddleware`. Adding dispatchers
would be YAGNI. D-63 "apply dispatcher uniformly" interpreted as "uniformly where CQRS
handlers existed before." Gate: 1 dep — PASS (below ≤5 threshold).

## W-AUTHCONTROLLER-REPO-SOLID04 (deferred from Phase 54)

`AuthController.GetMe` uses 3 repos directly (`ICompanyRepository`, `IEmployeeRepository`,
`IAccessGroupRepository`) for permission resolution, not routed via CQRS query handlers.
These are now `[FromServices]` on `GetMe` — removing them from ctor achieves the ≤5 gate.
SOLID-04 (direct repo access in controller) remains as a known debt deferred to a future
phase where a dedicated `GetPermissionsFromTokenQuery` handler could be introduced.
Documented here, subsumes W-FUNDOS-SPLIT from Phase 54 (no longer relevant — dispatcher
achieved ≤5 without split).

## W-DYNAMIC-REPLACED-BY-REFLECTION

Initial implementation used `dynamic` dispatch (`dynamic h = sp.GetService(handlerType)`).
This caused `RuntimeBinderException` in unit tests when handler types were `internal`.
Switched to `MethodInfo.Invoke` with cached reflection. Performance impact: negligible
(MethodInfo cached after first call per type pair; production handlers are always `public`
concrete classes resolved from DI — no accessibility issues in production).
