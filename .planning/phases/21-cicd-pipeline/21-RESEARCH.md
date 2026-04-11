# Phase 21 Research — CI/CD Pipeline Foundation

## Standard Stack

### GitHub Actions
| Component | Version | Rationale | Confidence |
|-----------|---------|-----------|------------|
| actions/checkout | v4 | Repository checkout | HIGH |
| actions/cache | v5.0.4 | Dependency caching (Node.js 24 runtime, requires runner >= 2.327.1). v4 (Node.js 20) also acceptable. | HIGH |
| actions/setup-dotnet | v4+ | .NET 10 SDK installation | HIGH |
| actions/setup-node | v4+ | Node.js installation for frontend | HIGH |
| dorny/test-reporter | v1+ (optional) | Publish test results in GitHub UI | MEDIUM |

### .NET 10 CI
| Component | Package | Version | Rationale | Confidence |
|-----------|---------|---------|-----------|------------|
| coverlet.collector | 8.0.1 | Current in project. Default in xUnit templates. Generates Cobertura XML. **Does NOT support threshold enforcement.** | HIGH |
| coverlet.msbuild | 6.0.3+ | Alternative with threshold support via `/p:Threshold=80`. **Required if enforcing coverage gates.** | HIGH |
| ReportGenerator | 5.x (optional) | Convert Cobertura XML to HTML reports | MEDIUM |
| Microsoft.Testing.Platform | Built-in .NET 10 | Integrated into `dotnet test`. Standardized CLI command order. | HIGH |

### Frontend CI
| Component | Command | Rationale | Confidence |
|-----------|---------|-----------|------------|
| Type check | `tsc --noEmit` | Validates TypeScript without emitting files | HIGH |
| Lint | `eslint . --max-warnings 0` | Zero-warning policy enforced by requirement | HIGH |
| Build | `vinxi build` | Production build. Generates `.output/` directory | HIGH |
| Test | `vitest run` | Unit tests (already configured in both frontends) | HIGH |

### Key Findings on .NET 10 Tooling
- **.NET 10 SDK** includes native `Microsoft.Testing.Platform` support in `dotnet test`. CLI command order has been standardized — legacy scripts may need review.
- **`dotnet test`** remains the entry point. No breaking changes to basic `restore -> build -> test` pipeline.
- **Terminal Logger** is default since .NET 9 — CI environments auto-detect and suppress it (no `--nologo` needed, but harmless to include).

---

## Architecture Patterns

### 1. Parallel Job Workflow Structure

Use **explicit named jobs** (not matrix) for the 3 independent pipelines. Matrix builds are for running the same steps across multiple configurations (OS, .NET version). Here, each project has different tooling, steps, and cache keys.

```yaml
jobs:
  backend:
    runs-on: ubuntu-latest
    steps: ...
  frontend-client:
    runs-on: ubuntu-latest
    steps: ...
  frontend-backoffice:
    runs-on: ubuntu-latest
    steps: ...
```

No `needs:` between them — they run in parallel by default. All three are independent quality gates.

### 2. Caching Strategy

**Backend (NuGet):**
- Cache path: `~/.nuget/packages`
- Key: `${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}`
- Restore key: `${{ runner.os }}-nuget-`
- **Do NOT cache `bin/` or `obj/`** — they contain compiled artifacts that invalidate between builds and cause subtle bugs.

**Frontend (npm):**
- Cache path: `~/.npm` (the npm cache directory, NOT `node_modules`)
- Key: `${{ runner.os }}-npm-${{ hashFiles('frontend/client/package-lock.json') }}` (per-project)
- Restore key: `${{ runner.os }}-npm-`
- **Do NOT cache `node_modules`** — GitHub Actions official docs explicitly warn against this. It breaks with `npm ci` and causes cross-Node-version corruption.
- After cache restore, `npm ci` is still fast because the npm cache is warm.

