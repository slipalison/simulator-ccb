# Branch Protection Setup

This document guides repository administrators through configuring branch protection for the `main` branch.

## Prerequisites

- Repository admin access
- CI must have run at least once on `main` (so status checks are registered)

## Step-by-Step Configuration

### 1. Navigate to Branch Protection Settings

1. Open the repository on GitHub
2. Click **Settings** tab
3. In the left sidebar, click **Branches** (under "Code and automation")
4. Click **Add rule** or click **Edit** on the existing `main` rule

### 2. Configure the Rule

| Setting | Value |
|---------|-------|
| **Branch name pattern** | `main` |
| **Require a pull request before merging** | ✅ Enabled |
| **Require approvals** | ✅ Enabled (minimum 1 approval) |
| **Dismiss stale pull request approvals when new commits are pushed** | ✅ Enabled |
| **Require status checks to pass before merging** | ✅ Enabled |
| **Require branches to be up to date before merging** | ✅ Enabled |

### 3. Add Required Status Checks

Under "Status checks that are required", search and add the following **exact names**:

| # | Status Check Name | CI Job |
|---|-------------------|--------|
| 1 | `Backend (.NET 10)` | Backend build + test |
| 2 | `Frontend Client (Vinxi)` | Frontend client build |
| 3 | `Frontend Backoffice (Vinxi)` | Frontend backoffice build |
| 4 | `SAST — Semgrep` | Semgrep code scanning |
| 5 | `SAST — CodeQL` | CodeQL code scanning |
| 6 | `SCA — Trivy` | Trivy dependency scan |
| 7 | `Container Scan — Trivy Image` | Trivy container scan |
| 8 | `Container Lint — Dockle` | Dockle container lint |
| 9 | `IaC — Checkov` | Checkov IaC scan |
| 10 | `Secrets — Gitleaks` | Gitleaks secrets detection |
| 11 | `Secrets — TruffleHog` | TruffleHog secret verification |

**Note:** The `IaC — Kubescape` job should NOT be added as a required check yet — it currently skips when no K8s manifests exist and may produce inconsistent status.

### 4. Recommended Additional Settings

| Setting | Recommendation | Reason |
|---------|---------------|--------|
| **Require conversation resolution before merging** | ✅ Enabled | Ensures all PR comments are addressed |
| **Include administrators** | ✅ Enabled | Admins should follow the same process |
| **Restrict who can push to matching branches** | Enabled (admins only) | Prevents accidental direct pushes |
| **Do not allow bypassing the above settings** | ✅ Enabled | No emergency merges without checks |

### 5. Save and Verify

1. Click **Save changes**
2. Create a test PR from a feature branch
3. Verify that all 11 status checks appear as required
4. Verify merge button is disabled until all checks pass

## Troubleshooting

### Status check not appearing

- Ensure CI has run on `main` at least once (push a commit if needed)
- Check the exact job name in `.github/workflows/ci.yml` — names are case-sensitive
- Verify the workflow file is valid YAML

### Check shows as "expected" but never completes

- The CI job may be stuck — check Actions tab for errors
- Permissions issue: ensure workflow has `contents: read` and `security-events: write`

### Want to temporarily disable a check

1. Edit the branch protection rule
2. Remove the specific status check from required list
3. Save
4. Re-add when ready

**Do NOT** disable all checks — this defeats the purpose of security gating.

## Automation (Future)

Branch protection can be managed via Terraform or GitHub CLI in the future:

```bash
# GitHub CLI — set branch protection
gh api repos/{owner}/{repo}/branches/main/protection \
  --method PUT \
  --input protection.json
```

Where `protection.json` contains the full branch protection configuration.
