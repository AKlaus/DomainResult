## Project

`README.md` describes the two packages for consumers. A version tag triggers
`.github/workflows/release.yml`, which publishes the packages: never push a tag unless the
maintainer asks.

## Communication style

Applies to all prose you write: chat replies, commit and PR text, review comments, code comments,
and documentation. Inspired by ASD-STE100 Simplified Technical English.

- Lead with the answer or the decision. No preamble, no restating the request.
- One idea per sentence, at most 20 words. Put the condition first.
- Active voice.
- One term per concept within a document. Do not rotate synonyms for variety.
- Use `must`, `can`, `will` for requirements. "Should" reads as optional. Hedge only for genuine
  uncertainty, and give the reason.
- Use British English spelling in prose and in domain-owned identifiers. Keep US spelling in
  external API and framework identifiers. Never restyle code, identifiers, paths, quoted output,
  target framework monikers, or HTTP status titles.
- Avoid "leverage", "utilize", "in order to", "prior to", "enables you to", "powerful",
  "comprehensive", "seamlessly", and "simply". Write "what it does", not "functionality". Drop
  "it is worth noting that" and "it's important to", and state the fact.
- A chat reply has three short paragraphs of prose or fewer. Code, tables, and lists that the
  user asked for do not count. Skip the recap of work that succeeded as described. Always report
  failures, skipped scope, and assumptions. A skill's own report format wins.
- In `README.md`, keep its voice and headings.

## Reuse before adding

Before you write a helper, extension method, overload, or factory method, search the repository
by the domain term, not by the name you have in mind. If you still write your own, record why the
existing one does not fit. A near-duplicate must match the original's guard clauses, null
handling, default HTTP codes, and `ProblemDetails` fields.

## Naming

- A name describes the symbol, not a caller's use of it.
- One concept has one term everywhere, comments, test titles, and README text included. A value
  keeps its name across a call.
- Do not reuse a term that nearby code uses for another concept, such as "error" for both a
  `DomainOperationStatus` and an exception.
- A rename finishes in the same change, README examples included.

## Placement

Group files by feature, not by technical role, as `src/Mvc/ActionResult/` and
`src/Common/Exceptions/` do.

- Code that several features use lives in the feature that owns its concept. If no feature owns
  it, it sits in a `Helpers/` folder in the nearest folder that holds all its callers.
- Code that both packages need sits in `src/Common`.
- Each conversion has its own file in `src/Mvc/ActionResult/` or `src/Mvc/IResult/`, such as
  `DomainResultTo200OkResult.cs`. A file of the `static partial class DomainResultExtensions` is
  named after the conversion it holds.
- A namespace matches its folder, except for public extension methods. They stay in the package's
  root namespace, so one `using` brings in all of them, and their files keep
  `// ReSharper disable once CheckNamespace`.
- Order members as: public members, private helper methods, private types.
- C# files use tabs and CRLF line endings, as `.editorconfig` sets.

## Packages and dependencies

| Package ID | Project | Assembly and namespace |
|---|---|---|
| `DomainResult.Common` | `src/Common` | `DomainResults.Common` |
| `DomainResult` | `src/Mvc` | `DomainResults.Mvc` |

- `DomainResult.Common` must never reference `Microsoft.AspNetCore.*`, directly or through
  another package.
- Avoid a new `PackageReference` in `src/`. If a PR adds one, its description names the package
  and the reason.
- Never drop a target framework to make code compile.
- A change of target frameworks is its own PR. It touches the project files, both workflow files,
  and the target list in `README.md`.

## Public API

- MinVer computes versions from `v<major>.<minor>.<patch>` tags.
- Besides a signature change, a changed default in `HttpCodeConvention` and a changed
  status-to-HTTP-code mapping are breaking. The PR description marks each breaking change
  **Breaking**.
- Remove an `[Obsolete]` member no earlier than the next major version.

### Parity across result shapes

`README.md` lists the result shapes, under the `'DomainResult.Common' package` heading, and the
conversions, under the `'DomainResult' package` heading.

- A new status or behaviour reaches every shape and conversion where it applies, or the PR
  description says which one it leaves out and why.