**Cross-job cache behavior:** Cache is written at the end of the job and persisted to GitHub's storage. It does NOT share between parallel jobs in the same run — each job must populate its own cache on first run. Subsequent workflow runs benefit from the cache.

### 3. Coverage Enforcement Pattern

**CRITICAL FINDING:** The project currently uses `coverlet.collector` (v8.0.1), which does **NOT support threshold enforcement**. This is a known, documented limitation:

> "At the moment VSTest integration doesn't support all features of msbuild and .NET tool, for instance show result on console, report merging and threshold validation."

**Two approaches to enforce 80% coverage:**

**Approach A — Switch to `coverlet.msbuild` (Recommended):**
Replace `coverlet.collector` with `coverlet.msbuild` in test `.csproj` files. Then:
```bash
dotnet test --no-build /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```
This natively fails the build if coverage < 80%.

Trade-off: `coverlet.msbuild` is in maintenance mode and can have file lock issues in parallel test runs (mitigated by running tests sequentially in CI).

**Approach B — Keep `coverlet.collector` + parse report:**
Keep the current collector, generate Cobertura XML, then use a script or `reportgenerator` to parse and check coverage. More complex, more moving parts.

**Recommendation: Approach A.** Add `coverlet.msbuild` alongside `coverlet.collector` (they can coexist). Use `coverlet.msbuild` in CI for threshold enforcement, keep `coverlet.collector` for local dev. Or replace entirely with `coverlet.msbuild`.

**Threshold syntax (verified via Coverlet docs):**
```bash
# Fail if any module has < 80% line coverage
dotnet test /p:CollectCoverage=true /p:Threshold=80

# Fail if TOTAL line coverage across all modules < 80%
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total

# Multiple thresholds (line >= 80%, branch >= 100%, method >= 70%)
dotnet test /p:CollectCoverage=true /p:Threshold="80,100,70" /p:ThresholdType="line,branch,method"
```

### 4. Dual Frontend Project Pattern

Each frontend is an independent job with its own working directory. No shared code, no shared `node_modules`, no shared cache.

```yaml
frontend-client:
  steps:
    - uses: actions/setup-node@v4
    - working-directory: frontend/client
      run: npm ci
    - working-directory: frontend/client
      run: npx tsc --noEmit
    - working-directory: frontend/client
      run: npx eslint . --max-warnings 0
    - working-directory: frontend/client
      run: npm run build

frontend-backoffice:
  steps:
    - uses: actions/setup-node@v4
    - working-directory: frontend/backoffice
      run: npm ci
    # ... same steps
```

**Important:** ESLint is not yet installed in either frontend project (no `eslint.config.*` files found). This must be addressed in a prerequisite task before the CI can run `eslint --max-warnings 0`.

