# Branch Protection Setup — Phase 22

## Prerequisites

- [x] SAST jobs exist in `.github/workflows/ci.yml`
- [x] SAST jobs have run at least once on `main` (required for GitHub to list them as selectable checks)
- [ ] Repository admin access

## Manual Steps Required

The `gh` CLI is not available, so branch protection must be configured manually via GitHub UI.

### Step 1: Push SAST Changes to `main`

```bash
git add .github/workflows/ci.yml .github/codeql/codeql-config.yml .semgrep/ .semgrepignore
git commit -m "feat: add SAST pipeline (Semgrep + CodeQL)"
git push origin main
```

Wait for the CI workflow to complete on `main`. This registers the status checks with GitHub.

### Step 2: Configure Branch Protection

1. Open repository in browser: `https://github.com/slipalison/simulator-ccb/settings`
2. Navigate to **Branches** (left sidebar)
3. Click **Edit** on the `main` branch protection rule (or **Add rule** if none exists)
4. Configure:

   **Branch name pattern:** `main`

   **Protect matching branches:**
   - ✅ Require a pull request before merging
     - Required approvals: **1**
     - ✅ Dismiss stale pull request reviews when new commits are pushed
     - ✅ Require review from Code Owners (if CODEOWNERS exists)
   - ✅ Require status checks to pass before merging
     - Search and add these exact status checks:
       - `Backend (.NET 10)`
       - `Frontend Client (Vinxi)`
       - `Frontend Backoffice (Vinxi)`
       - `SAST — Semgrep`
       - `SAST — CodeQL`
     - ✅ Require branches to be up to date before merging
   - ✅ Include administrators (recommended — no one bypasses security)
   - ✅ Restrict who can push to matching branches (optional — limit to maintainers)

5. Click **Save changes**

### Step 3: Verify

1. Create a test branch: `git checkout -b test/branch-protection`
2. Make a trivial change and push: `git push origin test/branch-protection`
3. Open a PR targeting `main`
4. Verify that all 5 status checks appear under **"Required checks"** in the PR
5. Verify the **Merge** button is disabled until all checks pass
6. Close the test PR without merging

### Step 4: Via GitHub API (Alternative)

If you prefer API, here's the curl command (replace `TOKEN` with a Personal Access Token with `repo` scope):

```bash
curl -X PUT \
  -H "Authorization: token TOKEN" \
  -H "Accept: application/vnd.github+json" \
  https://api.github.com/repos/slipalison/simulator-ccb/branches/main/protection \
  -d '{
    "required_status_checks": {
      "strict": true,
      "contexts": [
        "Backend (.NET 10)",
        "Frontend Client (Vinxi)",
        "Frontend Backoffice (Vinxi)",
        "SAST — Semgrep",
        "SAST — CodeQL"
      ]
    },
    "required_pull_request_reviews": {
      "required_approving_review_count": 1,
      "dismiss_stale_reviews": true
    },
    "restrictions": null,
    "enforce_admins": true,
    "required_linear_history": false,
    "allow_force_pushes": false,
    "allow_deletions": false
  }'
```

## Status Check Names

GitHub derives status check names from the `name:` field in the workflow job:

| Workflow Job `name:` | GitHub Status Check Name |
|----------------------|-------------------------|
| `Backend (.NET 10)` | `Backend (.NET 10)` |
| `Frontend Client (Vinxi)` | `Frontend Client (Vinxi)` |
| `Frontend Backoffice (Vinxi)` | `Frontend Backoffice (Vinxi)` |
| `SAST — Semgrep` | `SAST — Semgrep` |
| `SAST — CodeQL` | `SAST — CodeQL` |

If the names don't match exactly, GitHub won't find them in the status check search. Use the exact names above.
