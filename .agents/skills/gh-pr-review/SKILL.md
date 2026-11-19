---
name: gh-pr-review
description: Work with GitHub pull requests of the repository through a GitHub token. Covers pull requests: create, list, review, diff, read a referenced issue, comment, reply, submit a review (approve or request changes), retitle, redescribe, change the target branch, manage stacks of PRs, publish a draft, resolve threads, address the review comments on your own PR, complete (squash-merge) a PR, size a change against the PR budget and summarise its tangible changes. Trigger on requests naming a PR, pull request, repo, diff, review, review comments, PR size, a target branch, a stack, or completing or merging a PR.
---

# GitHub Pull Request Skill

Works with the repository's GitHub pull requests through the GitHub REST and GraphQL APIs. All
calls go through `scripts/run.sh`.

Work starts from a GitHub issue, and a PR references it as `#<issue>`. The `issue` action reads
one, so a review can check the PR against it.

---

## ⚠️ Execution Rules — Read Before Running Anything

- **Every GitHub operation goes through `scripts/run.sh`** — it is the only entry point to the API. Never hand-write `curl`/API calls, never call other `scripts/` directly, and never source config manually (`run.sh` handles auth, encoding, and config). Each API action is its own `run.sh` invocation; don't chain multiple actions into one Bash call.
- **Local, read-only analysis is allowed**: you may redirect a `fetch` to a temp file and run `diff`/`grep` over fetched files to reconstruct and study the change. You may also run `git fetch origin pull/<n>/head` and `git diff` locally. This never touches the API, so it's exempt from the `run.sh`-only rule above.
- **Always invoke from the repo root**: `bash .agents/skills/gh-pr-review/scripts/run.sh <action>`. Never `cd` into the skill directory first.
- For `fetch`, pass a branch or a commit SHA exactly as `review` returns it. A branch may be short or full (for example, `"refs/heads/bugfix/#912729"`); the script strips `refs/heads/` and URL-encodes special characters. To fetch an immutable review snapshot, pass a commit SHA from `review`'s SNAPSHOT. Always double-quote refs and never URL-encode them yourself.
- **If a `run.sh` action fails** (non-zero exit, auth error, or `404`/not-found), report the failure and its likely cause — expired or unapproved token → see `references/setup.md`; wrong PR number, thread or comment id, branch, or file path — rather than retrying blindly or fabricating a result.

---

## Step 1 — Load Configuration

### Shared config (team settings)
`PROJECT_CFG.md` — lives in the skill directory (`.agents/skills/gh-pr-review/`), committed to Git. Contains the repository as `<owner>/<repo>`:

```
GITHUB_REPOSITORY=AKlaus/DomainResult
```

### Private config (token — never committed)
Store the token in `.agents/skills/gh-pr-review/PROJECT_CFG.Local.md` and exclude it through
`.gitignore`. As an option for linked worktrees, use
`<git-common-dir>/github/PROJECT_CFG.Local.md`; every worktree shares that file.

```
GITHUB_TOKEN=your-github-token-here
```

If both files are missing, tell the user to follow `references/setup.md`.

See `references/setup.md` for full setup instructions.

---

## Step 2 — Choose an Action

Determine which action the user wants; if unstated, ask. Every action maps to a `run.sh` command in the Step 3 table below: `list`, `create`, `review`, `issue`, `scope`, `fetch`, `comment`, `reply`, `edit`, `delete`, `vote`, `describe`, `title`, `retarget`, `stack`, `publish`, `complete`, `resolve`, `unresolve`.

Two requests run a workflow rather than one action:

- To review a PR, follow `references/review.md`.
- To address the open comments on the developer's own PR, follow `references/address-comments.md`.

---

## Step 3 — Run the Action

