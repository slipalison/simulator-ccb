---
plan: 04-03
phase: 04-observability
status: complete
completed: 2026-04-03
tasks_total: 3
tasks_completed: 3
deviations: 2
self_check: PASSED
---

## What Was Built

Grafana observability stack (Alloy, Loki, Tempo, Mimir, Grafana) added to `compose.yaml`. Alloy receives OTLP on port 4317 and routes logs→Loki, traces→Tempo, metrics→Mimir. Grafana at http://localhost:3000 auto-provisioned with all three datasources.

## Key Files Created

- `infra/alloy/config.alloy` — OTLP receiver pipeline routing to Loki, Tempo, Mimir
- `infra/grafana/provisioning/datasources/datasources.yaml` — Loki, Tempo, Mimir datasources
- `infra/tempo/tempo.yaml` — Tempo single-binary (HTTP 3200, OTLP gRPC 4317)
- `infra/loki/loki-config.yaml` — Loki single-process (HTTP 3100)
- `infra/mimir/mimir.yaml` — Mimir monolithic mode (target: all, HTTP 9009)
- `compose.yaml` — 5 new services, 4 new volumes, OTEL env vars on api service

## Commits

- `5c7b945` feat(04-03): create infra/ config files for Alloy, Tempo, Loki, Mimir, and Grafana provisioning
- `c44fe99` feat(04-03): add Grafana observability stack services to compose.yaml
- `e3f1d6d` fix(04-03): fix Mimir monolithic mode config and API healthcheck endpoint
- `7da99ef` fix(04-03): fix Mimir storage path overlap by removing common.storage

## Deviations

1. **OTEL env vars added to api service** — Plan assumed Plan 04-02 would add these, but api needed them immediately to export to alloy. Added `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` in this plan.
2. **Mimir config fixes** — Two iterations needed: (a) added `target: all` for monolithic mode, (b) removed `common.storage` to avoid path overlap with `ruler_storage`. Final working config uses explicit paths per component.

## Checkpoint Result

Human verified: Grafana at http://localhost:3000 shows Loki, Tempo, and Mimir datasources correctly. All 5 new services start without errors.
