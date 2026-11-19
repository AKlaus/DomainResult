# PR Scope

## The budget

`scope` counts changed lines, added plus deleted, against the target and the ceiling in
`AGENTS.md`. It prints only the result, on stderr. No description or report shows a line count.

The ceiling is twice the target, for a slice that is atomic but not small, such as a new
`DomainOperationStatus` value. Tests have no target, because a PR must carry its tests.

### What counts

`scripts/scope/classify.awk` puts each changed file in one category.

| Category | Files | Counts |
|---|---|---|
| Production | everything not below: `src/`, `samples/`, project files, and `.github/workflows/` | target and ceiling |
| Tests | `*.Tests/`, `Tests/`, and `tests/` folders | ceiling |
| Auxiliary | `*.sln`, `*.slnf`, `*.DotSettings`, `packages.lock.json`, settings such as `.gitignore` and `.editorconfig`, and anything under `.agents/` | no |
| Docs | every `*.md`, `README.md` and `AGENTS.md` included, and `docs/` | no |
| Binary | files that Git cannot count in lines, such as `DomainResult.snk` | no |

A moved file counts by its edits only. `scripts/scope/uncounted.awk` also leaves out:

- **Comment lines**: a line that holds only a comment, or a line inside a block comment, in C#,
  MSBuild and other XML files, and YAML. XML doc comments (`///`) are comment lines. A code line
  with a trailing comment counts.
- **Simple renames**: a deleted line and the added line that replaces it, if all of these are
  true:
  - The two lines differ only in identifiers. A changed number, operator, or argument list
    counts, and so does a name that changes only in its digits, such as `net8.0` to `net9.0`.
  - No replaced identifier is a keyword, a modifier, a literal, or a built-in type.
  - No replaced identifier remains in the new file, and no replacing identifier occurs in the old
    file.
  - Another line pair in the diff makes the same replacement. The comparison ignores case and
    the shared start and end of the names, so `NotFound` to `Gone` and `ToNotFoundResult` to
    `ToGoneResult` are the same replacement.

The rename check is a heuristic: a switch to another symbol that the old file did not use looks
like a rename. The reviewer reads every line.

### Exemptions from the ceiling

A PR over the ceiling names one of these in its description, and contains nothing else.

- **Mechanical**: a rename, a move, or a reformat, with no behaviour change. A rename of a public
  member is still breaking.
- **Removal**: dead code deleted, with only the edits that the deletion forces, such as the code
  for a dropped target framework.
- **Atomic**: a change that leaves `master` broken if it is split. The description says why each
  split fails.

## Split a change

- If slice B builds on slice A before A merges, branch B from A. Open B as a draft that targets
  A's branch, then `stack create <PR of A> <PR of B>`. Add a later slice with
  `stack add <stack> <PR of C>`.
- Complete A with `complete`. GitHub then retargets B to `master` and replays its commits onto
  the squash commit under new hashes. Fetch B's new head and check it with `git range-diff`
  before you build on it.
- Rebase every PR whose parent changed and that GitHub did not replay, such as one in a chain
  without a stack: `git rebase --onto <parent new tip> <parent old tip> <branch>`. If the parent
  merged, its new tip is the squash commit, and the PR also needs `retarget` to `master`. A plain
  rebase onto `origin/master` replays the parent's commits again.

## Run `scope`

`scope` reads committed changes only. Commit before you run it.

```bash
# Author: the branch against master
git fetch origin
bash .agents/skills/gh-pr-review/scripts/run.sh scope

# Author of a stacked slice: the branch against the parent slice
bash .agents/skills/gh-pr-review/scripts/run.sh scope "<parent branch>"

# Reviewer: the commits that `review` returns
git fetch origin pull/<n>/head
bash .agents/skills/gh-pr-review/scripts/run.sh scope <baseCommit> <sourceCommit>
```

The output is the **Changes at a glance** section of the description: a table of tangible changes
by kind, the **Areas** with changed production or test files, and one detail list per kind. Each
list stops at 15 rows.

Most changes of this library are to its public API, which `scope` cannot see. So the output often
reads "No tangible change that `scope` detects".

### What each kind detects

| Kind | Source | It cannot see |
|---|---|---|
| Project references | `ProjectReference` in each changed `*.csproj` | a reference that only a `Directory.Build.props` adds |
| Package dependencies | `PackageReference` in each changed production `*.csproj` and `Directory.Build.props`, and `PackageVersion` in `Directory.Packages.props` | a `FrameworkReference`; the test project's packages; a second, conditional version of one package, as `src/Directory.Build.props` pins for `net7.0` |
| API endpoints | verb and route attributes in each touched controller class: the sample controllers in `samples/WebApi/Controllers/` | the minimal-API routes in `samples/WebApiMinimal/Routes/` |
| Configuration keys | keys added to or removed from `appsettings*.json` in `samples/` | changed values |
| CI workflows | `.github/workflows/*.yml` | the effect of the change |

## Put it in the description

1. Paste the **Changes at a glance** section as `scope` prints it. Delete a row that is wrong.
2. Add what `scope` cannot see:
   - **Public API**: each public type or member that the PR adds, changes, or removes.
   - **Behaviour**: a changed HTTP code, `ProblemDetails` field, or `HttpCodeConvention` default
     behind an unchanged signature.
   - **Sample routes**: each minimal-API route in `samples/WebApiMinimal/Routes/` that the PR
     adds, changes, or removes.
   - **Target frameworks**: a `TargetFramework` or `TargetFrameworks` change.
   - **Package metadata**: a change to the package ID, description, tags, or version settings.
3. Mark each change that breaks a consumer as **Breaking**.
4. If the PR is over the target or the ceiling, add what **Pull request scope** in `AGENTS.md`
   asks for.

When a push changes the result, run `scope` again and update the description with `describe`.
