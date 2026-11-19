# Setup — gh-pr-review Skill

## 1. Create a GitHub token

If the GitHub CLI is signed in to an account with access to the repository, skip to section 3:
the scripts use its token.

Otherwise, create a fine-grained personal access token at `https://github.com/settings/personal-access-tokens/new`,
for resource owner `AKlaus` and only the `DomainResult` repository, with these permissions:

| Permission | Access | Needed by |
|---|---|---|
| Pull requests | Read and write | every PR action, and `resolve` on a general comment |
| Contents | Read and write | `fetch`, `review`, `complete`, and `resolve` or `unresolve` on a review thread |
| Issues | Read-only | `issue` |
| Metadata | Read-only | GitHub selects it by itself |

GitHub's `resolveReviewThread` mutation needs **Contents → Read and write** although it changes
no content. Without it, the call fails with "Resource not accessible by personal access token".
**Contents → Read-only** is enough if you never resolve a review thread or complete a PR.

A contributor without write access can still read, comment, and review with a token for their own
account. A classic token with the `public_repo` scope also works.

## 2. Store the token

`PROJECT_CFG.md` in the skill folder sets `GITHUB_REPOSITORY` and needs no change.

The scripts use the first token they find, in this order:

1. `PROJECT_CFG.Local.md` in the skill folder.
2. `github/PROJECT_CFG.Local.md` under the directory that
   `git rev-parse --path-format=absolute --git-common-dir` prints. Every linked worktree shares it.
3. The `GH_TOKEN` environment variable, then `GITHUB_TOKEN`.
4. The GitHub CLI, through `gh auth token`.

A file holds one line, and the root `.gitignore` excludes `.agents/**/*.Local.md`:

```bash
GITHUB_TOKEN=your-github-token-here
```

When the token comes from place 3 or 4, every action names the place on stderr. Such a token can
belong to another account, or lack a permission from section 1. The scripts never use the Git
credential helper, because its credential can belong to another account.

## 3. Pre-approve the commands

If your agent asks before it runs a command, allow `bash .agents/skills/gh-pr-review/scripts/run.sh *`.

## 4. Check the setup

The scripts need `curl`, `git`, and `jq` 1.7 or newer. Run from the repository root:

```bash
bash .agents/skills/gh-pr-review/scripts/run.sh list
```

The output is a JSON array of open PRs, or `[]`. A `404` usually means that the token cannot see
the repository.
