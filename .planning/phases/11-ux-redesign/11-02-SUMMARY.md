# Phase 11 Plan 02 — Forgot Password Flow (Resend.com) — SUMMARY

## Status: COMPLETE

## Commits

| Commit | Hash | Description |
|--------|------|-------------|
| 11.2.1 | `9c0cfeb` | test(11-02): add RED stub tests for forgot/reset password flow (14 tests) |
| 11.2.2 | `1fb6520` | feat(11-02): implement forgot/reset password backend with Resend.com integration |
| 11.2.3 | `dddde80` | feat(11-02): add forgot/reset password frontend pages and routes |

## Test Results

### Backend (51 passed, 2 skipped, 0 failed)
- `ForgotPasswordEndpointTests` — 4 tests (existing email, non-existing email, invalid email, rate limited)
- `ResetPasswordEndpointTests` — 5 tests (valid token, expired token, invalid token, weak password, after success)

### Frontend (72 passed, 1 pre-existing failure)
- `forgot-password.test.tsx` — 3 tests (renders, success message, no info disclosure)
- `reset-password.test.tsx` — 5 tests (renders, strength meter, passwords match, redirect, expired token)

## Files Created

### Backend
- `src/Onboarding.Domain/Aggregates/PasswordReset/PasswordResetToken.cs` — Entity with factory method
- `src/Onboarding.Domain/Repositories/IPasswordResetTokenRepository.cs` — Repository interface
- `src/Onboarding.Domain/Exceptions/RateLimitExceededException.cs` — Rate limit exception
- `src/Onboarding.Application/Auth/Commands/ForgotPasswordCommand.cs` — Command + handler
- `src/Onboarding.Application/Auth/Commands/ResetPasswordCommand.cs` — Command + handler
- `src/Onboarding.Application/Auth/Validators/ForgotPasswordCommandValidator.cs` — Email validation
- `src/Onboarding.Application/Auth/Validators/ResetPasswordCommandValidator.cs` — Token/password validation
- `src/Onboarding.Application/Services/IEmailService.cs` — Email service interface
- `src/Onboarding.Application/Common/BadRequestException.cs` — Application-level exception
- `src/Onboarding.Infrastructure/Services/ResendEmailService.cs` — Resend.com implementation
- `src/Onboarding.Infrastructure/Repositories/PasswordResetTokenRepository.cs` — EF Core repository
- `src/Onboarding.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs` — EF configuration
- `src/Onboarding.Infrastructure/Persistence/Migrations/20260408182041_AddPasswordResetTokens.cs` — Migration

### Frontend
- `frontend/src/components/pages/ForgotPasswordPage.tsx` — Forgot password form
- `frontend/src/components/pages/ResetPasswordPage.tsx` — Reset password form with strength meter
- `frontend/src/tests/forgot-password.test.tsx` — 3 UI tests
- `frontend/src/tests/reset-password.test.tsx` — 5 UI tests

## Files Modified

- `src/Onboarding.Application/Common/IKeycloakUserService.cs` — Added `UserExistsByEmailAsync`, `GetUserByEmailAsync`, `UpdateUserPasswordAsync` + `KeycloakUser` record
- `src/Onboarding.Application/DependencyInjection.cs` — Registered forgot/reset command handlers + validators
- `src/Onboarding.Infrastructure/Keycloak/KeycloakUserService.cs` — Implemented 3 new methods
- `src/Onboarding.Infrastructure/DependencyInjection.cs` — Registered `IEmailService` + `IPasswordResetTokenRepository`
- `src/Onboarding.Infrastructure/Persistence/AppDbContext.cs` — Added `PasswordResetTokens` DbSet
- `src/Onboarding.API/Controllers/AuthController.cs` — Added `POST /api/auth/forgot-password` and `POST /api/auth/reset-password` endpoints
- `frontend/src/lib/api.ts` — Added `forgotPasswordClient`, `resetPasswordClient`, `ForgotPasswordError`, `ResetPasswordError`
- `frontend/src/router.tsx` — Added `/forgot-password` and `/reset-password` routes
- `tests/Onboarding.API.Tests/Authentication/AuthTestApiFactory.cs` — Added mocks for `IKeycloakUserService`, `IPasswordResetTokenRepository`, `IEmailService`

## Security Implementation

- **No info disclosure**: Forgot password returns same 200 response whether email exists or not
- **Rate limiting**: Max 3 reset requests per hour per email (returns 429)
- **Single-use tokens**: Tokens marked as used after successful reset
- **Token expiry**: 15-minute expiration
- **Password policy**: Min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char

## Next Steps

- Configure `RESEND_API_KEY` in `.env` for production email delivery
- Update `Email:FromAddress` configuration with actual domain
- Consider adding email template customization