- A new `DomainOperationStatus` value needs all of these: its factory methods, its HTTP code and
  title in `HttpCodeConvention`, its mapping in the `IActionResult` and the `IResult` converters,
  a sample in `samples/WebApi` and in `samples/WebApiMinimal`, tests for each shape, and a row in
  the status table of `README.md`.

## XML documentation

- Document every public type and member: the docs ship in the package. `Directory.Build.props`
  suppresses CS1591, so the build does not report a missing doc comment.
- The `<summary>` is one short statement. Caveats go in `<remarks>`.
- A factory method's `<summary>` names the `DomainOperationStatus` it returns and its default
  HTTP code, as `IDomainResult.cs` does. Docs in `src/Common` say nothing else about ASP.NET Core.
- Add `<param>` and `<returns>` only when they add value. A method's `<param>` tags cover every
  parameter or none.
- Implementations use `<inheritdoc/>`, with `<remarks>` only for contract-relevant differences.

## README and samples

- `README.md` ships on nuget.org as the package documentation. When public behaviour changes,
  update its examples, method lists, and status table. Each changed snippet must compile against
  the current API.
- `samples/` shows each status through a controller (`samples/WebApi/Controllers/`) and a
  minimal-API route (`samples/WebApiMinimal/Routes/`). Keep the two in step.

## Testing

- Test files mirror the source folders: `tests/DomainResults.Tests/Common/` and
  `tests/DomainResults.Tests/Mvc/`.
- Run at least `dotnet test --framework net10.0` locally. CI runs every target framework.
- A new test method is named `<Method>_<Scenario>_<ExpectedResult>`, with `GIVEN`, `WHEN`, and
  `THEN` comments. In an older test file, follow its existing names. Rename them only in a
  separate refactoring PR.
- Each test case, including each `[Theory]` row, covers a branch, boundary, or contract that no
  other case covers. Cases that differ only in input and expected result are one `[Theory]`.
- Cover each result shape and conversion that the change touches.
- Assert the specific status, HTTP code, and `ProblemDetails` fields.
- Test a conversion through its public extension method. Do not mock the library's own types.
- Do not test input that production cannot produce.

## Pull request scope

A PR does one thing: one behaviour, one refactor, one dependency upgrade, or one mechanical move.
Plan the slices before you write code, and open the PR at a slice boundary.

**Budget.** The `scope` action of the `gh-pr-review` skill measures a change in changed lines.
Its `references/scope.md` defines which lines count, and the exemptions from the ceiling.

| Lines | Target | Ceiling |
|---|---|---|
| Production | 500 | 1000 |
| Production and tests | none | 2000 |

- If a PR is over the target, its description states why the change cannot be split.
- If a PR is over the ceiling, split it, or name an exemption.
- The PR carries the tests for the behaviour it adds or changes.
- A behaviour change carries no refactor, reformat, or unrelated fix. Land a preparatory
  refactor first, as its own PR.

**Ship together.** These parts of a change go in one PR:
- A public API change, its `README.md` update, and its samples.
- A new `DomainOperationStatus` value and every item that **Parity across result shapes** lists.

**Split in this order.**
1. Preparatory refactors, moves, and renames, with no behaviour change.
2. The smallest consumer-visible increment, with the parts that ship together.
3. If one behaviour is over the budget, split it by package, inner package first: `src/Common`,
   then `src/Mvc`. The parts that ship together cannot split.

## Branches, commits, and pull requests

- Work starts from a GitHub issue, on a `feature/#<issue>` branch from `master`. A PR targets
  `master`.
- A commit subject and a PR title read `Area: imperative summary`, under 60 characters, with the
  area of the code that the change is about:

  | Area | Code |
  |---|---|
  | `Common` | `src/Common`, the `DomainResult.Common` package |
  | `Mvc` | `src/Mvc`, the `DomainResult` package |
  | `Samples` | `samples/` |
  | `Tests` | `tests/` only, with no change to a package |
  | `CI` | `.github/`, and build settings such as `Directory.Build.props` |
  | `Docs` | `README.md` only |
  | `Agents` | `AGENTS.md` and `.agents/` |

  A change to both packages takes the area of the package it is about, usually `Common`.
- For pull request work, such as creating, reviewing, sizing, or merging a PR, or addressing
  review comments, follow `.agents/skills/gh-pr-review/SKILL.md`.
