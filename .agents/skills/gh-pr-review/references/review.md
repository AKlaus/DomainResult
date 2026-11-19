# Pull Request Review Workflow

Read this reference after running `review <pr>` and before analysing or posting findings. Read
`writing.md` before writing a finding or the final review report.

A request to review a PR runs every step below, in order, posting included, without further
prompts. If the request asks for a dry run, make no change to the PR: put the drafts in the report
instead of posting them, and do not reply, edit, resolve, or vote. Each request is one review pass;
a repeat pass reviews a PR that already has your threads. If a step does not apply, the report
says so.

Before step 1, copy `review-worksheet.md` to a scratch file. If your tool keeps a task list, add one
item per numbered step. Re-read a step's section of this file before you start it. A step is done
only when its worksheet section is filled.

## 1. Gather context

- Read the PR title, description, every review thread (resolved ones included), every general
  comment, and every review body.
- Read each issue the description references, such as `Closes #88`, with
  `bash .agents/skills/gh-pr-review/scripts/run.sh issue <issue>`, and check whether the PR does
  what the issue and its comments ask.
- List each factual claim in the description, such as "no breaking change" or "changes no
  behaviour". Step 3 checks each one against the code.

## 2. Audit your earlier threads

On a repeat pass, check every review thread and general comment that you opened, open or resolved,
against the code at `sourceCommit`, not against the author's reply. A thread with
`isOutdated: true` sits on lines that a later commit changed; check it the same way, at its new
place in the code. `review` reports `isResolved` for both kinds; a general comment is resolved
once `resolve` has hidden it as resolved.

- Check each change the thread asked for. A thread is addressed only when every part is.
- Check each factual claim in the author's reply, such as "all callers updated" or "the test fails
  without the fix".
- Search the whole diff for the defect the thread named. A fix in one place can leave the same
  defect elsewhere.
- If the author declined a part, judge the stated reason. If the reason does not hold and the
  finding is `[Minor]` or more severe, `reply` in the thread with the evidence. Otherwise accept
  the decision.
- A new defect that the fix introduced is a new finding with its own thread.
- When a thread or general comment you raised is addressed, or you accepted the author's decline,
  `resolve` it if it is open. When a resolved one is neither, `unresolve` it and `reply` with the
  evidence.

## 3. Analyse the change

Robustness and maintainability are paramount. Judge whether the change remains correct under
failure, edge, and concurrency conditions, and whether future maintainers can safely change it.
Grade each finding by the severity table in `writing.md`.

- Read applicable `AGENTS.md` files and `.agents/rules/*.md` path-scoped rules.
- Reconstruct the diff at the immutable `baseCommit` (the merge base) and `sourceCommit` (the
  head) returned by `review`. Additions use an empty base, deletions use an empty source, edits
  fetch both paths, and renames fetch `originalPath` from the base and `path` from the source.
  Pass the commit SHA as the `fetch` ref. Alternatively, run `git fetch origin pull/<n>/head` and
  `git diff <baseCommit> <sourceCommit>` locally. Read the surrounding whole method or file.
- Review the design, logic, security, naming, maintainability, and test coverage. Do not repeat
  diagnostics that CI already fails on.
- Check out `sourceCommit` in a throwaway `git worktree`, and run `git fetch origin pull/<n>/head`
  first if the commit is missing. Run `dotnet build` and `dotnet test` there, and list the new warnings in changed files. Do not build your working tree: its
  uncommitted edits add warnings that the PR does not carry. A warning that `rules.library.md`
  names, such as CS1573 for partial `<param>` tags, is a finding. So is a CS1591 warning for an
  undocumented public member.
- **Description claims**: check each claim that step 1 listed against the code, a command result,
  or the PR text. Post the claims that do not hold in one comment.
- **References**: a PR that the description names as plain text, such as `PR 12`, does not
  link. The example is the line with `#12`. See `writing.md`.

### Public API

The packages ship to consumers who cannot see this repository's history. Check the public
surface before the lines, against these sections of `rules.library.md`:

