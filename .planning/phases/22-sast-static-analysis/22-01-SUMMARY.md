# Plan 22-01 — Summary

**Executed:** 2026-04-11
**Status:** ✅ COMPLETE

## Changes Made

1. **`.semgrep/` directory** — 6 custom Semgrep rules:
   - `no-localstorage-tokens.yaml` (ERROR, TS/JS) — detects `localStorage.setItem` with token-related keys
   - `no-dangerously-set-inner-html.yaml` (ERROR, TS/JS) — detects `dangerouslySetInnerHTML` (XSS risk)
   - `no-hardcoded-credentials.yaml` (ERROR, C#) — detects connection strings with passwords, API key patterns
   - `no-missing-csrf.yaml` (ERROR, C#) — detects `[HttpPost]` without `[ValidateAntiForgeryToken]`
   - `no-raw-cpf-cnpj-comparison.yaml` (WARNING, C#) — detects raw CPF/CNPJ string comparison
   - `no-insecure-deserialization.yaml` (ERROR, C#) — detects `BinaryFormatter`, `TypeNameHandling.Auto/All`

2. **`.semgrepignore`** — excludes build outputs, dependencies, generated code, test config, IDE files

3. **`sast-semgrep` job in `ci.yml`** — pip install semgrep → scan with `--config auto --config .semgrep/` → SARIF upload with `category: semgrep`

4. **CONTRIBUTING.md** — already had complete local SAST run instructions (no changes needed)

## Validation Results

- Semgrep scan (6 custom rules): ✅ 0 findings on codebase (clean)
- Rule validation: ✅ `no-localstorage-tokens` correctly detects `localStorage.setItem("authToken", token)` and ignores `localStorage.setItem("theme", "dark")`
- YAML syntax: ✅ Valid (all 6 rules + .semgrepignore)
- CI job: ✅ Valid YAML, `security-events: write` permission, SARIF upload configured

## Issues Encountered

- **Semgrep pattern-regex on Windows**: Some regex rules had YAML escaping issues with double-quoted strings. Resolved by using single-quoted regex or pattern-regex at root level.
- **Language `tsx`/`jsx`**: Not supported by Semgrep CLI — use `typescript` and `javascript` instead (Semgrep handles TSX/JSX under these names).
- **CSRF rule**: AST-based pattern matching for `[HttpPost]` without `[ValidateAntiForgeryToken]` is complex due to attribute ordering. Switched to pattern-regex as fallback.

## Files Changed

| File | Action |
|------|--------|
| `.semgrep/no-localstorage-tokens.yaml` | Created |
| `.semgrep/no-dangerously-set-inner-html.yaml` | Created |
| `.semgrep/no-hardcoded-credentials.yaml` | Created |
| `.semgrep/no-missing-csrf.yaml` | Created |
| `.semgrep/no-raw-cpf-cnpj-comparison.yaml` | Created |
| `.semgrep/no-insecure-deserialization.yaml` | Created |
| `.semgrepignore` | Overwritten (updated exclusions) |
| `.github/workflows/ci.yml` | Edited — added sast-semgrep job |
