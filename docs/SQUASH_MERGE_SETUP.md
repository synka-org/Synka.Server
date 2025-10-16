# Squash Merge Configuration

## Configuring GitHub Repository for Squash Merging

To enforce squash merging for all future pull requests, follow these steps:

### 1. Repository Settings (Required)

1. Go to: <https://github.com/synka-org/Synka.Server/settings>
2. Scroll down to "Pull Requests" section
3. **Uncheck** "Allow merge commits"
4. **Uncheck** "Allow rebase merging"
5. **Check** "Allow squash merging" (should be the ONLY option checked)
6. **Check** "Always suggest updating pull request branches"
7. Click "Save"

### 2. Default Commit Message (Recommended)

In the same "Pull Requests" section:

- Set "Default to pull request title and description" for squash merge commits
- This ensures PR title becomes the commit message

### 3. Branch Protection Rules (Optional but Recommended)

1. Go to: <https://github.com/synka-org/Synka.Server/settings/branches>
2. Add rule for `main` branch:
   - ✅ Require pull request reviews before merging
   - ✅ Require status checks to pass before merging
   - ✅ Require branches to be up to date before merging
   - ✅ Include administrators

### Benefits of Squash Merging

✅ **Clean History**: Each PR becomes exactly one commit in `main`  
✅ **Easy Revert**: Revert entire features with a single command  
✅ **Readable Log**: `git log` shows features, not implementation details  
✅ **No Merge Commits**: Linear history without merge bubbles  

### Workflow After Configuration

```bash
# Developer workflow (no changes)
git checkout -b feat/my-feature
# ... make multiple commits ...
git push origin feat/my-feature
# Create PR, get reviews

# Maintainer merges via GitHub UI
# GitHub automatically squashes all commits into one
# Developer can then:
git checkout main
git pull
# main now has one clean commit per merged PR
```

### Example: Before vs After

**Before (multiple commits per PR):**

```text
* a1b2c3d feat: add feature X
* e4f5g6h fix: typo in feature X
* i7j8k9l refactor: improve feature X
* m0n1o2p docs: update feature X docs
* q3r4s5t fix: linting issues
```

**After (squash merge):**

```text
* a1b2c3d feat: add feature X (#42)
```

### Commit Message Format

When squashing, ensure PR titles follow Conventional Commits:

```text
feat: add new feature
fix: resolve bug in feature
docs: update README
refactor: improve code structure
test: add missing tests
ci: update GitHub Actions workflow
style: fix formatting issues
chore: update dependencies
```

## Configuration Complete

Once these settings are applied, all future PRs will automatically be squashed into single commits when merged to `main`.
