# Locked project decisions

D-1 (2026-05-11): Code design = **Clean Architecture + DDD + CQRS manual** (detectado e auto-confirmado em /jdi-adopt baseado em src/Onboarding.{Domain,Application,Infrastructure,API} + aggregates DDD + commands/handlers per slice). CQRS via DI manual — sem MediatR (commercial license, ver CLAUDE.md memoria feedback_no_mediatr.md).

D-2 (2026-05-11): Adopted brownfield. Boundary commit hash = **968eefb19dba216d729723e8ffa6a9e166d7698c**. Cobertura 80% enforced APENAS em arquivos novos criados apos este commit. Codigo pre-existente nao eh enforced. Reviewer usa este marker pra distinguir "new" vs "legacy".

D-3 (2026-05-11): OSS-only libraries — todos NuGet packages devem ser MIT/Apache 2.0. Sem MediatR (commercial), sem FluentAssertions (paid). Substitutos: CQRS manual via DI, Shouldly (em vez de FluentAssertions). Referencia: CLAUDE.md memoria feedback_oss_libraries.md.

D-4 (2026-05-11): Frontend separation architectural constraint — `frontend/client` e `frontend/backoffice` sao projetos totalmente independentes. Sem shared code, sem cross-imports, builds e deploys separados.

D-5 (2026-05-11): Isolamento multi-tenant eh requisito de seguranca de primeira classe. Aggregates company-scoped (Company, Employee, Fundo, ConsultoriaFundo, Custodiante, Cedente) tem HasQueryFilter + ClientId. TipoAtivo eh global (catalogo CVM compartilhado).

D-6 (2026-05-11): Sistema paralelo `.planning/` (legado, milestones v1-v8) coexiste com `.jdi/`. JDI nao modifica `.planning/`. Phases novas vao em `.jdi/`. Migracao gradual conforme phases JDI ficam done.