### 5. Workflow Triggers

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:  # Manual trigger for ad-hoc runs
```

This covers: every push to main, every PR targeting main, and manual runs from the Actions tab.

---

## Don't Hand-Roll

### Use GitHub Native Features

| Problem | Use This | NOT This |
|---------|----------|----------|
| Parallel execution | Separate top-level `jobs:` (runs in parallel by default) | Custom dispatch workflows, manual orchestration |
| Caching | `actions/cache@v5` with `hashFiles()` | Custom scripts that save/restore artifacts |
| Dependency installation | `dotnet restore`, `npm ci` | Manual package download scripts |
| Test execution | `dotnet test`, `vitest run` | Custom test runners |
| Type checking | `tsc --noEmit` | Custom TypeScript compiler scripts |
| Build execution | `vinxi build`, `dotnet build` | Webpack/Vite custom configs |
| Workflow permissions | Top-level `permissions: read-all` + job-level overrides | Custom token management |
| Coverage collection | `coverlet.msbuild` with `/p:Threshold=80` | Custom coverage parsing scripts |
| Fail on PR | GitHub fails the check status automatically | Custom PR comment bots |

### What NOT to Build

- **Custom coverage threshold scripts** — Coverlet's `/p:Threshold` does this natively (with `coverlet.msbuild`).
- **Manual cache save/restore logic** — `actions/cache@v5` handles key matching, fallback restore-keys, and eviction.
- **Multi-project build orchestration** — GitHub parallel jobs handle this. Don't write a dispatcher.
- **Artifact upload for coverage reports** — Use GitHub Checks API or PR annotations instead of uploading files (unless you need HTML reports for archival).
- **Custom ESLint formatter for GitHub** — ESLint natively supports `--format github` (via `@github/eslint-formatter`) or use the standard formatter with GitHub's error annotations via `::error file=...` workflow commands.

---

## Common Pitfalls

### 1. Coverlet Collector vs MSBuild Confusion
**Pitfall:** Using `coverlet.collector` and expecting `/p:Threshold=80` to work. It silently ignores the threshold — the build passes regardless of coverage.

**Fix:** Use `coverlet.msbuild` for CI threshold enforcement. The project currently references `coverlet.collector` v8.0.1 (API tests) and v6.0.4 (integration tests) — both lack threshold support.

**Verification:** Run `dotnet test /p:CollectCoverage=true /p:Threshold=80` with `coverlet.collector` — the threshold parameter is ignored without error.

### 2. Cache Miss on Every Run
**Pitfall:** Using `hashFiles('**/*')` which includes non-lock files, causing cache misses on every code change.

**Fix:** Hash only lockfiles: `hashFiles('**/*.csproj')` for NuGet, `hashFiles('**/package-lock.json')` for npm.

### 3. Caching `node_modules` Instead of npm Cache
**Pitfall:** Caching `node_modules` directory. This is incompatible with `npm ci` (which deletes `node_modules` before installing) and causes cross-Node-version issues.

**Fix:** Cache `~/.npm` (the npm cache directory). `npm ci` still benefits because it downloads from the warm local cache instead of the registry.

### 4. ESLint Not Installed
**Pitfall:** The CI workflow runs `eslint --max-warnings 0` but ESLint is not configured in either frontend project. No `eslint.config.*`, no `eslint` in `devDependencies`.

**Fix:** ESLint must be added to both frontend projects as a prerequisite task (install `eslint`, configure flat config, add `eslint` npm script).

### 5. Vinxi Build in CI
**Pitfall:** Vinxi builds assume a server preset. In CI on `ubuntu-latest`, the default preset auto-detects the environment and produces a Node.js server build. No special env vars needed for basic CI.

**Gotcha:** `vinxi build` generates `.output/` directory. If the workflow later tries to `vinxi start`, it needs `node .output/server/index.mjs`. For CI validation (just checking build succeeds), `vinxi build` alone is sufficient.

### 6. ThresholdStat: minimum vs total
**Pitfall:** Default `ThresholdStat=minimum` enforces the threshold per module. If you have 3 test projects, EACH must have >= 80% coverage. With `ThresholdStat=total`, the combined coverage across all projects must be >= 80%.

**Fix:** For a project with multiple test assemblies, use `/p:ThresholdStat=total` to enforce 80% on the aggregate. Otherwise, a focused test project with 100% coverage can't compensate for an integration test project at 60%.

### 7. Multiple Test Projects in Single `dotnet test`
**Pitfall:** Running `dotnet test` on the solution file runs all test projects sequentially. Coverage is aggregated. If one test project fails, the whole job fails.

**Fix:** Run `dotnet test` per test project for isolated reporting, OR run on the solution file with `--no-build` for speed. For CI simplicity, solution-level test is acceptable.

### 8. .NET 10 CLI Command Order
**Pitfall:** .NET 10 standardized CLI argument order. Legacy scripts with properties before the command (`/p:CollectCoverage=true dotnet test`) will break.

**Fix:** Always: `dotnet test [project] [options] /p:Property=Value`. Properties come AFTER the subcommand.

### 9. Parallel Job Resource Contention
**Pitfall:** All 3 jobs run on separate `ubuntu-latest` runners. No resource contention — GitHub provisions independent VMs per job.

**Not a concern:** Unlike self-hosted runners, `ubuntu-latest` has no shared state between jobs.

### 10. Cache Size and Eviction
**Pitfall:** GitHub enforces 10 GB total cache per repository. Caches not accessed in 7 days are evicted. Least-recently-used eviction when limit is reached.

**Mitigation:** Keep cache keys specific (lockfile-based). Avoid caching build outputs (`bin/`, `obj/`, `.vinxi/`). The cache for NuGet + npm for this project should stay well under 10 GB.

### 11. Flaky Tests in CI
**Pitfall:** Integration tests using Testcontainers (PostgreSQL, Keycloak) may timeout in CI due to slower startup.

**Fix:** Increase container startup timeouts in CI. Use `CI=true` environment variable to adjust test timeouts. Consider retry logic for known flaky tests (`xunit.runner.json` with `"maxParallelThreads"`).

---

## Code Examples

### Minimal `.github/workflows/ci.yml` Scaffold

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  workflow_dispatch:

permissions: read-all

jobs:
  backend:
    name: Backend (.NET 10)
    runs-on: ubuntu-latest
    permissions:
      contents: read
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Cache NuGet packages
        uses: actions/cache@v5
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore Onboarding.slnx

      - name: Build
        run: dotnet build Onboarding.slnx --no-restore --configuration Release

      - name: Test
        run: dotnet test Onboarding.slnx --no-build --configuration Release --verbosity normal /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total /p:CoverletOutputFormat=cobertura

  frontend-client:
    name: Frontend Client (Vinxi)
    runs-on: ubuntu-latest
    permissions:
      contents: read
    defaults:
      run:
        working-directory: frontend/client
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "22"

      - name: Cache npm
        uses: actions/cache@v5
        with:
          path: ~/.npm
          key: ${{ runner.os }}-npm-client-${{ hashFiles('frontend/client/package-lock.json') }}
          restore-keys: |
            ${{ runner.os }}-npm-client-

      - name: Install dependencies
        run: npm ci

      - name: Type check
        run: npx tsc --noEmit

      - name: Lint
        run: npx eslint . --max-warnings 0

      - name: Build
        run: npm run build

  frontend-backoffice:
    name: Frontend Backoffice (Vinxi)
    runs-on: ubuntu-latest
    permissions:
      contents: read
    defaults:
      run:
        working-directory: frontend/backoffice
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: "22"

      - name: Cache npm
        uses: actions/cache@v5
        with:
          path: ~/.npm
          key: ${{ runner.os }}-npm-backoffice-${{ hashFiles('frontend/backoffice/package-lock.json') }}
          restore-keys: |
            ${{ runner.os }}-npm-backoffice-

      - name: Install dependencies
        run: npm ci

      - name: Type check
        run: npx tsc --noEmit

      - name: Lint
        run: npx eslint . --max-warnings 0

      - name: Build
        run: npm run build
```

