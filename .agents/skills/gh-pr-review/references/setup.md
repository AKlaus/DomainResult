# Setup Guide — gh-pr-review Skill

## 1. Create a GitHub token

Use a fine-grained personal access token.

1. Go to: `https://github.com/settings/personal-access-tokens/new`
2. Set a name (e.g. "AI PR Review") and an expiration
3. **Resource owner**: `AKlaus`
4. **Repository access**: **Only select repositories** → `DomainResult`
5. **Repository permissions**:
    - **Pull requests → Read and write** (list, review, comment, reply, vote, create, retarget,
      stack, publish, and resolve a general comment)
    - **Contents → Read and write** (`fetch`, the merge base, the head commit, `resolve` and
      `unresolve` on a review thread, and `complete`). GitHub's `resolveReviewThread` and
      `unresolveReviewThread` need it although they change no content; without it they fail with
      "Resource not accessible by personal access token". Write access also lets the token push
      branches and merge PRs. **Read-only** is enough if you never resolve a review thread or
      complete a PR.
    - **Issues → Read-only** (`issue`, which reads the issue a PR references)
    - **Metadata → Read-only** (GitHub selects it by itself)
6. Click **Generate token** and copy it immediately — it won't be shown again

A contributor without write access to `AKlaus/DomainResult` can still read, comment on, and
review PRs with a token for their own account. GitHub refuses the write actions, such as `resolve`,
`publish`, and `complete`, with `403` or `404`.

A classic token also works. It needs the `public_repo` scope.

Permissions can be edited on a fine-grained token later; a classic token's scopes can too.

---

## 2. `PROJECT_CFG.md` (shared with team)

The file sits in the skill directory (`.agents/skills/gh-pr-review/`) and is committed to Git.
It sets `GITHUB_REPOSITORY` to `<owner>/<repo>`, as in `https://github.com/<owner>/<repo>`. It
needs no change for this repository.

---

## 3. Create `PROJECT_CFG.Local.md` (private, never committed)

Place this file in the skill directory (`.agents/skills/gh-pr-review/`). **Never commit it.**

```bash
# PROJECT_CFG.Local.md — private secrets, DO NOT COMMIT
GITHUB_TOKEN=your-github-token-here
```

The scripts read the token from this file only. Do not export `GITHUB_TOKEN` in your shell
instead: GitHub Copilot CLI and other tools read that variable too.

### Check that Git ignores it

The root `.gitignore` excludes `.agents/**/*.Local.md`. Check it:
```bash
git check-ignore -v .agents/skills/gh-pr-review/PROJECT_CFG.Local.md
# Should output: .gitignore:N:.agents/**/*.Local.md  .agents/skills/gh-pr-review/PROJECT_CFG.Local.md
```

### Optional: share the token across linked worktrees

To avoid copying the private file into each worktree, find the repository's common Git directory:

```bash
git rev-parse --path-format=absolute --git-common-dir
```

Create `github/PROJECT_CFG.Local.md` under the returned directory. Every linked worktree
uses this file. Git cannot stage files under its common directory.

The skill-local file takes precedence when both files exist.

### Claude Code

Claude Code finds skills in `.claude/skills/`. To use this skill there, link the skill folder:

```bash
mkdir -p .claude/skills && ln -s ../../.agents/skills/gh-pr-review .claude/skills/gh-pr-review
```

Keep `.agents/skills/_shared` reachable: the scripts find `_shared` from their physical path.

---

## 4. Pre-approve execution patterns

If your agent runtime supports command pre-approval, allow the repo-root wrapper used by this skill:

```text
bash .agents/skills/gh-pr-review/scripts/run.sh *
```

Agents must invoke the wrapper from the repository root, for example:

```bash
bash .agents/skills/gh-pr-review/scripts/run.sh list
```

---

## 5. Verify Setup

Test your token and config by running from the repository root:

```bash
bash .agents/skills/gh-pr-review/scripts/run.sh list
```

Expected output: a JSON array of open PRs (or `[]` if none are open). A `404` usually means the
token cannot see the repository; see section 1.

---

## 6. Dependencies

The scripts require:
- `curl` — available by default on macOS and most Linux distros
- `jq` 1.7 or newer — macOS 15 and later ship 1.7.1 as `/usr/bin/jq`; otherwise `brew install jq`
  (macOS), `apt install jq` (Linux; Ubuntu before 24.04 installs 1.6, which is untested), or
  `winget install jqlang.jq` (Windows). The scripts are tested with jq 1.7.1 and 1.8.1.
- `git` — for `scope` and `link`

Check with:
```bash
curl --version && jq --version
```

Python is not required — `fetch_file.sh` URL-encodes with jq's `@uri`.

On Windows, make sure jq's directory is on the `PATH` that Git Bash inherits; a WinGet
install is not always picked up automatically.

---

## 7. Token Security Notes

- Treat your token like a password — anyone with it can act as you on GitHub
- Limit it to `DomainResult` and the permissions above
- Set a short expiration (30–90 days) and rotate regularly
- Never paste your token in chat, code comments, or PR descriptions
- If you accidentally commit it, revoke it immediately in the token settings above