- **Public API**: list each change that the section calls breaking, and whether the description
  marks it **Breaking**. List each new `public` member that consumers do not need.
- **Parity across result shapes**: list each shape or conversion that the change leaves out
  without a reason in the description.
- **Packages and dependencies**: check the package boundary of `src/Common`.
- **README and samples**: check each README snippet that the change affects against the API, and
  list each missing README or sample update.

### Design and data model

Judge the design before the lines, and before you grade any finding. Do not start from the
reasons that the description gives: a design can work and still be wrong. A finding here needs no
wrong result today. A later change that the design makes easy to get wrong is enough.

- **Existing structure**: before you judge a new concept, search for where the code already
  expresses it, such as registrations, configuration, or an existing type. The rule often exists
  one level up.
- **Stored data**: for each new column, property, field, or cached value, name the existing data
  that gives it. If existing data gives it, the new value is a copy that can drift. This applies
  if nothing changes the source today, and if the copy moves to another type.
- **One rule, one place**: list every place that encodes the new rule, including overloads,
  the `IActionResult` and the `IResult` converters, and test data. A rule in two places in production code is a finding.
- **Abstraction level**: for each new member of a base class, interface, or shared helper, count
  the cases it serves. A flag, an override, or a parameter that serves one feature does not
  belong in a general type. Name the level where the rule belongs.
- **Layer**: a policy belongs in the project that already knows what the policy needs. Flag a
  layer that must learn another layer's facts, such as `src/Common` code that knows an HTTP code.
- **Alternative**: sketch at least one design that stores nothing new and touches fewer layers.
  Compare the two in the worksheet. If the alternative is simpler, the finding gives it as the
  remedy.


### Size and changes at a glance

Read `scope.md`. Run `scope` with the `baseCommit` and `sourceCommit` that `review` returns. If a
commit is missing locally, `git fetch origin pull/<n>/head` first.

- **Budget**: read the budget result that `scope` prints on stderr. Over the ceiling without a
  valid exemption, or over the target without a reason, is a finding. Post it as one general
  comment. Its example is a split: one line per proposed PR, in merge order,
  built from the diff, its **Changes at a glance** rows, and the split order in `AGENTS.md`.
- **Atomic**: flag a refactor, reformat, or unrelated fix mixed into a behaviour change. Flag a
  behaviour change without its tests, and a slice that leaves `master` broken.
- **Changes at a glance**: compare the description with the `scope` output and with the diff.
  Flag each missing or stale row. Also flag each tangible change that `scope` cannot see and the
  description does not name, such as a public API change or a behaviour change behind an unchanged signature. The example
  is the missing rows, ready to paste. Post the unnamed breaking changes in one comment, and the
  other gaps in another.

### Behavioural blast radius

For every touched symbol, inspect all use sites, including unchanged lines. Re-evaluate exception
types, cancellation, lifetime and disposal, nullability, shared state, return values, and symmetric
success and failure paths. Anchor a finding on the closest changed line when its root cause is
unchanged.

### Reuse

Search the wider repository in both directions. Name an existing symbol and file for inbound reuse,
or a concrete adoption site for outbound reuse. Treat outbound consolidation as a follow-up unless
the PR needs it for correctness. Do not post speculative reuse suggestions.

Search by concept, not by name — the same idea often exists in another package under another name.
When new code restates an existing helper, diff the two semantics before you judge it. Differing
tolerances, guard clauses, or null handling are a correctness finding, not a reuse nit. Grade it at
the severity of its effect. Lead with the effect and name the existing symbol as the remedy.

### Naming and placement

List every new or changed type, member, parameter, variable, constant, record, file, folder, SQL
parameter, test, test title, and test helper in the diff. Check each one against **Naming** and
**Placement** in `AGENTS.md` and **Code layout** in `rules.library.md`, and cite the rule in each
finding. Run these checks for each symbol:

- **Purpose**: state in the worksheet what the symbol holds or does. If the name does not say
  it, or names a caller's use instead, flag it.