### .NET Coverage Enforcement (with coverlet.msbuild)

**Prerequisite:** Add `coverlet.msbuild` to test project `.csproj` files:
```xml
<ItemGroup>
  <PackageReference Include="coverlet.msbuild" Version="6.0.3">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>
```

**CI command:**
```bash
dotnet test Onboarding.slnx \
  --no-build \
  --configuration Release \
  /p:CollectCoverage=true \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total \
  /p:CoverletOutputFormat=cobertura
```

This command:
1. Runs all tests in the solution
2. Collects line coverage across all test projects
3. Fails if total line coverage < 80%
4. Outputs Cobertura XML for potential PR annotations

### Vinxi Build + Lint (per frontend)

```bash
# Install (deterministic, uses lockfile)
npm ci

# Type check (fails on type errors, no output files)
npx tsc --noEmit

# Lint (fails on any warning)
npx eslint . --max-warnings 0

# Production build
npm run build
```

Order matters: type check before lint (catches type errors faster), lint before build (ensures code quality), build last (validates full compilation).

### Coverage Report Upload (Optional)

If HTML coverage reports are needed for PR review:
```yaml
- name: Generate coverage report
  uses: danielpalme/ReportGenerator-GitHub-Action@5.2.4
  with:
    reports: '**/coverage.cobertura.xml'
    targetdir: 'coveragereport'
    reporttypes: 'HtmlInline;Cobertura'

- name: Upload coverage report
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: coveragereport/
```

