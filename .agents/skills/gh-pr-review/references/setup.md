# Setup — gh-pr-review Skill

## 1. Create a GitHub token

Create a fine-grained personal access token at `https://github.com/settings/personal-access-tokens/new`,
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

Put the token in `PROJECT_CFG.Local.md` in the skill folder:

```bash
GITHUB_TOKEN=your-github-token-here
```

The root `.gitignore` excludes `.agents/**/*.Local.md`. The scripts read the token from this file
only, and ignore an exported `GITHUB_TOKEN`.

To share one token across linked worktrees, put the file at `github/PROJECT_CFG.Local.md` under
the directory that `git rev-parse --path-format=absolute --git-common-dir` prints. The file in the
skill folder takes precedence when both exist.

## 3. Use the skill from Claude Code

Claude Code finds skills in `.claude/skills/`. Link the skill folder there:

```bash
mkdir -p .claude/skills && ln -s ../../.agents/skills/gh-pr-review .claude/skills/gh-pr-review
```

Keep `.agents/skills/_shared` in place: the scripts find it from their physical path.

To pre-approve the skill's commands, allow `bash .agents/skills/gh-pr-review/scripts/run.sh *`.

## 4. Check the setup

The scripts need `curl`, `git`, and `jq` 1.7 or newer. Run from the repository root:

```bash
bash .agents/skills/gh-pr-review/scripts/run.sh list
```

The output is a JSON array of open PRs, or `[]`. A `404` usually means that the token cannot see
the repository.
