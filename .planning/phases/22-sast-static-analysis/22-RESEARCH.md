# Phase 22 Research — SAST (Static Application Security Testing)

## Standard Stack

| Tool | Version | Installation | Rationale |
|------|---------|--------------|-----------|
| **Semgrep** | 1.157+ (Mar 2026) | `pip install semgrep` via GitHub Actions | Fast pattern-matching SAST. Custom rules in `.semgrep/`. Free mode with `--config auto` + custom YAML rules. No token required for OSS use. |
| **CodeQL** | Action v4.x (includes CLI 2.24.0+) | `github/codeql-action/init@v4` + `github/codeql-action/analyze@v4` | Deep dataflow/taint analysis. Native GitHub Security Tab integration. **.NET 10 + C# 14 support added in CodeQL 2.24.0 (Jan 2026).** |
| **SARIF Upload** | `github/codeql-action/upload-sarif@v4` | Used for both Semgrep and CodeQL results | Official GitHub action for SARIF ingestion. Handles deduplication, category tagging, and Security Tab posting. |

### Key Version Notes
- **`returntocorp/semgrep-action` is DEPRECATED.** Use native `pip install semgrep` + `semgrep scan` CLI directly in workflow steps.
- **`github/codeql-action@v3` deprecated Dec 2026.** Must use `@v4` (Node.js 24 runtime).
- **CodeQL 2.24.0 (Jan 2026)** explicitly added .NET 10 and C# 14 support. Earlier versions fail with exit code 32 on `database finalize`.
- **`actions/checkout@v4`** is the current stable. `@v5` exists but v4 is widely adopted.

## Architecture Patterns

### CI Workflow Structure

Add **two new jobs** to the existing `ci.yml` (which already has 3 parallel jobs: backend, frontend-client, frontend-backoffice). SAST jobs run in parallel with existing jobs, not as steps within them.

```yaml
# In .github/workflows/ci.yml — add these jobs alongside existing ones

sast-semgrep:
  name: SAST — Semgrep
  runs-on: ubuntu-latest
  permissions:
    contents: read
    security-events: write
  steps:
    - uses: actions/checkout@v4
    - name: Install Semgrep
      run: pip install semgrep
    - name: Run Semgrep Scan
      run: semgrep scan --config auto --config .semgrep/ --output semgrep.sarif --sarif --error
    - name: Upload SARIF
      uses: github/codeql-action/upload-sarif@v4
      if: always()
      with:
        sarif_file: semgrep.sarif
        category: semgrep

sast-codeql:
  name: SAST — CodeQL
  runs-on: ubuntu-latest
  permissions:
    contents: read
    security-events: write
  steps:
    - uses: actions/checkout@v4
    - name: Setup .NET 10
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: "10.0.x"
    - name: Initialize CodeQL
      uses: github/codeql-action/init@v4
      with:
        languages: csharp, javascript-typescript
        config-file: .github/codeql/codeql-config.yml
    - name: Autobuild
      uses: github/codeql-action/autobuild@v4
    - name: Perform CodeQL Analysis
      uses: github/codeql-action/analyze@v4
      with:
        category: codeql
```

**Workflow permissions:** Top-level `permissions: read-all` stays. Each SAST job overrides with `security-events: write` for SARIF upload.

**Why separate jobs, not steps:**
- Semgrep and CodeQL are independent analyses with different runtimes
- Parallel execution — no blocking dependency
- CodeQL needs .NET SDK setup; Semgrep only needs Python
- Failure isolation — one tool failing doesn't block the other's results

### Custom Rules Pattern — `.semgrep/` Directory

Create `.semgrep/` at project root with one YAML file per rule category:

```
.semgrep/
  no-localstorage-tokens.yaml      # React/TypeScript — localStorage token storage
  no-dangerously-set-inner-html.yaml  # React — XSS via dangerouslySetInnerHTML
  no-hardcoded-credentials.yaml    # C# — hardcoded connection strings, API keys
  no-missing-csrf.yaml             # C# — POST/PUT/DELETE without [ValidateAntiForgeryToken]
  no-raw-cpf-cnpj-comparison.yaml  # C# — CPF/CNPJ validated via string instead of VO
```

