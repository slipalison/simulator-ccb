# Secrets Incident Response

Procedures for handling secrets that are detected in the codebase by automated scanning tools (Gitleaks, TruffleHog).

## Detection

Secrets are detected through two CI pipeline jobs:

| Tool | Method | Trigger |
|------|--------|---------|
| **Gitleaks** | Pattern matching against known secret formats | Every push/PR |
| **TruffleHog** | Pattern matching + active verification (attempts to authenticate) | Every push/PR |

When a secret is detected:
1. The CI job fails with details in the GitHub Security Tab → Secret scanning alerts
2. The PR merge is blocked until the issue is resolved
3. A finding is created with: file path, line number, secret type, severity

## Immediate Response (0-1 hour)

### Step 1: Identify the Secret

Determine the type of secret exposed:

- **Database password** — Connection string with `Password=` or `Pwd=`
- **Keycloak client secret** — `client_secret` value for admin or public client
- **JWT signing key** — Secret used to sign/verify JWT tokens
- **API key** — Third-party service key (SMTP, cloud provider, etc.)
- **Encryption key** — Key used for data encryption at rest

### Step 2: Revoke Immediately

**DO NOT** just remove the secret from code. The secret must be considered **compromised** and revoked.

| Secret Type | Revocation Method |
|-------------|-------------------|
| Database password | `ALTER USER appuser WITH PASSWORD 'new-password';` in PostgreSQL |
| Keycloak client secret | Keycloak Admin Console → Clients → onboarding-api-admin → Credentials → Regenerate |
| JWT signing key | Generate new key, update `appsettings.json` or environment variable |
| API key | Provider console (AWS, Azure, SendGrid, etc.) → Revoke key |

### Step 3: Document the Finding

Record the incident details:

```
Date: YYYY-MM-DD
File: path/to/file.ext
Commit: abc123def
Secret Type: [database/keycloak/jwt/api]
Severity: [CRITICAL/HIGH/MEDIUM]
Action Taken: [revoked/rotated/removed]
```

## Rotation Procedure (1-24 hours)

### Database Passwords

```bash
# 1. Generate new password (use a password manager)
NEW_PASSWORD=$(openssl rand -base64 32)

# 2. Update PostgreSQL
docker compose exec app_db psql -U appuser -d onboarding -c \
  "ALTER USER appuser WITH PASSWORD '${NEW_PASSWORD}';"

# 3. Update .env file (never commit this file)
echo "APP_DB_PASSWORD=${NEW_PASSWORD}" >> .env

# 4. Restart application to pick up new password
docker compose restart app
```

### Keycloak Client Secrets

```bash
# 1. Generate new client secret
NEW_SECRET=$(openssl rand -base64 32)

# 2. Update Keycloak via Admin API or Console
# Admin Console: Clients → onboarding-api-admin → Credentials → Regenerate

# 3. Update application configuration
# Update appsettings.json or environment variable with new secret
```

### JWT Signing Keys

```bash
# 1. Generate new signing key
NEW_KEY=$(openssl rand -base64 64)

# 2. Update API configuration
# Update Jwt__Key environment variable or appsettings.json

# 3. Redeploy API — all existing tokens become invalid
# Users will need to re-authenticate
```

### API Keys (Third-Party)

1. Log into provider console (AWS, Azure, SendGrid, etc.)
2. Navigate to API keys / credentials section
3. Create new key
4. Update application configuration
5. Revoke old key

## Post-Incident (24-48 hours)

### Root Cause Analysis

Answer these questions:

1. **How was the secret committed?**
   - Accidental commit in feature branch?
   - Copied from local `.env` to source code?
   - Hardcoded in test fixture?
   - Merged without CI check?

2. **Why didn't pre-commit hooks catch it?**
   - Developer skipped hooks?
   - Rule not configured?
   - Hook not installed?

3. **What was the exposure window?**
   - Time from commit to detection
   - Was the code pushed to `main`?
   - Were any external systems exposed?

### Prevention

- Add a new Gitleaks rule if the pattern was not caught
- Ensure pre-commit hooks are installed (`CONTRIBUTING.md` instructions)
- Consider adding a `git-secrets` or `husky` pre-commit hook for local enforcement
- Team notification with lessons learned

### Cleanup

- Remove the secret from git history if committed (use `git filter-branch` or BFG Repo-Cleaner)
- Update `.gitleaksignore` ONLY if the finding was a confirmed false positive
- Close the GitHub security alert with appropriate resolution status

## Escalation Path

| Level | Who | When |
|-------|-----|------|
| **Level 1** | Developer who committed the secret | Immediate — they should fix their own mistake |
| **Level 2** | Tech lead / security champion | If developer is unavailable or secret type is unclear |
| **Level 3** | CTO / security team | If production credentials were exposed publicly |

### When to Escalate to Level 3

- Production database credentials committed to public repository
- Keycloak admin secret exposed in public code
- JWT signing key leaked (all user sessions compromised)
- Any secret that could result in data breach or regulatory impact (LGPD/GDPR)

## Tools

### Running Gitleaks Locally

```bash
# Install
brew install gitleaks  # macOS
# or
winget install gitleaks  # Windows

# Scan current directory
gitleaks detect --config .gitleaks.toml --source . --verbose

# Scan full git history
gitleaks detect --config .gitleaks.toml --source . --log-opts="--all" --verbose
```

### Running TruffleHog Locally

```bash
# Install
brew install trufflehog  # macOS
# or
docker run --rm -v $(pwd):/code trufflesecurity/trufflehog filesystem /code --only-verified

# Scan directory
trufflehog filesystem --directory . --only-verified --fail
```

### Adding Suppressions

Every suppression in `.gitleaksignore` **MUST** include a justification:

```
# Fingerprint format: sha256 of the secret + file path
abc123...  # Test fixture: mock DB password in RegistrationIntegrationTests.cs
def456...  # Documentation example in README.md (not a real credential)
```
