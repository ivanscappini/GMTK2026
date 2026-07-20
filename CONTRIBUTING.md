# Contributing

Some useful guidelines to contribute to this project.

---

# Before You Start

1. Read the project's `README.md`
2. Make sure you can open and run the project successfully.

---

# Working on a Task

## 1. Sync your local repository

Always start from the latest `development` branch.

```bash
git switch development
git pull --rebase
```

## 2. Create a feature branch

Create your branch from `development`.

```bash
git switch -c feature/my-feature
```

---

## Branch Naming Convention

Use the following format:

```text
<type>/<task-name>
```

### Branch Types

| Prefix | Purpose | Example |
|---------|---------|---------|
| `feature/` | New features | `feature/player-dash` |
| `fix/` | Bug fixes | `fix/audio-volume` |
| `refactor/` | Code cleanup or restructuring | `refactor/input-system` |
| `docs/` | Documentation | `docs/update-readme` |

### Guidelines

- Always branch from `development`.
- Use lowercase names.
- Separate words with hyphens (`-`).
- Keep branch names short but descriptive.

---

# During Development

Choose the workflow you prefer.

---

## Option 1: Git (CLI)

### Keep your branch up to date

Rebase regularly to minimize merge conflicts.

```bash
git switch development
git pull --rebase

git switch feature/my-feature
git rebase development
```

If conflicts occur:

```bash
git add <file>
git rebase --continue
```

To cancel the rebase:

```bash
git rebase --abort
```

---

## Option 2: GitHub Desktop

### Keep your branch up to date

1. Commit or stash any local changes.
2. Switch to the `development` branch.
3. Click **Fetch origin**.
4. Click **Pull origin**.
5. Switch back to your feature branch.
6. Go to **Branch → Rebase current branch...**
7. Select **development**.
8. Resolve any conflicts if prompted.
9. If GitHub Desktop asks you to, click **Force Push**.

---

# Commit Guidelines

Use concise and descriptive commit messages.

```text
feat: add enemy spawning
fix: resolve camera clipping
refactor: simplify wall generation
docs: update setup instructions
```

---

# Pull Requests (checklist)

Before opening a Pull Request:

- The project builds successfully.
- Your changes have been tested.
- There are no obvious errors or warnings.
- Your branch has been rebased onto the latest `development`.

### Git (CLI)

```bash
git push origin feature/my-feature
```

### GitHub Desktop

Simply click **Push origin** after committing your changes.

Then open a Pull Request targeting **`development`**.

---

# Before Merging a Branch

## Git (CLI)

```bash
git switch development
git pull --rebase

git switch feature/my-feature
git rebase development

git push --force-with-lease
```

## GitHub Desktop

1. Switch to `development`.
2. Click **Pull origin**.
3. Switch back to your feature branch.
4. Select **Branch → Rebase current branch...**
5. Choose **development**.
6. Resolve conflicts if needed.
7. Click **Force Push** if prompted.

# Housekeeping

After your Pull Request has been merged, clean up your local repository.

## Git (CLI)
```bash
git fetch --prune
git branch -vv | grep ': gone]' | awk '{print $1}' | xargs git branch -d
```

This removes local branches whose remote counterpart no longer exists.

---

# Useful Git Resources

- Git Cheat Sheet: https://education.github.com/git-cheat-sheet-education.pdf
