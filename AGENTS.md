@.agents/rules/rules.library.md

## Project

`README.md` describes the two packages for consumers. `rules.library.md` gives the rules for
changing them. A version tag triggers `.github/workflows/release.yml`, which publishes the
packages: never push a tag unless the maintainer asks.

## Communication style

Applies to all prose you write: chat replies, commit and PR text, review comments, code comments,
and documentation. Follow ASD-STE100 Simplified Technical English.

- Lead with the answer or the decision. No preamble, no restating the request.
- One idea per sentence, at most 20 words. Put the condition first.
- Active voice.
- One term per concept within a document. Do not rotate synonyms for variety.
- Use `must`, `can`, `will` for requirements. "Should" reads as optional. Hedge only for genuine
  uncertainty, and give the reason.
- Use British English spelling in prose and in domain-owned identifiers. Keep US spelling in
  external API and framework identifiers. Never restyle code, identifiers, paths, quoted output,
  target framework monikers, or HTTP status titles.
- Delete on sight: "leverage" and "utilize" (write "use"), "in order to", "prior to",
  "functionality", "enables you to", "powerful", "comprehensive", "seamlessly", "simply",
  "it is worth noting that", "it's important to".
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
- A namespace matches its folder. **Code layout** in `rules.library.md` gives the exception.

## Pull request scope

A PR does one thing: one behaviour, one refactor, one dependency upgrade, or one mechanical move.
Plan the slices before you write code, and open the PR at a slice boundary.

**Budget.** The `scope` action of the `gh-pr-review` skill measures a change in changed lines.
Its `references/scope.md` defines which lines count.

| Lines | Target | Ceiling |
|---|---|---|
| Production | 500 | 1000 |
| Production and tests | none | 2000 |

- If a PR is over the target, its description states why the change cannot be split.
- If a PR is over the ceiling, split it, or name an exemption from `references/scope.md`.
- The PR carries the tests for the behaviour it adds or changes.
- A behaviour change carries no refactor, reformat, or unrelated fix. Land a preparatory
  refactor first, as its own PR.

**Split in this order.**
1. Preparatory refactors, moves, and renames, with no behaviour change.
2. The smallest consumer-visible increment, with the parts that **Pull request slices** in
   `rules.library.md` ships together.
3. If one behaviour is over the budget, split it by package, inner package first: `src/Common`,
   then `src/Mvc`. The slices in `rules.library.md` cannot split.

## Branches and issues

Work starts from a GitHub issue, on a `feature/#<issue>` branch from `master`. A PR targets `master`.
