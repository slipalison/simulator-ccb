## Description

<!-- Describe the changes in this PR. Link to related issues. -->

## Type of Change

- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Refactoring (no functional changes)

## Security Checklist

- [ ] SAST checks pass (Semgrep + CodeQL) — no new ERROR-level findings
- [ ] No new `// nosem` suppressions added without justification comment
- [ ] No hardcoded credentials, API keys, or tokens in this PR
- [ ] CSRF validation present for new POST/PUT/DELETE endpoints (or `// nosem` with justification if stateless API)
- [ ] No `localStorage` usage for authentication tokens (use memory or httpOnly cookies)
- [ ] No `dangerouslySetInnerHTML` usage (or sanitized with DOMPurify if required)

## Testing

<!-- Describe how you tested these changes. -->

## Checklist

- [ ] Code follows project conventions (linting, type checking)
- [ ] Tests added/updated and passing
- [ ] Coverage threshold met (>= 80%)
- [ ] Self-reviewed my code
- [ ] Comments added for complex logic
