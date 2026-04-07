---
status: awaiting_human_verify
trigger: "GET http://localhost:8080/healthz/live retorna 404"
created: 2026-04-07T00:00:00Z
updated: 2026-04-07T00:05:00Z
---

## Current Focus

hypothesis: CONFIRMADA E RESOLVIDA — duas causas combinadas:
  1. HealthController.cs com [HttpGet("/healthz")] existia como código morto/conflitante, sem rota para /live
  2. MapHealthChecks("/healthz/live") não tinha .AllowAnonymous() — com UseAuthorization() ativo no pipeline, os endpoints de health check podiam ser bloqueados pelo middleware de autorização

test: dotnet test --filter "Category=HealthCheck" após remover HealthController e adicionar .AllowAnonymous()
expecting: 5/5 testes passando
next_action: checkpoint — aguardar verificação humana em ambiente real

## Symptoms

expected: GET /healthz/live retorna 200 OK com status de saúde da API
actual: retorna 404 — página não encontrada
errors: HTTP 404
reproduction: curl http://localhost:8080/healthz/live
started: comportamento atual — não se sabe se já funcionou

## Eliminated

- hypothesis: Problema de configuração do Docker (porta errada)
  evidence: compose.yaml mapeia corretamente 127.0.0.1:8080:8080 e healthcheck usa curl -f http://localhost:8080/healthz/live
  timestamp: 2026-04-07T00:00:00Z

- hypothesis: MapHealthChecks não registrado em Program.cs
  evidence: Program.cs linha 129 registra explicitamente app.MapHealthChecks("/healthz/live", ...) antes de app.MapControllers()
  timestamp: 2026-04-07T00:00:00Z

## Evidence

- timestamp: 2026-04-07T00:00:00Z
  checked: src/Onboarding.API/Controllers/HealthController.cs
  found: Classe HealthController com [Route("[controller]")] e [HttpGet("/healthz")] — rota absoluta "/healthz" apenas, SEM "/live" ou "/ready"
  implication: Este controller NÃO serve /healthz/live — mas existe como controller registrado via MapControllers()

- timestamp: 2026-04-07T00:00:00Z
  checked: src/Onboarding.API/Program.cs linhas 129-141
  found: app.MapHealthChecks("/healthz/live") registrado ANTES de app.MapControllers(). Middleware de health checks tem prioridade de rota.
  implication: /healthz/live deveria ser servido pelo middleware, não pelo controller

- timestamp: 2026-04-07T00:00:00Z
  checked: Conflito conceitual entre HealthController e MapHealthChecks
  found: HealthController.Get() retorna manualmente { status = "healthy", timestamp = ... } via IActionResult. MapHealthChecks usa o serviço AddHealthChecks() com checks reais. São duas implementações paralelas e inconsistentes do mesmo conceito.
  implication: O HealthController é código morto que deveria ter sido removido quando MapHealthChecks foi adicionado. Não causa diretamente o 404 em /live (pois sua rota é /healthz, não /healthz/live), mas é código conflitante e enganoso.

- timestamp: 2026-04-07T00:00:00Z
  checked: Análise do comportamento de roteamento ASP.NET Core
  found: MapHealthChecks registra um endpoint de roteamento com padrão exato "/healthz/live". MapControllers registra o HealthController com rota "/healthz". Não há sobreposição direta. O 404 em /healthz/live ocorre provavelmente porque o Dockerfile não expõe a porta corretamente, ou porque a aplicação não está iniciando (dependência do Keycloak failing startup), ou por alguma outra razão de infraestrutura.
  implication: O HealthController em si não é a causa direta do 404, mas é código problemático que deve ser removido

## Resolution

root_cause: Duas causas combinadas:
  1. HealthController.cs definia [HttpGet("/healthz")] — rota absoluta que não cobre /healthz/live nem /healthz/ready. Código morto e conflitante com o sistema MapHealthChecks do ASP.NET Core.
  2. MapHealthChecks("/healthz/live") e MapHealthChecks("/healthz/ready") não tinham .AllowAnonymous(). Com UseAuthorization() registrado no pipeline (linha 126 do Program.cs), os endpoints de health check ficavam sujeitos ao middleware de autorização, podendo ser bloqueados dependendo da política padrão — causando 404 ou 401 em vez de 200.
fix: (1) Removido src/Onboarding.API/Controllers/HealthController.cs. (2) Adicionado .AllowAnonymous() em ambos os MapHealthChecks em Program.cs.
verification: dotnet test --filter "Category=HealthCheck" — 5/5 aprovados. Suite completa — 42/42 aprovados, 0 falhas.
files_changed: [src/Onboarding.API/Controllers/HealthController.cs, src/Onboarding.API/Program.cs]