| Goal                      | Command                                                                                                   |
|---------------------------|-----------------------------------------------------------------------------------------------------------|
| List open PRs             | `bash .agents/skills/gh-pr-review/scripts/run.sh list`                                                    |
| List by status            | `bash .agents/skills/gh-pr-review/scripts/run.sh list merged` (`open`, `merged`, `closed`, `all`)         |
| Create draft PR           | `bash .agents/skills/gh-pr-review/scripts/run.sh create <source> <target> "Title" [description]`          |
| Create ready PR           | `bash .agents/skills/gh-pr-review/scripts/run.sh create <source> <target> "Title" [description] --ready`  |
| Review a PR               | `bash .agents/skills/gh-pr-review/scripts/run.sh review <pr>`                                             |
| Read a referenced issue   | `bash .agents/skills/gh-pr-review/scripts/run.sh issue <issue>`                                           |
| Size a change             | `bash .agents/skills/gh-pr-review/scripts/run.sh scope [base] [head]`                                     |
| Fetch a file              | `bash .agents/skills/gh-pr-review/scripts/run.sh fetch path/to/file.cs "<ref>"`                           |
| General comment           | `bash .agents/skills/gh-pr-review/scripts/run.sh comment <pr> "Your comment"`                             |
| File-level comment        | `bash .agents/skills/gh-pr-review/scripts/run.sh comment <pr> "Your comment" <commit> path/to/file.cs`    |
| Line comment              | `bash .agents/skills/gh-pr-review/scripts/run.sh comment <pr> "Your comment" <commit> path/to/file.cs <line>` |
| Reply                     | `bash .agents/skills/gh-pr-review/scripts/run.sh reply <thread_id or general_comment_id> "Your comment"`  |
| Edit your comment         | `bash .agents/skills/gh-pr-review/scripts/run.sh edit <comment_id> "New text"`                            |
| Delete your comment       | `bash .agents/skills/gh-pr-review/scripts/run.sh delete <comment_id>`                                     |
| Approve                   | `bash .agents/skills/gh-pr-review/scripts/run.sh vote <pr> approve ["Body"]`                              |
| Request changes           | `bash .agents/skills/gh-pr-review/scripts/run.sh vote <pr> request-changes "Reason"`                      |
| Comment without a verdict | `bash .agents/skills/gh-pr-review/scripts/run.sh vote <pr> comment "Body"`                                |
| Update description        | `bash .agents/skills/gh-pr-review/scripts/run.sh describe <pr> "New description"`                         |
| Update title              | `bash .agents/skills/gh-pr-review/scripts/run.sh title <pr> "New title"`                                  |
| Change the target branch  | `bash .agents/skills/gh-pr-review/scripts/run.sh retarget <pr> <branch>`                                  |
| List or show stacks       | `bash .agents/skills/gh-pr-review/scripts/run.sh stack list` / `stack show <pr>`                           |
| Form or grow a stack      | `bash .agents/skills/gh-pr-review/scripts/run.sh stack create <pr> <pr> [...]` / `stack add <stack> <pr> [...]` |
| Unstack the unmerged PRs  | `bash .agents/skills/gh-pr-review/scripts/run.sh stack unstack <stack>`                                   |
| Publish a draft           | `bash .agents/skills/gh-pr-review/scripts/run.sh publish <pr>`                                            |
| Complete (squash-merge)   | `bash .agents/skills/gh-pr-review/scripts/run.sh complete <pr>`                                           |
| Resolve                   | `bash .agents/skills/gh-pr-review/scripts/run.sh resolve <thread_id or general_comment_id>`               |
| Reopen                    | `bash .agents/skills/gh-pr-review/scripts/run.sh unresolve <thread_id or general_comment_id>`             |

`<pr>` is the PR number. `<thread_id>` is a review thread node ID (`PRRT_…`). `<comment_id>` is a
general comment (`IC_…`) or a review comment (`PRRC_…`); the node ID decides which kind `edit`
and `delete` change. `review` prints all three. Paths are repository-relative, without a leading
`/`; the scripts strip one if present.

GitHub threads only review comments. A general comment stays a flat item in the conversation, so
`reply` and `resolve` treat its `IC_…` id the closest way GitHub allows:

| Action      | Review thread (`PRRT_…`)        | General comment (`IC_…`)                                                       |
|-------------|---------------------------------|--------------------------------------------------------------------------------|
| `reply`     | Nested reply in the thread      | New general comment that mentions the author, links the original, and quotes its first line |
| `resolve`   | Marks the thread resolved       | Hides the comment with the reason "Resolved"; it collapses and anyone can expand it |
| `unresolve` | Reopens the thread              | Shows the hidden comment again                                                 |

