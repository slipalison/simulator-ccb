# Onboarding de Clientes PF/PJ

Sistema de onboarding para cadastro de clientes Pessoa Física e Pessoa Jurídica com autenticação via Keycloak.

## Security

[![CI](https://github.com/{owner}/onboarding/actions/workflows/ci.yml/badge.svg)](https://github.com/{owner}/onboarding/actions/workflows/ci.yml)
[![Dependabot](https://img.shields.io/badge/Dependabot-enabled-green.svg)](https://docs.github.com/en/code-security/dependabot)
[![Security Policy](https://img.shields.io/badge/Security-Policy-blue.svg)](.github/SECURITY.md)

This project runs **14 independent security checks** on every pull request:

| Category | Tools |
|----------|-------|
| Build/Test | .NET 10 + coverlet (80% coverage threshold) |
| Frontend | Vinxi (tsc, eslint, build) × 2 projects |
| SAST | Semgrep (custom rules), CodeQL (dataflow analysis) |
| SCA | Trivy (dependency CVEs), Dependabot (weekly updates) |
| SBOM | Syft (source code SPDX + container CycloneDX) |
| DAST | OWASP ZAP (baseline scan against running API) |
| Container | Trivy (image scan), Dockle (CIS Benchmarks) |
| IaC | Checkov (Docker Compose), Kubescape (K8s preparation) |
| Secrets | Gitleaks (pattern detection), TruffleHog (active verification) |

See [Security Overview](docs/security-overview.md) for complete documentation.

## Tech Stack

- **Backend**: .NET 10, ASP.NET Core Controllers, Entity Framework Core, PostgreSQL
- **Frontend**: React 19, Vinxi (Vite-based), TypeScript, Tailwind CSS, TanStack Router
- **Auth**: Keycloak 26.1 (hardened), JWT, ROPC grant
- **Infrastructure**: Docker Compose, GitHub Actions CI/CD
- **Observability**: Serilog, OpenTelemetry

## Quick Start

```bash
# Start infrastructure
docker compose up -d

# Backend
dotnet restore Onboarding.slnx
dotnet run --project src/Onboarding.API

# Frontend Client
cd frontend/client && npm ci && npm run dev

# Frontend Backoffice
cd frontend/backoffice && npm ci && npm run dev
```

## Documentation

- [Contributing](CONTRIBUTING.md) — Development setup, code quality, security tools
- [Security Overview](docs/security-overview.md) — All security documentation index
- [Security Runbook](docs/security-runbook.md) — Alert response procedures
- [Branch Protection](docs/branch-protection.md) — CI gating setup
- [IaC Policies](docs/iac-policies.md) — Docker Compose + K8s security rules
- [Compliance Mapping](docs/compliance-mapping.md) — OWASP/LGPD/CIS alignment

## License

Internal project — all rights reserved.