---

## Confidence Assessment

### HIGH Confidence
- **GitHub Actions parallel jobs** — Separate top-level `jobs:` run in parallel by default. Well-documented, stable behavior.
- **`actions/cache@v5` caching strategy** — Cache `~/.nuget/packages` and `~/.npm`, NOT `node_modules` or `bin/`. Key on lockfile hash. Restore keys for fallback. Official GitHub documentation confirms this.
- **`coverlet.msbuild` threshold enforcement** — `/p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total` syntax is verified against official Coverlet NuGet docs and GitHub documentation.
- **`coverlet.collector` lacks threshold support** — Explicitly documented in Coverlet's own docs. Verified across multiple sources (Stack Overflow, NuGet, GitHub issues). Still true in v8.0.1.
- **Vinxi build process** — `vinxi build` generates `.output/` directory. No special env vars needed for Node.js preset on ubuntu-latest. Confirmed via Vinxi docs and SolidStart Docker guides.
- **`npm ci` + npm cache** — Cache `~/.npm`, not `node_modules`. `npm ci` benefits from warm cache. Official actions/cache examples confirm this.
- **GitHub Actions security** — `permissions: read-all` at top level, `contents: read` per job. Confirmed via GitHub official docs.
- **Node.js version 22** — Current LTS. Both frontends use React 19 + Vinxi 0.5.x which require Node.js 18+.

### MEDIUM Confidence
- **`coverlet.msbuild` parallel test safety** — Documentation mentions file lock issues in parallel runs, but running `dotnet test` on solution file sequentially should be fine. Needs verification with the actual test suite.
- **ESLint installation requirement** — ESLint is not currently in either frontend's `devDependencies`. This is a gap that must be addressed. The exact ESLint config (flat config, plugins) depends on the project's coding standards.
- **`.NET 10 CLI command order`** — Microsoft states CLI order was standardized, but the exact impact on existing scripts is unclear. Basic `dotnet test project /p:Prop=Value` pattern should work unchanged.
- **Vinxi + TanStack Router type generation** — TanStack Router auto-generates route types. `tsc --noEmit` should catch type errors, but route tree generation may need to run first (`vinxi dev` or a codegen step). Needs verification with the actual project.
- **actions/cache@v5 on GitHub-hosted runners** — v5 requires runner >= 2.327.1. Current `ubuntu-latest` should meet this, but exact runner version in GitHub's pool needs confirmation at implementation time.

### LOW Confidence
- **Coverlet.MTP (Microsoft.Testing.Platform) threshold support** — Coverlet.MTP is the new integration for .NET 10's testing platform. Threshold validation is listed as "planned for future releases." If the project migrates to MTP, threshold enforcement would need a custom script. Currently, the project uses xUnit with VSTest adapter, not MTP, so this is not an immediate concern.
- **Vinxi version 0.5.x stability in CI** — Vinxi is a relatively young project (0.5.11). No documented CI-specific issues found, but the ecosystem is evolving. The `vinxi build` command should work, but edge cases with SSR/route manifest generation in headless environments are unknown.
- **Coverage aggregation across 3 test projects** — With `ThresholdStat=total`, Coverlet aggregates coverage across all test projects. The exact behavior when different test projects cover different assemblies needs empirical verification.