Hiding needs write access to the repository. Resolving or reopening a review thread needs
**Contents → Read and write**, as `references/setup.md` describes. `review` reports `isResolved`
for both kinds. To answer a review comment (`PRRC_…`), reply to its thread.

For `create`, source and target may be short branch names or full `refs/heads/...`
references. New PRs are drafts unless `--ready` is supplied. The flag may appear
before or after the optional description. Use `--` before a description that begins
with `--`.

`publish` takes a draft out of draft state, which requests its reviewers and notifies them. CI
(`.github/workflows/ci.yml`) skips draft PRs, so a draft has no build or test result until you
publish it. `review` reports
`isDraft`, so check there first: publishing a published PR changes nothing and still answers
`"isDraft": false`.

### Comment forms

| Form        | Arguments                            | Lands as                                                       |
|-------------|--------------------------------------|----------------------------------------------------------------|
| General     | `<pr> <text>`                        | A PR conversation comment (`IC_…`); flat, with no thread       |
| File-level  | `<pr> <text> <commit> <file>`        | A review thread on the whole file; the file must be in the PR  |
| Line        | `<pr> <text> <commit> <file> <line>` | A review thread on a line of that commit's version of the file |

`<commit>` is the full `sourceCommit` that `review` printed: the commit you read. A push after
`review` then cannot move the comment to another line; GitHub marks it outdated instead.

A line comment needs a line inside a diff hunk of the PR. GitHub refuses any other line with `422`,
and the script prints a hint. For a finding outside the diff, see "Findings outside the diff" in
`references/review.md`. GitHub also refuses every file or line comment with `422` while your
account has an unsubmitted (pending) review on the PR, for example one started in VS Code. Submit
or discard that review, or post a general comment with a permalink to the line.

### Review events for `vote`

| Event             | Meaning                         | Body                             |
|-------------------|---------------------------------|----------------------------------|
| `approve`         | Approve the PR                  | Optional                         |
| `request-changes` | Block the PR until it changes   | Required: state the reason       |
| `comment`         | Submit a review with no verdict | Required                         |

GitHub refuses `approve` and `request-changes` on a PR you opened. There is no reset: a later
review from you replaces your earlier verdict.

### Other actions

`scope` checks a change against the PR budget and counts its tangible changes: project
references, package dependencies, endpoints, contracts, routes, components and their size,
database objects, configuration keys, and infrastructure. It never prints line counts, and it
prints the budget result on stderr. It reads the local clone only, so it needs no token. `base`
defaults to `origin/master` and `head` to `HEAD`. Before `create` or `describe`, run it and read
`references/scope.md`; its output goes into the description.

`retarget <pr> <branch>` changes the PR's target branch; `<branch>` is a short name or a
`refs/heads/` ref. GitHub recomputes the diff against the new target and leaves the head branch
alone, so rebase the head onto the new target first when the two targets have diverged. It is how
a slice joins or leaves a chain of PRs (`references/scope.md`, "Split a change"): a PR that targets
another PR's branch shows only its own changes. GitHub refuses `retarget` on a PR that is in a
stack; `stack unstack` first, then retarget, then `stack create` again. `retarget` needs
**Pull requests → Read and write**.

`stack` reads and manages GitHub's stacks, its own objects over a chain of PRs. `stack show <pr>`
prints the stack that holds a PR, bottom first; `stack list` prints every stack. `stack create`
forms one from PR numbers given bottom first, and `stack add` appends PRs above a stack's top; both
require each PR to target the head branch of the one below, so `retarget` first. `stack unstack`
removes every unmerged PR; the PRs stay open with their targets unchanged. Merged and queued PRs
stay behind in a closed stack, so a stack whose bottom has merged survives holding it alone, and
GitHub dissolves a stack only when nothing remains. A chain that GitHub has not made a stack of
gets no automatic retarget or replay when its bottom merges: rebase the next PR onto the squash
commit and retarget it by hand.

