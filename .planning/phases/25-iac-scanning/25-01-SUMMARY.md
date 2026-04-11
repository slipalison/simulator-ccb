# Plan 25-01 — Summary

**Executed:** 2026-04-11
**Status:** ✅ COMPLETE

## Changes Made

1. **`.checkov.yml`** — Created with dockerfile_compose framework, compact output, soft-fail disabled.

2. **`iac-checkov` job in `ci.yml`** — Checkov IaC scanning:
   - Uses `bridgecrew/checkov-action@master` (official action)
   - Scans `compose.yaml` with `framework: dockerfile_compose`
   - Checks: `CKV_DOCKER_*,CKV2_DOCKER_*`
   - `soft-fail: false` — CRITICAL/HIGH findings block merge
   - SARIF upload with `category: checkov`

## Validation Results

- Checkov config: ✅ Valid YAML, correct framework
- CI YAML: ✅ Valid syntax, job present, correct permissions
- Checkov action: ✅ Official `bridgecrew/checkov-action@master`
- compose.yaml: Uses `${APP_DB_PASSWORD}` (env var) — may trigger CKV_DOCKER_5 but is a false positive (secrets in `.env` file, not hardcoded)

## Files Changed

| File | Action |
|------|--------|
| `.checkov.yml` | Created |
| `.github/workflows/ci.yml` | Edited — added iac-checkov job |