- **Across calls**: trace each new parameter, prop, and argument to its other end. Flag one value
  under two names.
- **Term**: search each term of the name in the same file, page, and module. Flag a term used
  there for another concept. Comments, test titles, UI text, and log messages count as names.
- **Rename leftovers**: for each rename in the diff, and each rename that a resolved thread
  agreed, search the old term in the changed files and their tests. Flag every leftover.
- **Place**: for each new or moved file, name the feature folder that holds it. For a shared
  symbol, list the features that call it and name the one that owns its concept. Flag shared code
  outside the feature that owns its concept, or above the nearest folder that holds all its
  callers. Check the namespace against **Placement** in `AGENTS.md`, and the test file location
  and member order against `rules.library.md`.
- **Older faults**: flag a wrong name or folder in a file the PR edits even when the fault is
  older, and ask for the move in a follow-up PR.
- **Parallel pattern**: parallel implementations of one feature follow one pattern. When several
  services repeat a change, flag the copy that differs.
- **Thin wrapper**: a thin wrapper needs a reason: a named rule, several callers, or test seeding.
  Flag a method, type, or conversion that only forwards, repeats another body, or that its caller
  undoes.

The step is done only when the worksheet's **Naming inventory** has one row per symbol. An empty
table, or one row per file, fails the step. A pass with no naming finding still reports how many
symbols it checked.

### Comments and documentation

Check every comment and doc comment the PR adds or changes, and every existing comment that
describes changed code. Comment lines do not count against the size budget (`scope.md`), so this
check is what limits their volume.

- A comment states the reason, an invariant, or a non-obvious condition. Flag a non-obvious
  condition that has no comment.
- Flag a comment that describes behaviour that the change, or a later commit of it, replaced.
- Doc comments follow **XML documentation** in `rules.library.md`.

#### Excess comments

Flag a comment, or a part of a doc comment, that adds little or nothing to the name, the
signature, and the nearby code. Test each one: if you delete it, does the reader lose a reason,
an invariant, a unit, a constraint, or a non-obvious condition? If not, it is excess. Typical
excess:

- A comment that restates the identifier, the signature, or the next statement, such as
  `// Save the changes` above `SaveChangesAsync`.
- Comments that narrate each step of a method that reads clearly without them.
- A `<remarks>` that repeats the `<summary>`, or explains general framework or language
  behaviour. A `<param>` or `<returns>` that repeats the name and the type.
- Text about the change history or the review, such as "now", "previously", "fixed", or "as
  requested". It belongs in the commit message or the PR description.
- Commented-out code.
- A comment block longer than the code it describes, when most of it fails the test above.

If `rules.library.md` requires a doc comment, such as on a public member, the comment
stays. Flag only its excess part.

Excess breaches `rules.library.md` and hides the comments that matter.
Keep the threads few:

- If one file has several excess comments, post one thread on the first and list the other lines.
- If the excess spans several files, post one general comment that lists each file and its lines.
- The example shows the trimmed comment, or the deletion, for one typical case.

### Tests

Check every changed test file and the unchanged suites that cover changed code. Build a coverage
table with one row per new or changed branch, defensive branches included. Each row names the
test that fails when that branch breaks, or marks the branch as a gap, an accepted gap (the PR
says why), or correctly untested (production cannot reach it).

- Check each case against **Testing** in `rules.library.md`. If you cannot name the branch,
  boundary, or contract that a case covers, flag it as redundant. Keep separate cases only when
  their failures would point to different faults.
- For a case whose input production cannot produce, also flag the code that only this case
  exercises.
- Flag a test that checks a test-only copy of production logic.
- Flag one wiring check repeated across classes. Propose moving the shared code so that one test
  covers it.
- The same feature in parallel suites has the same cases.
- Flag test setup and parameters that the code under test no longer uses.

## 4. Post findings

Post high-value, evidence-based findings inline by default. Include correctness, security, test,
robustness, and meaningful maintainability defects. Skip praise, summaries, style preferences, and
low-confidence guesses.

