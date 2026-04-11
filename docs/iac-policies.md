# IaC Security Policies

Infrastructure as Code (IaC) security policies for Docker Compose and preparation for future Kubernetes deployments.

## Docker Compose Security Rules

All `compose.yaml` configurations must comply with the following rules, enforced by Checkov (`CKV_DOCKER_*` checks):

### CRITICAL — Block Merge

| Rule | Description | Checkov ID | Remediation |
|------|-------------|------------|-------------|
| No Privileged Mode | Containers must not run with `privileged: true` | CKV_DOCKER_3 | Remove `privileged: true` or use specific capabilities |
| No `--cap-add ALL` | Containers must not have all Linux capabilities | CKV_DOCKER_4 | Use `cap_add` with specific capabilities only |
| No Secrets in ENV | Database passwords and API keys must not be hardcoded | CKV_DOCKER_5 | Use `.env` files or Docker secrets |

### HIGH — Fix Within Sprint

| Rule | Description | Checkov ID | Remediation |
|------|-------------|------------|-------------|
| No Host Network | Containers must not use `network_mode: host` | CKV_DOCKER_8 | Use bridge networking with explicit port mapping |
| Read-Only Root Filesystem | Containers should run with read-only root filesystem | CKV_DOCKER_7 | Add `read_only: true` and use `tmpfs` for writable paths |

### MEDIUM — Review Recommended

| Rule | Description | Checkov ID | Remediation |
|------|-------------|------------|-------------|
| Non-Root User | Containers should not run as root | CKV_DOCKER_1 | Add `user: "1000:1000"` or equivalent |
| Memory Limits | Containers should have memory limits | CKV_DOCKER_6 | Add `deploy.resources.limits.memory` |
| CPU Limits | Containers should have CPU limits | CKV_DOCKER_9 | Add `deploy.resources.limits.cpus` |
| HEALTHCHECK | Services should have health checks | CKV_DOCKER_2 | Add `healthcheck` to each service |

## Future Kubernetes Policies

When migrating to Kubernetes, the following policies will be enforced via Kubescape:

### Pod Security Standards (Restricted)

- **Privileged**: Pods must not run in privileged mode
- **Host Namespaces**: No access to host network, PID, or IPC namespaces
- **Capabilities**: Drop ALL capabilities, add only required ones
- **Run As Non-Root**: All containers must run as non-root user
- **Seccomp**: Seccomp profile must be set to `RuntimeDefault` or `docker/default`

### Resource Management

- **Resource Quotas**: All namespaces must have resource quotas
- **Limits**: Every container must specify resource limits (CPU, memory)
- **Requests**: Resource requests must be set for scheduling

### Network Policies

- **Default Deny**: All namespaces must have a default-deny network policy
- **Explicit Allow**: Traffic between pods must be explicitly allowed
- **Egress Restrictions**: Outbound traffic should be restricted to required endpoints

### Image Security

- **Pinned Digests**: All container images must reference pinned digests (not tags)
- **Private Registry**: Images must come from approved registries only
- **No Latest Tag**: `latest` tag is prohibited

## Suppression Workflow

### Checkov Suppressions

Add exceptions to `.checkov.yml`:

```yaml
# .checkov.yml
skip-check:
  - CKV_DOCKER_2  # HEALTHCHECK — justified for development mode
  - CKV_DOCKER_6  # Memory limits — not applicable for dev environment
```

Every suppression **MUST** include a justification comment in the code:

```yaml
# checkov:skip=CKV_DOCKER_3:Privileged mode required for Docker-in-Docker testing
```

### Kubescape Suppressions

When K8s manifests exist, use annotations to suppress findings:

```yaml
metadata:
  annotations:
    kubescape.kscope.io/skip: "Justification for skipping this check"
```

### Review Process

1. Suppressions are reviewed during PR review
2. Each suppression requires: check ID, reason, expiration date
3. Suppressions are re-evaluated quarterly
4. Temporary suppressions (e.g., during migration) must have a removal date

## Tools

| Tool | Scans | CI Job | Local Command |
|------|-------|--------|---------------|
| **Checkov** | Docker Compose, Dockerfile | `iac-checkov` | `checkov -f compose.yaml --framework dockerfile_compose` |
| **Kubescape** | Kubernetes manifests | `iac-kubescape` | `kubescape scan infra/ --format sarif` |

### Running Checkov Locally

```bash
# Install
pip install checkov

# Scan compose.yaml
checkov -f compose.yaml --framework dockerfile_compose --compact

# Scan with config file
checkov --config-file .checkov.yml
```

### Running Kubescape Locally (when K8s manifests exist)

```bash
# Install
curl -s https://raw.githubusercontent.com/kubescape/kubescape/master/install.sh | /bin/bash

# Scan infra directory
kubescape scan infra/ --format sarif --output kubescape-results.sarif
```
