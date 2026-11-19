---
name: gh-pr-review
description: Work with GitHub pull requests of the repository through a GitHub token. Covers pull requests: create, list, review, diff, read a referenced issue, comment, reply, submit a review (approve or request changes), retitle, redescribe, change the target branch, manage stacks of PRs, publish a draft, resolve threads, address the review comments on your own PR, complete (squash-merge) a PR, size a change against the PR budget and summarise its tangible changes. Trigger on requests naming a PR, pull request, repo, diff, review, review comments, PR size, a target branch, a stack, or completing or merging a PR.
---

# GitHub Pull Request Skill

## Execution rules

- Every GitHub call goes through `bash .agents/skills/gh-pr-review/scripts/run.sh <action>`, run
  from the repository root. Never call the API, the other scripts, or the config files directly.
  Run one action per Bash call.
- Local read-only Git is allowed: `git fetch origin pull/<n>/head`, `git diff`, and `diff` or
  `grep` over fetched files.
- Double-quote every ref, and pass it as `review` printed it. Do not URL-encode it.
- If an action fails, report the failure and its likely cause. For a missing or rejected token,
  point the user to `references/setup.md`.

## Workflows

- To review a PR, follow `references/review.md`. A review request runs the whole workflow,
  posting included, without further prompts, unless it asks for a dry run.
- To address the open comments on the developer's own PR, follow `references/address-comments.md`.
- Before you write a PR title or description, a comment, a reply, or a review report, read
  `references/writing.md`.
- Before `create` or `describe`, run `scope`.

## Actions

Each command below follows `bash .agents/skills/gh-pr-review/scripts/run.sh`.

| Goal | Command |
|---|---|
| List PRs | `list [open\|merged\|closed\|all]` (default `open`) |
| Create a PR | `create <source> <target> "Title" [description] [--ready]` |
| Read a PR: metadata, snapshot commits, files, threads, comments, reviews | `review <pr>` |
| Read an issue with its comments | `issue <issue>` |
| Size a change | `scope [base] [head]` |
| Fetch a file at a ref | `fetch path/to/file.cs "<ref>"` |
| Comment: general, file-level, or on a line | `comment <pr> "Text" [<commit> <file> [<line>]]` |
| Reply | `reply <PRRT_… or IC_…> "Text"` |
| Edit or delete your comment | `edit <IC_… or PRRC_…> "Text"`, `delete <IC_… or PRRC_…>` |
| Submit a review | `vote <pr> approve\|request-changes\|comment ["Body"]` |
| Update the description or title | `describe <pr> "Text"`, `title <pr> "Text"` |
| Change the target branch | `retarget <pr> <branch>` |
| Manage stacks | `stack list`, `stack show <pr>`, `stack create <pr> <pr> [...]`, `stack add <stack> <pr> [...]`, `stack unstack <stack>` |
| Publish a draft | `publish <pr>` |
| Squash-merge | `complete <pr>` |
| Resolve or reopen | `resolve <PRRT_… or IC_…>`, `unresolve <PRRT_… or IC_…>` |

### Behaviour that the commands do not show

- `create` makes a draft unless `--ready` is given. Put `--` before a description that starts
  with `--`. CI skips draft PRs, so a draft has no build result until `publish`.
- `comment` with a `<commit>` makes a review thread. Pass the full `sourceCommit` that `review`
  printed, so the comment stays on the code you read. GitHub refuses a line outside a diff hunk,
  and every file or line comment while your account has a pending review on the PR.
- A general comment (`IC_…`) has no thread. `reply` to it posts a new general comment that
  mentions the author, links the original, and quotes its first line. `resolve` hides it with
  the reason "Resolved", and `unresolve` shows it again. To answer a review comment (`PRRC_…`),
  reply to its thread (`PRRT_…`).
- `vote request-changes` and `vote comment` need a body.
- `scope` reads the local clone only and needs no token. `base` defaults to `origin/master`.
- `issue` refuses a pull request number.
- GitHub refuses `retarget` on a PR in a stack: `stack unstack`, `retarget`, then `stack create`
  again. `stack create` and `stack add` take PRs bottom first, and each PR must already target the
  head branch of the one below.
- `complete` runs only when the developer asks to complete or merge the PR. The squash commit
  message is a copy of the PR description, so check the description first, as step 6 of
  `references/address-comments.md` describes. `complete` refuses a draft, a closed PR, a PR
  without a description, and a stacked PR that is not the bottom of its stack. It polls the merge
  for up to ten minutes, and running it again picks up a pending merge. After the merge, it deletes
  the branch on GitHub and locally. It keeps a branch that open PRs target, and a local branch
  that holds commits the PR did not merge. It then fast-forwards the local target branch. Its output reports each cleanup step in
  `remoteBranch`, `localBranch`, and `targetBranch`.

## Maintaining the scripts

After you change a file under `scripts/`, run the mocked suite from the repository root. It does
not contact GitHub.

```bash
bash .agents/skills/gh-pr-review/tests/run_tests.sh
```

The scripts must also pass on bash 3.2 (`/bin/bash`) and jq 1.7.1 (`/usr/bin/jq`), the macOS
defaults. Test cases live in `tests/cases/*.sh`, one file per area.