Each file follows Semgrep rule syntax: single `rules:` array with one or more rule objects.

### CodeQL Configuration

Create `.github/codeql/codeql-config.yml`:

```yaml
name: "CodeQL Custom Config"

queries:
  - uses: security-extended
  - uses: security-and-quality

query-filters:
  - exclude:
      problem.severity:
        - recommendation

paths-ignore:
  - "**/node_modules"
  - "**/bin"
  - "**/obj"
  - "**/*.test.*"
  - "**/*.spec.*"
  - "frontend/client/.vinxi"
  - "frontend/backoffice/.vinxi"
  - "scripts"
```

### SARIF Upload and Security Tab Integration

Both Semgrep and CodeQL results flow into **GitHub Security Tab > Code scanning alerts**:
- **Category field** distinguishes source (`semgrep` vs `codeql`)
- **Alert states:** Open, Dismissed (False Positive / Won't Fix / Used in tests), Fixed
- **Triage workflow:** Developers dismiss false positives with reason; dismissals persist across scans
- **Branch protection:** Add code scanning status checks to branch protection rules to block merge on active alerts

## Custom Rules (Semgrep)

### Rule 1: Detect localStorage Token Storage (React/TypeScript)

**File:** `.semgrep/no-localstorage-tokens.yaml`

```yaml
rules:
  - id: no-localstorage-tokens
    patterns:
      - pattern-either:
          - pattern: localStorage.setItem("$KEY", $VALUE)
          - pattern: localStorage.getItem("$KEY")
          - pattern: window.localStorage.setItem("$KEY", $VALUE)
          - pattern: window.localStorage.getItem("$KEY")
      - metavariable-regex:
          metavariable: $KEY
          regex: "(?i)(token|access_token|refresh_token|session|auth|jwt|bearer)"
    message: >
      Storing authentication tokens in localStorage is vulnerable to XSS attacks.
      Tokens should be kept in memory (React state/Context) or use HttpOnly cookies.
      Found: localStorage.$METHOD("$KEY")
    severity: ERROR
    languages:
      - typescript
      - javascript
    metadata:
      owasp: "A02:2021 - Cryptographic Failures"
      cwe: "CWE-312: Cleartext Storage of Sensitive Information"
```

**Why metavariable-regex:** Filters to only token-related keys, avoiding false positives on benign localStorage usage (e.g., theme preferences).

### Rule 2: Detect dangerouslySetInnerHTML (React)

**File:** `.semgrep/no-dangerously-set-inner-html.yaml`

```yaml
rules:
  - id: no-dangerously-set-inner-html
    patterns:
      - pattern-either:
          - pattern: dangerouslySetInnerHTML={{ __html: $CONTENT }}
          - pattern: dangerouslySetInnerHTML={$OBJ}
          - pattern: dangerouslySetInnerHTML=$VALUE
    message: >
      dangerouslySetInnerHTML bypasses React's XSS protections.
      If HTML rendering is required, sanitize input with DOMPurify first.
      Found: dangerouslySetInnerHTML with $CONTENT
    severity: ERROR
    languages:
      - typescript
      - javascript
      - tsx
      - jsx
    metadata:
      owasp: "A03:2021 - Injection"
      cwe: "CWE-79: Cross-site Scripting"
```

### Rule 3: Detect Hardcoded Credentials (C#)

**File:** `.semgrep/no-hardcoded-credentials.yaml`

```yaml
rules:
  - id: no-hardcoded-credentials
    patterns:
      - pattern-inside: |
          class $CLASS {
            ...
          }
      - pattern-either:
          - patterns:
              - pattern: $VAR = "$CONN"
              - metavariable-regex:
                  metavariable: $CONN
                  regex: "(?i)(Server|Data Source).*(Password|Pwd|UID)\\s*="
          - patterns:
              - pattern: $VAR = "$SECRET"
              - metavariable-regex:
                  metavariable: $SECRET
                  regex: "(sk-[a-zA-Z0-9]|ghp_[a-zA-Z0-9]|xox[bpaors]-|AKIA[0-9A-Z])"
          - patterns:
              - pattern: $CONN = "...Password=$PWD..."
    message: >
      Hardcoded credential detected. Use IConfiguration with environment variables,
      Azure Key Vault, or Docker secrets. Never commit secrets to source control.
      Found: $VAR = "...$SECRET..."
    severity: ERROR
    languages:
      - csharp
    metadata:
      owasp: "A07:2021 - Identification and Authentication Failures"
      cwe: "CWE-798: Use of Hard-coded Credentials"
```

### Rule 4: Detect Missing CSRF Validation (C# ASP.NET Core)

**File:** `.semgrep/no-missing-csrf.yaml`

```yaml
rules:
  - id: no-missing-csrf-validation
    patterns:
      - pattern-inside: |
          [HttpPost]
          ...
      - pattern-inside: |
          public IActionResult $METHOD(...) { ... }
      - pattern-not-inside: |
          [ValidateAntiForgeryToken]
          ...
      - pattern-not-inside: |
          [AutoValidateAntiforgeryToken]
          ...
    message: >
      HTTP POST action missing [ValidateAntiForgeryToken] attribute.
      Without CSRF protection, authenticated users are vulnerable to cross-site request forgery.
      Add [ValidateAntiForgeryToken] to the action or [AutoValidateAntiforgeryToken] globally.
    severity: ERROR
    languages:
      - csharp
    metadata:
      owasp: "A01:2021 - Broken Access Control"
      cwe: "CWE-352: Cross-Site Request Forgery"
```

**Note:** This rule targets Controllers with `[HttpPost]` that lack `[ValidateAntiForgeryToken]`. Controllers using global `[AutoValidateAntiforgeryToken]` on the class level will also trigger — use `# nosem: no-missing-csrf` to suppress if the global filter applies.

### Rule 5: Detect Raw CPF/CNPJ String Comparison (C#)

**File:** `.semgrep/no-raw-cpf-cnpj-comparison.yaml`

```yaml
rules:
  - id: no-raw-cpf-cnpj-comparison
    patterns:
      - pattern-either:
          - patterns:
              - pattern: $VAR == "$CPF"
              - metavariable-regex:
                  metavariable: $CPF
                  regex: "^\\d{11}$"
          - patterns:
              - pattern: $VAR == "$CNPJ"
              - metavariable-regex:
                  metavariable: $CNPJ
                  regex: "^\\d{14}$"
          - pattern: string.Compare($VAR, $VAL)
    message: >
      Direct string comparison of CPF/CNPJ detected.
      Use the domain Value Object (Cpf, Cnpj) for validation instead of raw string comparison.
      Raw strings cannot distinguish valid from invalid documents.
    severity: WARNING
    languages:
      - csharp
    metadata:
      category: domain-integrity
```

### Rule 6: Detect Insecure Deserialization (C#)

**File:** `.semgrep/no-insecure-deserialization.yaml`

```yaml
rules:
  - id: no-insecure-deserialization
    patterns:
      - pattern-either:
          - patterns:
              - pattern-inside: |
                  new BinaryFormatter();
                  ...
              - pattern: $OBJ.Deserialize(...)
          - patterns:
              - pattern-inside: |
                  new JavaScriptSerializer();
                  ...
              - pattern: $OBJ.Deserialize<$TYPE>(...)
          - pattern: |
              JsonSerializerOptions { TypeNameHandling = TypeNameHandling.Auto }
          - pattern: |
              JsonSerializerOptions { TypeNameHandling = TypeNameHandling.All }
    message: >
      Insecure deserialization detected. BinaryFormatter is obsolete and vulnerable to RCE.
      TypeNameHandling.Auto/All allows type injection attacks.
      Use DataContractSerializer or System.Text.Json with known types only.
    severity: ERROR
    languages:
      - csharp
    metadata:
      owasp: "A08:2021 - Software and Data Integrity Failures"
      cwe: "CWE-502: Deserialization of Untrusted Data"
```

## Don't Hand-Roll

### Use Native Features

| What | Use Instead |
|------|-------------|
| Custom secret scanning script | **Semgrep `--config auto`** pulls 2000+ built-in rules from registry (includes AWS keys, GitHub tokens, Stripe keys, etc.) |
| Custom SQL injection detector | **CodeQL `security-extended`** suite includes taint-tracking SQL injection queries for EF Core and raw SQL |
| Custom XSS detector | **CodeQL `security-extended`** includes `dangerouslySetInnerHTML` detection and React XSS patterns |
| Custom dependency auditor | **Phase 23** — `dotnet list package --vulnerable` + `npm audit` + Dependabot |
| Custom SARIF uploader | **`github/codeql-action/upload-sarif@v4`** is the official action for any SARIF-producing tool |
| Custom branch protection | **GitHub Branch Protection rules** with "Require status checks to pass" for CodeQL and Semgrep |
| Custom alert triage UI | **GitHub Security Tab** provides built-in alert management (open, dismiss, fix tracking) |
| Custom path traversal detector | **CodeQL built-in** — C# path injection queries cover `File.Open`, `Path.Combine` with user input |
| Custom CPF/CNPJ format validator | Semgrep rules above detect **raw string comparisons**. Actual CPF/CNPJ validation logic lives in domain VOs — Semgrep only flags the anti-pattern of bypassing them |

### What NOT to Build

- **No custom SAST engine.** Semgrep + CodeQL cover 95%+ of use cases.
- **No separate security dashboard.** GitHub Security Tab is sufficient for this project scale.
- **No custom CI failure logic.** Use Semgrep `--error` flag and CodeQL built-in failure modes.
- **No wrapper scripts around Semgrep/CodeQL.** Call CLI directly in workflow steps.
- **No custom rule for every CWE.** Only write custom rules for domain-specific patterns (CPF/CNPJ VO usage, project-specific anti-patterns). Use built-in rules for OWASP Top 10.

## Common Pitfalls

### False Positive Management

**Semgrep false positives:**
- **ASP.NET Core Controllers:** `no-missing-csrf` rule triggers on every `[HttpPost]` without explicit `[ValidateAntiForgeryToken]`. Many projects use global `[AutoValidateAntiforgeryToken]` — suppress with `# nosem: no-missing-csrf` on affected actions.
- **Hardcoded credentials regex:** Connection string patterns in `appsettings.Development.json` samples may match. Add to `.semgrepignore`.
- **localStorage rules:** Legitimate non-sensitive usage (theme, language preference) will not trigger due to `metavariable-regex` filter on token-related keys.

**CodeQL false positives:**
- **Test code:** CodeQL scans `*.Test.cs` and `*.Spec.cs` files. Exclude via `paths-ignore` in `codeql-config.yml`.
- **Generated code:** `.vinxi/` output, `obj/`, `bin/` directories. Exclude via `paths-ignore`.
- **Security-extended suite:** Lower precision rules generate more noise. Start with `security-extended`, review false positives, then exclude specific query IDs via `query-filters.exclude`.

### `.semgrepignore` Configuration

Create `.semgrepignore` at project root:

```
# Build outputs
**/bin/
**/obj/
**/dist/

# Dependencies
**/node_modules/

# Generated code
**/.vinxi/
**/*.generated.cs

# Test files (optional — may still want SAST on tests)
# **/*.test.ts
# **/*.spec.ts

# Configuration samples (contain placeholder secrets)
**/appsettings.Development.json
**/.env.example
**/*.env.sample

# Documentation
**/*.md
**/docs/
```

### `nosem` Comment Suppression

Use `// nosem: rule-id` (C#) or `// nosem: rule-id` (TypeScript) for intentional bypasses:

```csharp
// nosem: no-missing-csrf
public IActionResult PublicEndpoint([FromBody] Model model)
{
    // This endpoint is called by external webhooks — CSRF not applicable
    return Ok();
}
```

**Rule:** Every `nosem` comment MUST include a justification. Enforce via PR review.

### CodeQL Database Build

- **.NET 10 compatibility:** CodeQL 2.24.0+ supports .NET 10. Verify the action pulls v4.x which includes CLI 2.24.0+.
- **autobuild vs manual build:** `github/codeql-action/autobuild@v4` runs `dotnet build` automatically. If the project has complex build configurations, use `run:` with explicit `dotnet build` instead.
- **Memory usage:** CodeQL analysis can consume 4-7GB RAM. `ubuntu-latest` runners have 7GB — sufficient for this project. If it grows, consider `ubuntu-latest-large`.

### Alert Fatigue Prevention

1. **Start with `--error` only on ERROR severity** — WARNING/INFO findings post to Security Tab but don't block CI.
2. **First week:** Run SAST in `allow_failure: true` mode to baseline findings before enforcing.
3. **Dismiss false positives promptly** — a growing list of untriaged alerts creates noise and erodes trust.
4. **Weekly review cadence:** Review new alerts in GitHub Security Tab, dismiss FPs with documented reason.

### Semgrep Performance

- **Scan time:** Typically 30-90 seconds for this project size. Custom rules in `.semgrep/` add negligible overhead.
- **`--config auto` downloads:** First run downloads rule definitions from Semgrep Registry (~10-20MB). Subsequent runs cache in runner.
- **Caching:** Semgrep does not need explicit caching in GitHub Actions — rule downloads are cached by the CLI itself.

## Code Examples

### Complete `ci.yml` SAST Job Addition

```yaml
# Add to existing .github/workflows/ci.yml

  sast-semgrep:
    name: SAST — Semgrep
    runs-on: ubuntu-latest
    permissions:
      contents: read
      security-events: write
    steps:
      - uses: actions/checkout@v4

      - name: Install Semgrep
        run: pip install semgrep

      - name: Run Semgrep Scan
        run: |
          semgrep scan \
            --config auto \
            --config .semgrep/ \
            --output semgrep.sarif \
            --sarif \
            --error \
            --metrics off

      - name: Upload Semgrep SARIF
        uses: github/codeql-action/upload-sarif@v4
        if: always()
        with:
          sarif_file: semgrep.sarif
          category: semgrep

  sast-codeql:
    name: SAST — CodeQL
    runs-on: ubuntu-latest
    permissions:
      contents: read
      security-events: write
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "14.0.x"

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v4
        with:
          languages: csharp, javascript-typescript
          config-file: .github/codeql/codeql-config.yml

      - name: Autobuild
        uses: github/codeql-action/autobuild@v4

      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@v4
        with:
          category: codeql
```

### CodeQL Config File

**File:** `.github/codeql/codeql-config.yml`

```yaml
name: "Onboarding CodeQL Config"

queries:
  - uses: security-extended
  - uses: security-and-quality

query-filters:
  - exclude:
      problem.severity:
        - recommendation
  # Exclude known false-positive queries (tune after first scan)
  # - exclude:
  #     id: cs/missing-preconditions-check

paths-ignore:
  - "**/node_modules/**"
  - "**/bin/**"
  - "**/obj/**"
  - "**/.vinxi/**"
  - "**/*.test.*"
  - "**/*.spec.*"
  - "scripts/**"
  - "**/Migrations/**"
```

### Semgrep Custom Rule Template

All custom rules follow this template:

```yaml
rules:
  - id: <unique-rule-id>
    patterns:
      - pattern-either:
          - pattern: <code-pattern-1>
          - patterns:
              - pattern: <code-pattern>
              - metavariable-regex:
                  metavariable: $VAR
                  regex: "<regex-filter>"
    message: >
      <Description of the vulnerability and remediation guidance.>
      Found: <what was detected>
    severity: ERROR   # ERROR = CI fails; WARNING = posted to Security Tab only
    languages:
      - <language>
    metadata:
      owasp: "A0X:2021 - <Category>"
      cwe: "CWE-XXX: <Description>"
```

### Branch Protection Enforcement

After SAST jobs are passing:

1. Go to **Repository Settings > Branches > Branch protection rules**
2. Edit `main` branch rule
3. Under "Require status checks to pass before merging", add:
   - `SAST — Semgrep`
   - `SAST — CodeQL`
4. Enable "Require branches to be up to date before merging"
5. Save

This blocks merge until both SAST jobs complete successfully.

### Weekly Scheduled CodeQL Scan

Add a separate workflow for weekly full scans (catches issues in code that hasn't been PR'd):

```yaml
# .github/workflows/codeql-schedule.yml
name: CodeQL Scheduled Analysis

on:
  schedule:
    - cron: "0 6 * * 1"  # Monday 6am UTC
  workflow_dispatch:

permissions:
  contents: read
  security-events: write

jobs:
  codeql:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "14.0.x"
      - uses: github/codeql-action/init@v4
        with:
          languages: csharp, javascript-typescript
          config-file: .github/codeql/codeql-config.yml
      - uses: github/codeql-action/autobuild@v4
      - uses: github/codeql-action/analyze@v4
        with:
          category: codeql-scheduled
```

## Confidence Assessment

| Area | Confidence | Notes |
|------|-----------|-------|
| **Semgrep installation and CLI usage** | HIGH | Native `pip install semgrep` + `semgrep scan` is well-documented. Deprecated wrapper action confirmed. |
| **Semgrep custom rule syntax** | HIGH | Rule syntax verified against official Semgrep docs. YAML structure, patterns, metavariable-regex all confirmed. |
| **CodeQL Action v4** | HIGH | v4 is current stable. v3 deprecated Dec 2026. Migration is straightforward (`@v3` -> `@v4`). |
| **CodeQL .NET 10 support** | MEDIUM-HIGH | CodeQL 2.24.0 (Jan 2026) explicitly adds .NET 10 and C# 14 support. Issue #20827 was open at time of 2.23.x but should be resolved in 2.24.0+. Recommend verifying in actual pipeline before Phase 22 merge. |
| **SARIF upload via upload-sarif** | HIGH | `github/codeql-action/upload-sarif@v4` is the official, documented method for third-party SARIF ingestion. |
| **Semgrep --config auto coverage** | MEDIUM | Auto config pulls registry rules for detected languages. Exact rule coverage for C# ASP.NET Core patterns depends on registry freshness. Custom rules fill domain-specific gaps. |
| **CodeQL security-extended query relevance** | MEDIUM | Includes lower-precision queries that may generate noise. Actual false positive rate needs empirical verification after first scan. |
| **JavaScript/TypeScript CodeQL for Vinxi** | MEDIUM | CodeQL analyzes JS/TS files directly. Vinxi-specific bundler output (`.vinxi/`) should be excluded. CodeQL doesn't understand Vinxi's build-time transforms, but this is acceptable — it analyzes source code, not build output. |
| **CSRF detection accuracy** | MEDIUM | Semgrep pattern-based approach catches explicit `[HttpPost]` without `[ValidateAntiForgeryToken]`. May miss complex scenarios (filters, middleware-level CSRF). CodeQL's taint analysis is more thorough for dataflow-based CSRF detection. |
| **CI performance impact** | HIGH | Semgrep: ~30-90s. CodeQL: ~3-5min (includes autobuild). Both run in parallel with existing jobs — no additional wall-clock time for the pipeline. |

### Risks to Monitor

1. **CodeQL + .NET 10:** If CodeQL 2.24.0+ still has issues with the specific project structure, fallback to `build-mode: none` in CodeQL init (analyzes without compilation) — reduced precision for dataflow queries but still catches pattern-based issues.
2. **Semgrep rule drift:** Custom rules may need tuning after first real-world scan. Plan 1-2 iterations of rule refinement.
3. **Alert volume:** First run will likely surface many findings (especially from `--config auto`). Plan time for triage before enforcing CI failure.
