# PR Scope

Read this before `create` or `describe`, and before the size step of a review. `AGENTS.md`
**Pull request scope** states the budget and the split order. This file gives the reasons, the
exemptions, and the use of the `scope` action.

The main reviewer is an LLM agent, such as Claude, that runs the `review.md` workflow. A small PR
keeps its context window on one clear goal. The maintainer who approves the PR must still
understand it, so both read the description first.

The budget is measured in changed lines, but no report shows a line count. Both reviewers learn
more from what changed, such as the public API and the package dependencies, so `scope` reports
what it can detect and gives the budget result only on stderr.

## The budget

`scope` counts changed lines, added plus deleted, and prints only the result, on stderr. The
description does not show it: a reviewer reads what changed, and runs `scope` for the budget.

| Lines | Target | Ceiling |
|---|---|---|
| Production | 500 | 1000 |
| Production and tests | none | 2000 |

- **Within the target**: no remark is needed.
- **Over the target**: the description states, in one sentence, why the change cannot be split.
- **Over the ceiling**: split the PR. If it cannot be split, the description names one exemption
  from the list below and gives the reason.

### Why these numbers

- A review loads much more than the diff. `review.md` makes the agent read the rules, the whole
  of each touched method or file, every use site of each touched symbol, and the existing
  threads. Each extra line of diff brings more of that context with it.
