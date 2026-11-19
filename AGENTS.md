@.agents/rules/rules.library.md

## Project

DomainResult is two NuGet packages that apply the Result Pattern to ASP.NET Web API apps.
`README.md` describes them for consumers, and `rules.library.md` gives the rules for changing them.

- `src/Common` builds `DomainResult.Common`, the result types for a domain layer.
- `src/Mvc` builds `DomainResult`, the conversions to `IActionResult` and `IResult`.
- `tests/DomainResults.Tests` holds the xUnit tests, and `samples/` holds example apps.
- Build with `dotnet build`, and test with `dotnet test`. CI (`.github/workflows/ci.yml`) runs
  the tests on each target framework.
- A `v<major>.<minor>.<patch>` tag triggers `.github/workflows/release.yml`, which publishes the
  packages. Never push a tag unless the maintainer asks.

## Communication style

Applies to all prose you write: chat replies, commit and PR text, review
comments, code comments, and documentation. Inspired by ASD-STE100 Simplified
Technical English.

**Structure**
- Lead with the answer or the decision. No preamble, no restating the request.
- One idea per sentence, at most 20 words. Condition first: "If the build fails, read the log."
- Active voice. "The converter returns 404", not "404 is returned by the converter".
- One term per concept within a document. Do not rotate check/verify/validate or
  config/settings for variety.
- Use `must`, `can`, `will` for requirements and instructions — "should" reads as optional.
  Hedge only for genuine uncertainty, and give the reason: "This probably fixes it —
  I have not reproduced the failure."
- Use British English spelling (`colour`, `behaviour`, `initialise`) in new prose and domain-owned identifiers. Preserve US spelling only for external API or framework identifiers; keep each API-facing abstraction consistent.

**Delete on sight**
- leverage, utilize → use
- in order to → to
- prior to → before
- functionality → what it does
- enables you to → you can
- powerful, comprehensive, seamlessly, simply → delete
- "it is worth noting that", "it's important to" → delete, state the fact

**Budgets** (targets; exceed one only with a one-line reason)
- Chat reply: three short paragraphs or fewer of prose. Code, diffs, tables and
  lists the user asked for do not count against this.
- No closing recap of work that succeeded as described. Always report failures,
  skipped scope, and assumptions you made.
- Where a skill defines its own report or comment format, that format wins.

**Do not restyle**

These are technical names, not prose: identifiers, code blocks, CLI commands, file
paths, quoted error and log lines, config keys, target framework monikers (`net10.0`),
C# generic type arguments, and HTTP status codes and titles.

`README.md` is consumer documentation that ships on nuget.org. When you edit it, keep its voice
and headings, and apply the rules above to the sentences you write.

## Reuse before adding

Search the repository for the concept before you write a helper, extension method, overload, or
factory method. Search the domain term, not the name you have in mind. The same idea often exists
elsewhere under another name.

- If an implementation exists, use it. `rules.library.md` says which package a shared one belongs
  in. Inside it, **Placement** below gives the folder.
- If you still write your own, record why the existing one does not fit.
- A near-duplicate must match the original's semantics. Compare guard clauses, null handling,
  default HTTP codes, and the `ProblemDetails` fields line by line.
- Silent semantic divergence is a bug, not a style choice. Two copies agree
  today and disagree after one is fixed.

## Naming

- A name describes the symbol, not a caller's use of it. A lookup that returns `null`
  holds no `fallback`.
- One concept has one term, in comments, test titles, and README text too. A value keeps
  its name across a call.
- Do not reuse a term that nearby code uses for another concept, such as "error" for both a
  `DomainOperationStatus` and an exception.
- A rename finishes in the same change: variables, helpers, test titles, comments, README
  examples, and file names. A rename of a public member is a breaking change, as
  `rules.library.md` says.

## Placement

Group files by feature, not by technical role. Layers are projects: `src/Common` and `src/Mvc`.
Inside a project, folders group files by feature, such as `src/Mvc/ActionResult/` and
`src/Common/Exceptions/`.

- A feature folder holds every file the feature needs.
- Code that one feature uses stays in that feature.
- Code that several features use lives in the feature that owns its concept, and the others use
  it from there.
- Code that several features use and no feature owns sits in the nearest folder that holds all
  its callers, in a `Helpers/` folder there, such as `src/Mvc/Helpers/`.
- Code that both packages need sits in `src/Common`, because `src/Mvc` references it. It must not
  pull an ASP.NET Core type into `src/Common`.
- A public extension method keeps the package's root namespace whatever its folder, as
  `rules.library.md` says.

## Pull request scope

A PR does one thing: one behaviour, one refactor, one dependency upgrade, or one mechanical move.
Plan the slices before you write code. When the work reaches a slice boundary, stop and open
the PR. The main reviewer is an LLM agent, and a small PR keeps its context window on one clear
goal. The maintainer who approves the PR must still understand the whole change.

**Budget.** `bash .agents/skills/gh-pr-review/scripts/run.sh scope` counts changed lines
(added plus deleted) and prints the budget result on stderr. Its output, the tangible changes,
goes into the PR description. The description never shows line counts or the budget result.

| Lines | Target | Ceiling |
|---|---|---|
| Production | 500 | 1000 |
| Production and tests | none | 2000 |

- Generated code, auxiliary files (such as `*.sln` and `*.DotSettings`), agent tooling (anything
  under `.agents/` or `.claude/`), Markdown (`README.md` included), and binary files do not count.
  Code in `samples/` counts as production.
- Comment lines and simple renames do not count either. A code line with a trailing comment
  counts. `.agents/skills/gh-pr-review/references/scope.md` defines a simple rename.
- If a PR is over the target, its description states why the change cannot be split.
- If a PR is over the ceiling, split it. The exemptions are in
  `.agents/skills/gh-pr-review/references/scope.md`, and the description must name one.

**Each PR is atomic and testable.**
- `master` builds, packs, and passes every test on every target framework after the merge.
- The PR contains the tests for the behaviour it adds or changes. Do not defer tests to a later PR.
- The PR contains the `README.md` and `samples/` updates for the public behaviour it adds or changes.
- A behaviour change carries no refactor, reformat, or unrelated fix. Land a preparatory
  refactor first, as its own PR.

**Split in this order.**
1. Preparatory refactors, moves, and renames, with no behaviour change.
2. The smallest consumer-visible increment: one behaviour in `src/Common`, its conversion in
   `src/Mvc`, its tests, its samples, and its README text.
3. If one behaviour is over the budget, split it by package, inner package first:
   `src/Common` with its tests, then `src/Mvc` with its tests, samples, and README text.
   A new `DomainOperationStatus` value cannot split: it ships whole, as `rules.library.md` says.

## Branches and issues

- Work starts from a GitHub issue. Its branch is `feature/#<issue>`, from `master`.
- A PR targets `master`, and its description references the issue as `#<issue>`.
- External contributors open PRs from forks. Review those as any other PR.