`complete` squash-merges the PR. It runs only when the developer asks to complete or merge the PR.
The commit subject is the PR title with ` (#<n>)`, and the commit message is a copy of the PR
description. So before `complete`, check that the description still matches the code, as step 6 of
`references/address-comments.md` describes. `complete` refuses a draft, a closed or merged PR, a
PR without a description, and a stacked PR that is not the bottom of its stack, before it sends
the merge: GitHub merges a stacked PR together with every open PR below it, so complete the lower
PR first. The merge goes through GitHub's asynchronous merge API, the only one that accepts a
stacked PR: GitHub accepts the request at once, and `complete` polls its result, for up to ten
minutes, until it is merged or failed. It merges the head commit that it read, so a push after the
read makes GitHub cancel the merge. Branch protection and repository rules run only when the merge
runs, so a failed required check, a missing approval, or a conflict arrives as a failed result,
and the script prints the reason. A run that gives up while the merge is still pending says so;
running `complete` again picks the pending request up. The token needs **Contents → Read and
write**, as `references/setup.md` describes.

After the merge, `complete` deletes the PR's branch on GitHub and in the local clone, and
fast-forwards the local target branch, such as `master`. Run it from the repository root, so the
local steps find the clone. The deletion is a push with a lease on the merged head, so a branch
that moved after the merge stays. It keeps a branch that open PRs target, because the deletion may
close those PRs; after a stack merge GitHub retargets and replays the PRs above by itself, so the
branch is usually free by then (`references/scope.md`, "Split a change"). A branch that GitHub keeps, such as a fork's branch or the default branch,
stays in the clone too. It also keeps a local branch that holds commits the PR did not merge. It
never forces a switch or a non-fast-forward. The output
reports each step in `remoteBranch`, `localBranch`, and `targetBranch`, and stderr warns about a
step that kept or skipped something. A failed cleanup step never fails the run, because the merge
has already happened.

`issue <issue>` prints an issue's title, state, labels, body, and comments. It refuses a pull
request number, because GitHub numbers issues and PRs together; read a PR with `review`.

Before writing a PR title or description, review comment, reply, or review report, read
`references/writing.md` and `.agents/skills/_shared/references/text.md`.

For `review`, read `references/review.md` before analysing or posting findings. A request to
review a PR runs that whole workflow, posting included, without further prompts, unless it asks
for a dry run.

---

## Maintaining the Scripts

When changing files under `scripts/`, run the mocked regression suite from the repository root:

```bash
bash .agents/skills/gh-pr-review/tests/run_tests.sh
```

The suite uses fake token files and an in-process curl replacement. The `scope` cases build a
throwaway Git repository. The suite does not contact GitHub or mutate remote state.

Run the suites with the oldest supported versions too: bash 3.2 (macOS `/bin/bash`) and jq 1.7.1
(macOS `/usr/bin/jq`). jq 1.8 accepts expressions that 1.7 rejects, so a pass on 1.8 alone does
not show that the scripts run on a default macOS.

`.agents/skills/_shared/tests/harness.sh` is the harness: counters, assert helpers, and
`run_test`. `run_tests.sh` names the skill and sources it. The cases live in `tests/cases/*.sh`,
one file per area, each ending with the `run_test` calls for the cases above it. The harness also
runs the cases in `.agents/skills/_shared/tests/cases/`, which hold only cases for
`_shared/scripts`. Add an area of this skill as a new file in `tests/cases/` — the harness sources
whatever it finds.

---

## Reference Files

- `references/setup.md` — How to create a GitHub token, set up config files, configure `.gitignore`
- `references/api.md` — GitHub REST and GraphQL reference for each script
- `references/review.md` — Review analysis, comment, and report workflow
- `references/address-comments.md` — Address the review comments on your own PR: judge, fix, review, push, reply and resolve, and check the description
- `references/review-worksheet.md` — Scratch template that each review pass fills, step by step
- `references/writing.md` — PR titles, descriptions, comments, replies, reports, and references
- `references/scope.md` — PR size budget, exemptions, how to split, and how to use `scope` in a description or a review