- Model accuracy falls as the context grows, and falls further when the relevant fact sits in
  the middle of it ([Lost in the Middle](https://aclanthology.org/2024.tacl-1.9/);
  [Context Rot](https://research.trychroma.com/context-rot)). Anthropic treats context as a
  [finite attention budget](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents).
- Human review shows the same limit. A SmartBear study at Cisco found that reviewers find defects
  best in [200 to 400 lines at a time](https://smartbear.com/learn/code-review/best-practices-for-peer-code-review/).
  Google's guide calls [100 lines reasonable and 1000 lines usually too large](https://google.github.io/eng-practices/review/developer/small-cls.html).
- Past PRs of this library fit the target. The largest recent ones, such as #84 (exception
  extension methods) and #91 (a switch to `TypedResults` in the converters), stay within it.
- The ceiling is twice the target. It leaves room for a slice that is atomic but not small, such
  as a new `DomainOperationStatus` value that must reach every result shape and conversion.
- Tests have no target, because a PR must carry its tests. The 2000-line ceiling still stops a
  PR whose tests crowd the production change out of the review context.
- Generated code, auxiliary files, agent tooling, and Markdown do not count. The review does not
  analyse a solution file line by line, agent tooling is not shipped, and Markdown is prose.
  `README.md` ships as the package documentation, but the review reads it as prose, not code.
- Comment lines and simple renames do not count. A comment changes no behaviour, and a rename
  that touches many call sites would otherwise push a small change over the target. A simple
  rename can still hide a behaviour change; **What counts** names the limit.

### What counts

`scripts/scope/classify.awk` puts each changed file in one category.

| Category | Files | Counts |
|---|---|---|
| Production | everything not below: `src/`, `samples/`, project files, and `.github/workflows/` | target and ceiling |
| Tests | `*.Tests/`, `Tests/`, `tests/`, and the other test folder names that `classify.awk` lists | ceiling |
| Generated | `*.gen.ts` and `*.snap` | no |
| Auxiliary | `*.sln`, `*.slnf`, `*.DotSettings`, lockfiles, and settings such as `.gitignore` and `.editorconfig` | no |
| Experiments and tooling | anything under `.agents/`, `.claude/`, or `sandbox/`, and any `*ScratchPad*` folder | no |
| Docs | every `*.md`, `README.md` and `AGENTS.md` included, and `docs/` | no |
| Binary | files that Git cannot count in lines, such as `DomainResult.snk` | no |

A moved file counts by its edits only, as the GitHub diff shows it.

`scripts/scope/uncounted.awk` removes two more kinds of line from the production and test counts:

- **Comment lines**: a line that holds only a comment, or a line inside a block comment. A code
  line with a trailing comment counts. XML doc comments (`///`) are comment lines. The script knows
  the comment syntax of C#, YAML, and XML files such as `*.csproj` and `*.props`, among others.
- **Simple renames**: a deleted line and the added line that replaces it, if all of these are
  true:
  - The two lines differ only in identifiers. Whitespace does not matter. A changed number,
    operator, or argument list counts. So does a name that changes only in its digits, such as
    `net8.0` to `net9.0`.
  - No replaced identifier is a keyword, a modifier, a literal such as `true` or `null`, or a
    built-in type, in any case. A swap of `public` to `internal`, or `int` to `long`, counts.
  - No replaced identifier remains in the code of the new file. A `[Fact]` that became
    `[Theory]` counts while other tests in the file keep `[Fact]`.
  - No replacing identifier occurs in the code of the old file. A loop over `errors` that now
    loops over `messages` counts, because `messages` was already there.
  - Another such line pair in the diff makes the same replacement. The comparison ignores case
    and the shared start and end of the names, so `NotFound` to `Gone`, `notFoundTitle` to
    `goneTitle`, and `ToNotFoundResult` to `ToGoneResult` are the same replacement. A replacement
    made on one line only counts.

The rename check is a heuristic. It cannot tell a renamed symbol from a switch to another symbol
that the old file did not use. That switch still looks like a rename. The reviewer reads every
line, and judges a PR whose result looks wrong.

### Exemptions from the ceiling

A PR over the ceiling names one of these in its description. It contains nothing else.

- **Mechanical**: a rename, a move, or a reformat, with no behaviour change. A rename of a public
  member is still a breaking change, as `rules.library.md` says.
- **Removal**: dead code deleted, with only the edits that the deletion forces, such as the code
  for a dropped target framework.
- **Atomic**: a change that leaves `master` broken if it is split. The description says why each
  split fails. The reviewer judges the reason.

## Split a change

Follow the split order in `AGENTS.md` and the slice rules in `rules.library.md`.

- Each slice is a branch from `master`.
- If slice B builds on slice A before A merges, branch B from A. Open B as a draft that targets
  A's branch, then form the stack: `stack create <PR of A> <PR of B>`. Add a later slice with
  `stack add <stack> <PR of C>`.
- Complete A with `complete`, which merges the bottom of a stack alone; it refuses B while A is
  open. GitHub's stack-aware merge then retargets B to `master` and replays B's commits onto the
  squash commit by itself, with the same patches under new hashes, and `complete` deletes A's
  branch once no open PR targets it. Fetch B's new head and check it with `git range-diff`
  before you build on it. A PR that is not in the stack, such as one opened after the stack
  formed, keeps its old base and needs a rebase by hand: onto its parent's new tip, with the
  parent's old tip as the upstream, `git rebase --onto <parent new tip> <parent old tip> <C branch>`,
  never onto `origin/master`, or its PR shows the parent's changes again. If GitHub leaves B
  untouched, rebase it the same way onto the squash commit with A's merged head as the upstream.

## Run `scope`

`scope` reads the local clone. It never calls GitHub, so it needs no token.

```bash
# Author: the committed branch against master
git fetch origin
bash .agents/skills/gh-pr-review/scripts/run.sh scope

# Author of a stacked slice: the committed branch against the parent slice
bash .agents/skills/gh-pr-review/scripts/run.sh scope "<parent branch>"

# Reviewer: the commits that `review` returns
git fetch origin pull/<n>/head
bash .agents/skills/gh-pr-review/scripts/run.sh scope <baseCommit> <sourceCommit>
```

Uncommitted edits do not count. Commit before you run it.

The output is the **Changes at a glance** section of the description, in this order. The budget
result goes to stderr, not into it.

- A table that counts each kind of tangible change as added, changed, or removed. The table does
  not count projects.
- **Areas**: the top-level folders with changed production or test files, such as `src`, `tests`,
  and `samples`. Experiments and the root workspace files are not areas.
- One detail list per kind that has rows, which names each tangible change. Each list stops at
  15 rows and gives a count of the rest.

If no kind has rows, the section says "No tangible change that `scope` detects". That is usual
here: most changes of this library are to its public API, which `scope` cannot see.

### What each kind detects

`scope` also holds detectors for web apps, such as API endpoints, UI routes, and database
migrations. This repository has none of those files in `src/`, so only these kinds apply:

| Kind | Source | It cannot see |
|---|---|---|
| Project references | `ProjectReference` in each changed `*.csproj` | a reference that only a `Directory.Build.props` adds |
| Package dependencies | `PackageReference` in each changed production `*.csproj` and `Directory.Build.props` | a `FrameworkReference`, such as `Microsoft.AspNetCore.App`; the test project's packages; a second, conditional version of one package, as `src/Directory.Build.props` pins for `net7.0` |
| Configuration keys | keys added to or removed from `appsettings*.json` in `samples/` | changed values, which it never prints |
| Infrastructure files | `.github/workflows/*.yml` | the effect of the change |

`scope` cannot see these, so the description names them by hand:

- **Public API**: each public type or member that the PR adds, changes, or removes, in
  `DomainResult.Common` and in `DomainResult`.
- **Behaviour**: a changed HTTP code, `ProblemDetails` field, or default in `HttpCodeConvention`
  behind an unchanged signature.
- **Target frameworks**: a `TargetFramework` or `TargetFrameworks` change in a project file.
- **Package metadata**: a change to the package ID, description, tags, or version settings.

## Put it in the description

1. Paste the **Changes at a glance** section as `scope` prints it.
2. Add what `scope` cannot see, from the list above.
3. Mark each change that breaks a consumer as **Breaking**. Examples are a removed or renamed
   public member, a changed signature, a changed default HTTP code, and a dropped target framework.
4. Delete a row that is wrong.
5. If the PR is over the target, add the reason. If it is over the ceiling, name the exemption.

When a push changes the result, run `scope` again and update the description with `describe`.
