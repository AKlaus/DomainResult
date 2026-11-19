# Writing Conventions

Use these conventions for PR titles and descriptions, review comments and replies, and final
review reports. The `AGENTS.md` Communication style section governs the prose itself; these
conventions add what is specific to GitHub pull requests.
`.agents/skills/_shared/references/text.md` holds the title areas and the rules for multiline
content.

## Titles and descriptions

These rules apply to `create`, `title`, and `describe`.

**Title**: `Area: imperative summary`, under 60 characters, with an area from "Title areas" in
`.agents/skills/_shared/references/text.md`.
GitHub squash-merges, so the title becomes the commit subject, and GitHub appends ` (#<n>)` to
it. Keep it readable on its own. `complete` copies the description into the commit message, so
the description must still match the code when the PR merges.

**Description**: why the change is needed, what changed in outline, its tangible changes, and any
check that it needs. Nothing else. Do not enumerate the changed files; the diff already lists
them, and do not give line counts or the budget result. A PR over the target or the ceiling adds
the reason or the exemption. GitHub accepts up to 65,536 characters, but aim for a reviewer to
grasp the change in 30 seconds.

An agent ends the description with `_-- AI Assistant_` on a new line, as it ends a comment. It
adds no other attribution, such as a "Generated with" line, even when its tool proposes one.

**What changed** gives one bullet per part of the change: a bold name and a fragment of about ten
words. It is an outline, not an explanation. Leave out what **Changes at a glance** already shows,
such as package dependencies, and details the diff makes plain.

**How to verify** lists only the checks that this change needs and a reviewer would not run
anyway: a manual step in the app, or a command with arguments that this PR chose. Leave out a
step that every PR repeats, such as running the project's test suite. If no such check exists,
leave out the section.

Before `create` or `describe`, run `scope` and read `scope.md`. Paste its **Changes at a glance**
section, then add what `scope` cannot see, as `scope.md` describes. Run it again after a push that
changes the result. If the tables grow long, keep the rows a reviewer needs and replace the rest
with a count, as `scope.md` describes.

GitHub renders the description as GitHub Flavored Markdown, including `<details>` blocks, Mermaid
diagrams, and task lists. Format for scanning, not for volume: keep section headings at `###`.
Follow the skeleton below. Add a table only for three or more parallel facts, such as settings
changed or before-and-after behaviour; the tables that `scope` prints stay as they are. Use a
`- [ ]` task list only for follow-up work left out of the PR. Use a `>` call-out only for
something a reviewer must not miss, such as a **Breaking** change.
Fold long supporting detail, such as a full `scope` table, into a `<details>` block.

Reference issues and pull requests as "References" describes.

```markdown
### Why

A deleted record returned 404, so clients could not tell it from a mistyped ID.

### What changed

- **Common** — a `Gone` status, with factory methods for every result shape
- **Mvc** — `Gone` converts to `410 Gone` for `IActionResult` and `IResult`
- **Samples and README** — a 410 example in both sample apps and the status table

### Changes at a glance

No tangible change that `scope` detects.

**Areas**: `samples`, `src`, `tests`

**Public API** (not seen by `scope`)

- Added `DomainOperationStatus.Gone`
- Added `IDomainResult.Gone()`, `DomainResult.Gone()`, and their `GoneTask()` variants
- Added `HttpCodeConvention.GoneHttpCode` and `GoneProblemDetailsTitle`

### How to verify

Run `samples/WebApiMinimal` and call `GET /410`: the response is a `ProblemDetails` with status 410.

Closes #88. Follows #87.

_-- AI Assistant_
```

## References

These rules apply to PR descriptions, comments, replies, reviews, and edits. GitHub links `#12`
to a pull request or issue of this repository, and a bare commit SHA of the repository.
`PR 12` stays plain text. A description closes its issue with `Closes #<issue>`, which GitHub
acts on when the PR merges into `master`.

| Target | Write | Never |
|---|---|---|
| Pull request | `#12` | `PR 12`, `PR #12`, or a hand-built `[PR 12](url)` link |
| Issue | `#88`, or `Closes #88` for the issue the PR resolves | `issue 88`, or a hand-built link |
| Commit | A bare SHA, such as `7f50a06af3` (7 or more characters) | A link built by hand |

- Write `#12` as prose, not inside backticks. A code span is literal text.
- In a table cell, leave a space on each side of the reference.
- Outside code, escape a `#` that is not a reference, such as a hex colour: `\#fff`.
- Push a commit before you post its SHA, or GitHub leaves it unlinked.
- A PR title carries no reference. GitHub appends ` (#<n>)` to the squash commit.
- Every action that posts a description, a comment, or a review warns on stderr about `PR 12`
  (write `#<n>`). If the text quotes a commit subject, ignore the warning.

## Review comments and replies

See "References" for issue, pull request, and commit references.

- Start with `[Minor]`, `[Major]`, `[Blocker]`, `[Question]`, `[Info]`, `[Suggestion]`, or
  `[Critical]`. Grade by effect:

  | Marker | Effect |
  |---|---|
  | `[Critical]` | Data loss, a security hole, or an outage |
  | `[Blocker]` | Wrong results on the main path, a failing build on any target framework, or a reference from `src/Common` to ASP.NET Core |
  | `[Major]` | Wrong results on an edge, failure, or concurrency path, or the main behaviour untested. Also stored data that existing data gives, one rule in two places, a PR over the size ceiling without a valid exemption, a breaking change that the description does not name, or a README snippet that does not compile |
  | `[Minor]` | No wrong results: a misleading name, a member at the wrong abstraction level, a policy in the wrong layer, a test gap, stale text, excess comments, a missing result shape, or a breach of `rules.library.md`. Also a description that omits a tangible change, or a PR over the size target without a reason |
  | `[Suggestion]` | Optional; nothing is wrong |

- After the importance marker, use two or three sentences. Explain the defect, effect, and remedy.
- If the remedy needs more than a one-line change, add a fenced example the author can paste.
- End with `_-- AI Assistant_` on a new line.

## Review reports

Report, in this order:

1. Title, author, branches, status, `sourceCommit`, and changed files. Add the budget result
   that `scope` prints on stderr, and the tangible changes from its output.
2. Whether the PR does what each referenced issue asks.
3. The result of each `review.md` step: the description claims, the thread audit, the public API
   verdict, the design and data model verdict, posted comments (or the drafts, on a dry run), build warnings, the naming
   inventory (how many symbols you checked, and each one you flagged), documentation, and the
   coverage table. Include what is clean.
4. What you could not verify.
5. Recommendation and review event. Request changes while a `[Major]` or more severe thread is
   open. Comment while a `[Question]` is open. Otherwise approve. Submit the review with `vote`
   only when asked.
6. Follow-up issues, and checks after the next release, such as the package page on nuget.org.
