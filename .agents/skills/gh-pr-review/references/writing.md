# Writing Conventions

These conventions apply to PR titles and descriptions, review comments and replies, and review
reports. `.agents/skills/_shared/references/text.md` holds the title areas and the rules for
multiline content.

## Titles and descriptions

**Title**: `Area: imperative summary`, under 60 characters, with an area from "Title areas" in
`text.md`. The title carries no reference: GitHub appends ` (#<n>)` to the squash commit.

**Description**: why the change is needed, what changed in outline, its tangible changes, and any
check that it needs. Nothing else, and no list of changed files. A reviewer must grasp it in
30 seconds. `complete` copies it into the squash commit, so it must match the code at merge.

- **What changed**: one bullet per part of the change, a bold name and a fragment of about ten
  words. Leave out what **Changes at a glance** shows.
- **Changes at a glance**: from `scope`, as "Put it in the description" in `scope.md` describes.
- **How to verify**: only checks that a reviewer would not run anyway, such as a manual step or a
  command with arguments that this PR chose. Leave the section out if none exists.
- Headings are `###`. Use a table only for three or more parallel facts, a `- [ ]` task list
  only for follow-up work, and a `>` call-out only for a **Breaking** change. Fold long detail
  into `<details>`.
- End with `_-- AI Assistant_` on its own line. Add no other attribution, such as a
  "Generated with" line, even when your tool proposes one.

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

- A pull request is `#12`, never `PR 12` or a hand-built link. Each action that posts text warns
  on stderr about `PR 12`. Ignore the warning when the text quotes a commit subject.
- A description closes its issue with `Closes #<issue>`.
- A commit is a bare SHA of 7 or more characters. Push the commit before you post its SHA.

## Review comments and replies

- Start with a marker, graded by effect:

  | Marker | Effect |
  |---|---|
  | `[Critical]` | Data loss, a security hole, or an outage |
  | `[Blocker]` | Wrong results on the main path, a failing build on any target framework, or a reference from `src/Common` to ASP.NET Core |
  | `[Major]` | Wrong results on an edge, failure, or concurrency path, or the main behaviour untested. Also stored data that existing data gives, one rule in two places, a PR over the size ceiling without a valid exemption, a breaking change that the description does not name, a description claim that hides one, or a README snippet that does not compile |
  | `[Minor]` | No wrong results: a misleading name, a member at the wrong abstraction level, a policy in the wrong layer, a test gap, stale text, excess comments, a missing result shape, a public member that consumers do not need, a missing README or sample update, or a breach of `rules.library.md`. Also a description claim that does not hold, an unlinked reference, a description that omits a tangible change, or a PR over the size target without a reason |
  | `[Suggestion]` | Optional; nothing is wrong |
  | `[Question]` | You need an answer before you can judge |
  | `[Info]` | A reply that reports a fix or a decision |

- After the marker, two or three sentences: the defect, its effect, and the remedy.
- If the remedy needs more than a one-line change, add a fenced example the author can paste.
- End with `_-- AI Assistant_` on its own line.

## Review reports

Report, in this order:

1. Title, author, branches, status, `sourceCommit`, and changed files. Add the budget result
   that `scope` prints on stderr, and the tangible changes from its output.
2. Whether the PR does what each referenced issue asks.
3. The result of each `review.md` step: the description claims, the thread audit, the public API
   verdict, the design and data model verdict, posted comments (or the drafts, on a dry run),
   build warnings, the naming inventory (how many symbols you checked, and each one you flagged),
   documentation, and the coverage table. Include what is clean.
4. What you could not verify.
5. Recommendation and review event. Request changes while a `[Major]` or more severe thread is
   open. Comment while a `[Question]` is open. Otherwise approve. Submit the review with `vote`
   only when asked.
6. Follow-up issues, and checks after the next release, such as the package page on nuget.org.
