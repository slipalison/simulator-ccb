# Specialists routing (per-project doers)

Multi-stack project — 3 specialist pairs. Routing by file glob; cross-cutting security triggers on glob OR auto-trigger keywords.

| Stack | Agent | File glob | Trigger |
|---|---|---|---|
| Backend C# (.NET 10 + EF Core + Keycloak) | jdi-doer-onboarding-keycloak-backend-csharp | `**/*.{cs,csproj,sln,slnx}` | executor for .NET source/tests/projects |
| Frontend React + Vinxi→Vinext migration target | jdi-doer-onboarding-keycloak-frontend-vinext | `frontend/**/*.{ts,tsx,jsx,js,css,scss,html,mjs,cjs}` | executor for both SPA projects (client + backoffice) |
| Security (cross-cutting) | jdi-doer-onboarding-keycloak-security | `{.github/workflows/**,.semgrep/**,Dockerfile*,docker-compose*.yml,infra/**,keycloak/**,**/Security/**,**/Permission*,**/Auth*,**/.env*}` | security-adjacent files OR keywords: security/CVE/vulnerability/secret/token/keycloak/hardening/semgrep/codeql/trivy/zap/container/SAST/DAST |

## Notes
- Glob overlap: backend `**/*.cs` overlaps security `**/Security/**`. Resolution: security specialist takes precedence on files matching its glob (security-routed first). Backend specialist still ALSO loaded for context but writes go through security path.
- Frontend specialist owns both `frontend/client/` and `frontend/backoffice/` but D-4 forbids any cross-imports.
- Security is the only specialist with auto-trigger keywords (cross-cutting). Backend/frontend trigger purely on glob.
