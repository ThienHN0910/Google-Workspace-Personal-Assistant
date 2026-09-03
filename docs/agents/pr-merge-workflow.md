# Pull Request & Merge Workflow

Guidelines for agents and contributors to create issues, open pull requests, and safely merge code into `main` for the G-Ops Hub project.

---

## 1. Zero-Leakage Security & Secrets Hygiene (CRITICAL)

Before creating any commit, pushing to a remote branch, or opening a PR:
- **Scan Git Diff**: Run `git diff` or `git diff origin/main` to guarantee that no sensitive credentials (e.g. Gemini API keys, Google client secrets, MongoDB connection strings, JWT secrets) are written in code, tests, markdown, or scripts.
- **Verify .gitignore**: Ensure `.env`, `**/.env`, `**/.env.*`, `*.pem`, and `*.key` are strictly excluded from version control.
- **Environment Variables**: All configurations must use sanitized `.env.example` placeholders with zero real secrets.

---

## 2. Issue Creation

Every non-trivial piece of work should be tracked in GitHub Issues:
```bash
gh issue create \
  --title "feat(ux): Command Center UX overhaul and real-time alerts" \
  --body "## Context\n...\n## Scope\n- Task 1\n- Task 2\n## Verification\n- Tests pass" \
  --label "triage:feature"
```

---

## 3. Local Verification Standards

Before pushing or opening a PR, the following checks MUST pass cleanly:
1. **Backend**:
   ```bash
   cd src/backend
   dotnet test
   dotnet build
   ```
2. **Frontend**:
   ```bash
   cd src/frontend
   npm run build
   ```

---

## 4. Creating a Pull Request

Push the feature branch and create a PR referencing the issue:
```bash
git push -u origin feat/<branch-name>

gh pr create \
  --base main \
  --head feat/<branch-name> \
  --title "feat: <title>" \
  --body "## Summary\n...\n## Linked Issues\nCloses #<issue-number>\n\n## Verification Checklist\n- [x] dotnet test passed\n- [x] npm run build passed\n- [x] Secrets hygiene checked"
```

---

## 5. Merging into `main`

Once verification is confirmed:
```bash
# Merge PR (merge commit or squash) and remove remote feature branch
gh pr merge <pr-number> --merge --delete-branch

# Switch back to local main and pull updates
git checkout main
git pull origin main
```