Post a finding that you make while answering a follow-up question about the PR in the same way.
Leave out a finding that the PR description already accepts, such as one under "Not in scope", and
say so in the report.

Draft every new thread, reply, and edit of the pass first. Post nothing until every draft meets the
checks under **Check the drafts before posting** and its worksheet row records each result.

- Prefer a thread, because a thread can be answered and resolved in place. Place the comment on
  the most specific changed line. If the line lies outside the diff, see **Findings outside the
  diff** below. Use a general comment only for a finding about the whole PR that no single file
  holds, such as the description, the size, or a split, or as the last step under **Findings
  outside the diff**.
- Read all existing threads and general comments before posting. Never open a second thread for a
  finding you already raised.
- Post at most one comment to a thread in each review pass.
- If you have more to say on a finding you already posted, and the new point carries the same
  marker, keep it to one post. `edit` the comment with a consolidated version when the edit rule
  below allows it, otherwise `reply`. An edit keeps the thread's id, state, position, and replies,
  so do not delete and re-`comment` it.
- A new point that needs a different marker is a separate finding. Open its own thread on its own
  line.
- On a repeat pass, re-`fetch` changed files at the new `sourceCommit` before analysing. New
  commits can shift or remove earlier finding lines. Do not repeat a finding the author has
  addressed.
- Use `run.sh comment` for a new thread, and pass the `sourceCommit` that `review` printed, so the
  comment lands on the code you read. `edit` a comment only if you posted it in this pass, or if it
  is a line or file comment that you posted, that has no reply, and whose `commit` equals
  `sourceCommit`. Otherwise `reply`, because the author may already have read or acted on the
  comment.
- `reply` takes a thread id or a general comment id. GitHub has no thread for a general comment, so
  `reply` posts a new general comment that mentions the author, links the original, and quotes its
  first line.

### Findings outside the diff

GitHub accepts a line comment only on a line inside a diff hunk; any other line fails with `422`.
Keep the finding in a thread where you can. In this order:

- When the root cause sits outside the diff but a changed line reaches it, anchor on that changed
  line, as **Behavioural blast radius** says.
- If the file is in the PR, post a file-level comment: `comment <pr> <text> <sourceCommit> <file>`.
  Name the line in the text.
- If another changed file is the one the finding concerns, such as the caller of an unchanged
  method, post a file-level comment there with a permalink to the line:
  `https://github.com/AKlaus/DomainResult/blob/<sourceCommit>/<path>#L<n>`.
- Otherwise, post a general comment with that permalink.

### Check the drafts before posting

Check each draft on its own:

- **Severity**: the marker matches the severity table in `writing.md` for the effect the comment
  states.
- **Statements**: every factual statement, such as a line number, symbol name, quote, count, or
  claim about behaviour or tests, matches the code at `sourceCommit`, a command result, or
  the PR text. Drop a statement you cannot check, or mark it as unchecked and say why.
- **Remedy**: the remedy achieves the effect the comment names. Trace it: a fix at one call site
  does not remove a defect that other call sites still reach.
- **Remedy design**: the remedy passes the checks under **Design and data model**. This includes a
  remedy that the user or the author proposes. A copy of data that moves to another table is still
  a copy.
- **Example**: the comment has the example that `writing.md` requires.

Then check the drafts together, with every open thread and the outcome of every resolved thread:

- **One finding, one thread**: no draft repeats a finding that an open thread raises. Merge it into
  that thread under the rules above.
- **No contradictions**: no two comments state different facts about the same code, give
  different remedies for the same problem, or grade findings with the same effect differently.
- **Remedies combine**: applying every remedy together leaves each one intact. For example, no
  comment deletes a test that another comment extends.
- **Resolved outcomes stand**: a draft that reverses what a resolved thread agreed says so and
  gives the reason.

If a check fails, fix the drafts and run both checks again.

After posting, read every new or edited comment back and confirm that it matches the draft.

## 5. Present the review

Before you write the report, confirm that every worksheet section is filled or says why it is
empty. Write the report from the worksheet, as `writing.md` describes under "Review reports".
