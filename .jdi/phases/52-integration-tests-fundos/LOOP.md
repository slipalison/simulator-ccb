---
phase_slug: integration-tests-fundos
phase_position: 52
iter: 0
total_resets: 1
status: running
max_iter_per_round: 5
max_resets: 3
created_at: 2026-05-23T19:46:14-03:00
last_reset_at: 2026-05-24T10:09:42-03:00
---

## History

- iter 1: BLOCKED, hash=6940e4670c3e, commit=7429666, ts=2026-05-23T22:10:23-03:00, summary="9 blockers: backend B1-B4 + frontend BFE-1-4 + security SEC-B1"
- iter 2: BLOCKED, hash=0b1a8653f72d, commit=f6e9180, ts=2026-05-23T23:28:05-03:00, summary="4 residual blockers: backend B1-iter2/B2-iter2/B3-iter2 + frontend BFE-5; security now APPROVED_WITH_WARNINGS"
- iter 3: BLOCKED, hash=b53a1c9f0da7, commit=1f65537, ts=2026-05-24T00:14:29-03:00, summary="2 residual backend blockers: B3-iter3 EF.Property needs CnpjRaw shadow prop + G11-iter3 coverlet.msbuild missing on Domain.Tests; frontend APPROVED_WITH_WARNINGS; security APPROVED_WITH_WARNINGS"
- iter 4: BLOCKED, hash=6b29fa62ce48, commit=41a2a91, ts=2026-05-24T00:41:43-03:00, summary="REGRESSION: shadow CnpjRaw maps same column as Cnpj VO → EF model init crash → 185 new test failures. New blocker B4-iter4; G11-iter3 RESOLVED. Reviewer suggests client-side projection."
- iter 5: BLOCKED, hash=e3b0c44298fc, commit=f871395, ts=2026-05-24T01:08:07-03:00, summary="B4-iter4 RESOLVED (187/187 integration tests pass). New B5-iter5 G11 coverage: 4 D-2 files <80% (GetFundoCedentesQueryHandler 22%, GetFundoTiposAtivosQueryHandler 22%, GetCedenteTiposAtivosQueryHandler 22%, AdminFundosController 67%). Coverage now measurable post-EF fix. Reviewer concrete fix: 8 happy-path GET-list integration tests."

--- RESET 1 at 2026-05-24T10:09:42-03:00 (cap hit at iter 5, user implicit-continue via re-invoke /jdi-loop; trajectory positive — iter 5 resolved B4 regression, only B5 coverage gap remains with concrete 8-test fix) ---
