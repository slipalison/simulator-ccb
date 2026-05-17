# Contributing Guidelines

## Development Setup

### Prerequisites

- .NET 10 SDK
- Node.js 22+ (for frontend)
- Docker Desktop (for local infrastructure)
- Python 3.10+ (for Semgrep)

### Local Development

**IMPORTANT:** Do not run `pnpm dev` / `npm run dev` on the host for the frontend SPAs.
Doing so creates a Vinxi process that conflicts with the docker compose port mapping and causes
503 errors on all `/api/*` routes. See [docs/dev-setup.md](./docs/dev-setup.md) for the full
explanation (D-16).

```bash
# 1. Copy env template and fill secrets
cp .env.example .env

# 2. Start the full stack — frontend SPAs run inside compose with hot reload via bind mounts
docker compose up -d

# 3. Verify frontend proxy is reachable
curl http://127.0.0.1:5173/api/healthz/live
# Expected: Healthy

# Backend (optional — already running in compose; start separately only for debugger attach)
dotnet restore Onboarding.slnx
dotnet run --project src/Onboarding.API
```

A `predev` guard in both `frontend/client/package.json` and `frontend/backoffice/package.json`
will abort `pnpm dev` if the compose service is already running. Use `ALLOW_HOST_DEV=1 pnpm dev`
only when you explicitly need a host-side debugger (see [docs/dev-setup.md](./docs/dev-setup.md)).

## Code Quality

### Linting & Type Checking

```bash
# Frontend Client
cd frontend/client
npm run lint        # ESLint --max-warnings 0
npm run typecheck   # tsc --noEmit

# Frontend Backoffice
cd frontend/backoffice
npm run lint
npm run typecheck
```

### Tests

```bash
# All tests with coverage
dotnet test Onboarding.slnx --configuration Release /p:CollectCoverage=true

# Domain tests only
dotnet test tests/Onboarding.Domain.Tests --configuration Release

# API tests only
dotnet test tests/Onboarding.API.Tests --configuration Release
```

Coverage threshold: **80% line coverage** (enforced via `coverlet.msbuild`).

## Security

See [docs/security-overview.md](docs/security-overview.md) for the complete security documentation index.

Key documents:
- [Security Policy](.github/SECURITY.md) — Vulnerability reporting
- [Security Runbook](docs/security-runbook.md) — Alert response procedures
- [Secrets Incident Response](docs/secrets-incident-response.md) — Secret revocation
- [Compliance Mapping](docs/compliance-mapping.md) — OWASP/LGPD/CIS alignment
- [Audit Checklist](docs/security-audit-checklist.md) — PR review + onboarding

### Running Semgrep Locally

```bash
# Install (requires Python 3.10+)
pip install semgrep

# Run custom rules only (fast)
semgrep scan --config .semgrep/ --error --metrics off .

# Run custom + registry rules (comprehensive, ~1-2 min)
semgrep scan --config auto --config .semrep/ --error .
```

**Interpreting Results:**
- **ERROR**: Must fix before merging. Blocks CI.
- **WARNING**: Review recommended. Posted to GitHub Security Tab.
- **Suppression**: Use `// nosem: rule-id` with a justification comment:
  ```csharp
  // nosem: no-missing-csrf — Stateless JWT API, CSRF not applicable (no session cookies)
  [HttpPost]
  public IActionResult Webhook(...) { ... }
  ```

### Running Gitleaks Locally (Secrets Detection)

```bash
# Install
brew install gitleaks  # macOS
winget install gitleaks  # Windows

# Scan current directory
gitleaks detect --config .gitleaks.toml --source . --verbose

# Scan full git history
gitleaks detect --config .gitleaks.toml --source . --log-opts="--all" --verbose

# Pre-commit hook (add to .git/hooks/pre-commit)
#!/bin/sh
gitleaks detect --config .gitleaks.toml --source . --staged --quiet
```

**Every commit is scanned for secrets.** If a secret is detected:
1. CI job fails with Gitleaks/TruffleHog error
2. Secret must be revoked immediately (see `docs/secrets-incident-response.md`)
3. Code history must be cleaned if secret was merged to main

### Suppressing Findings

Every `// nosem` comment **MUST** include a reason. Examples:

```csharp
// nosem: no-missing-csrf — Public webhook endpoint, no auth required
// nosem: no-hardcoded-credentials — Test fixture with mock connection string
```

```typescript
// nosem: no-localstorage-tokens — Theme preference, not auth token
localStorage.setItem('theme', 'dark');
```

### Running CodeQL Locally (Advanced)

```bash
# Install GitHub CLI + CodeQL extension
gh extension install github/gh-codeql

# Create database (C# only)
gh codeql database create test-db --language=csharp --command="dotnet build Onboarding.slnx"

# Analyze
gh codeql database analyze test-db github-security-extended --format=sarifv2 --output=codeql.sarif
```

## Commit Messages

Follow conventional commits:

```
feat: add user block dialog
fix: resolve CSRF false positive in webhook controller
docs: update security runbook
chore: update npm dependencies
test: add admin user deletion E2E test
```

## Pull Request Process

1. Create feature branch from `main`
2. Make changes with tests
3. Run local checks: `dotnet test`, `npm run lint`, `npm run typecheck`, `semgrep scan`
4. Open PR — template checklist must be completed
5. Address review feedback
6. Merge after CI passes (all 5 jobs: backend, frontend-client, frontend-backoffice, SAST Semgrep, SAST CodeQL)

## Branch Strategy

- `main` — protected branch, requires all CI checks passing
- Feature branches — prefix with `feature/`, `fix/`, `chore/`
- No direct commits to `main` — all changes via PR
