# Contributing Guidelines

## Development Setup

### Prerequisites

- .NET 10 SDK
- Node.js 22+ (for frontend)
- Docker Desktop (for local infrastructure)
- Python 3.10+ (for Semgrep)

### Local Development

```bash
# Start infrastructure (PostgreSQL + Keycloak)
docker compose up -d

# Backend
dotnet restore Onboarding.slnx
dotnet run --project src/Onboarding.API

# Frontend Client
cd frontend/client && npm ci && npm run dev

# Frontend Backoffice
cd frontend/backoffice && npm ci && npm run dev
```

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

## Security Scanning (SAST)

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
