# Pull Request Review Workflow

Read this after `review <pr>` and before you analyse or post. Read `writing.md` before you write a
finding or the report.

A review request runs every step below, in order, posting included. On a dry run, change nothing
on the PR: the report holds the drafts, and you do not reply, edit, resolve, or vote. If a step
does not apply, the report says so.

Before step 1, copy `review-worksheet.md` to a scratch file. A step is done only when its
worksheet section is filled.

## 1. Gather context

- Read the title, the description, every review thread (resolved ones included), every general
  comment, and every review body.
- Read each issue that the description references with `issue <issue>`. Check whether the PR does
  what the issue and its comments ask.
- List each factual claim in the description, such as "no breaking change". Step 3 checks them.

## 2. Audit your earlier threads

On a repeat pass, check every thread and general comment that you opened, open or resolved,
against the code at `sourceCommit`, not against the author's reply. Check an outdated thread at
its new place in the code.

- A thread is addressed only when every part of it is.
- Check each factual claim in the author's reply.
- Search the whole diff for the defect the thread named.
- If the author declined a part, judge the reason. If it does not hold and the finding is
  `[Minor]` or more severe, `reply` with the evidence. Otherwise accept the decision.
- A defect that the fix introduced is a new finding with its own thread.
- `resolve` an open thread that is addressed or whose decline you accepted. `unresolve` a resolved
  one that is neither, and `reply` with the evidence.

## 3. Analyse the change

Grade each finding by the severity table in `writing.md`.

- Read `AGENTS.md`.
- Diff `baseCommit` against `sourceCommit`, both from `review`. Read each touched method or file
  in full.
- In a throwaway `git worktree` at `sourceCommit`, run `dotnet build` and `dotnet test`. List the
  new warnings in changed files. Do not build your working tree, because its uncommitted edits
  add warnings that the PR does not carry.
- **Description claims**: check each claim from step 1 against the code, a command result, or
  the PR text. Post the claims that do not hold in one comment.
- **References**: flag a PR that the description names as plain text, such as `PR 12`. The
  example is the line with `#12`.

### Public API

Check the public surface before the lines, against these sections of `AGENTS.md`:

- **Public API**: list each breaking change, and whether the description marks it **Breaking**.
  List each new `public` member that consumers do not need.
- **Parity across result shapes**: list each shape or conversion that the change leaves out
  without a reason in the description.
- **Packages and dependencies**: check the package boundary of `src/Common`.
- **README and samples**: check each README snippet that the change affects against the API, and
  list each missing README or sample update.

### Design and data model

Judge the design before the lines, and do not start from the reasons that the description gives.
A design that makes a later change easy to get wrong is a finding, even with no wrong result today.

- **Existing structure**: before you judge a new concept, find where the code already expresses it.
- **Stored data**: for each new field, property, or cached value, name the existing data that
  gives it. A value that existing data gives is a copy that can drift.
- **One rule, one place**: list every place that encodes the new rule, including overloads, both
  converters, and test data.
- **Abstraction level**: a flag, an override, or a parameter that serves one feature does not
  belong in a general type. Name the level where it belongs.
- **Layer**: flag a layer that must learn another layer's facts, such as `src/Common` code that
  knows an HTTP code.
- **Alternative**: sketch at least one design that stores nothing new and touches fewer files.
  Compare the two in the worksheet. If the alternative is simpler, the finding gives it as the
  remedy.

### Size and changes at a glance

Run `scope` with `baseCommit` and `sourceCommit`, and read `scope.md`.

- **Budget**: a PR over the ceiling without a valid exemption, or over the target without a
  reason, is a finding. Post it as one general comment. Its example is a split: one line per
  proposed PR, in merge order.
- **Atomic**: flag a refactor, reformat, or unrelated fix mixed into a behaviour change, a
  behaviour change without its tests, and a slice that leaves `master` broken.
- **Changes at a glance**: compare the description with the `scope` output and the diff. Flag
  each missing or stale row, and each change that `scope` cannot see and the description does
  not name. The example is the missing rows, ready to paste. Post the unnamed breaking changes in
  one comment, and the other gaps in another.

### Use sites

For every touched symbol, inspect all use sites, unchanged lines included. Anchor a finding on
the closest changed line when its root cause is unchanged.

### Reuse

Search the repository by concept in both directions. Name an existing symbol and file for inbound
reuse, or a concrete adoption site for outbound reuse. Outbound consolidation is a follow-up
unless correctness needs it. When new code restates an existing helper, compare the semantics:
a difference is a correctness finding, graded by its effect.

### Naming and placement

List every new or changed type, member, parameter, variable, constant, file, folder, test, test
title, and test helper. Check each one against **Naming** and **Placement** in `AGENTS.md`,
and cite the rule in each finding.

- **Purpose**: write in the worksheet what the symbol holds or does. Flag a name that does not
  say it.
- **Across calls**: trace each new parameter and argument to its other end. Flag one value under
  two names.
- **Term**: flag a term that the same file uses for another concept, comments and test titles
  included.
- **Rename leftovers**: for each rename in the diff, and each that a resolved thread agreed,
  search the old term in the changed files and their tests.
- **Place**: name the feature folder of each new or moved file, and of each shared symbol's
  owning feature.
- **Older faults**: flag a wrong name or folder in a file the PR edits, and ask for the fix in a
  follow-up PR.
- **Parallel pattern**: when parallel implementations repeat a change, flag the copy that differs.
- **Thin wrapper**: flag a method, type, or conversion that only forwards, repeats another body,
  or that its caller undoes, unless a rule, several callers, or a test needs it.

The worksheet's **Naming inventory** has one row per symbol, not per file. A pass with no naming
finding still reports how many symbols it checked.

### Comments and documentation

Check every comment that the PR adds or changes, and every existing comment that describes
changed code. Comment lines do not count against the budget, so this check limits them.

- Flag a non-obvious condition that has no comment, and a comment that describes replaced
  behaviour.
- Doc comments follow **XML documentation** in `AGENTS.md`.
- Flag excess: a comment, or a part of a doc comment, whose deletion loses no reason, invariant,
  unit, constraint, or non-obvious condition. Text about the change history or the review belongs
  in the commit or the PR. A doc comment that `AGENTS.md` requires stays; flag only its
  excess part.
- For excess, post one thread per file that lists its lines, or one general comment when it spans
  several files. The example shows one trimmed case.

### Tests

Check every changed test file and the unchanged suites that cover changed code. Build a coverage
table with one row per new or changed branch, defensive branches included. Each row names the
test that fails when the branch breaks, or marks it as a gap, an accepted gap (the PR says why),
or correctly untested (production cannot reach it).

- Check each case against **Testing** in `AGENTS.md`. Flag a case whose branch, boundary,
  or contract you cannot name. Keep separate cases only when their failures point to different
  faults.
- For a case whose input production cannot produce, also flag the code that only it exercises.
- Flag a test of a test-only copy of production logic, one wiring check repeated across classes,
  and setup or parameters that the code under test no longer uses.

## 4. Post findings

Post evidence-based findings. Skip praise, summaries, style preferences, and low-confidence
guesses. Post a finding from a follow-up question the same way. Leave out a finding that the
description already accepts, such as one under "Not in scope", and say so in the report.

Draft every new thread, reply, and edit of the pass first. Post nothing until every draft passes
**Check the drafts before posting**, and the worksheet records each result.

- Prefer a thread on the most specific changed line. Use a general comment only for the whole PR,
  such as the description, the size, or a split, or as the last option under **Findings outside
  the diff**.
- Never open a second thread for a finding that a thread already holds. Post at most one comment
  to a thread in each pass.
- To add to your own finding with the same marker, `edit` the comment if the edit rule allows it,
  otherwise `reply`. A point that needs a different marker is a new thread.
- On a repeat pass, `fetch` the changed files at the new `sourceCommit` first.
- `edit` a comment only if you posted it in this pass, or if it is your line or file comment with
  no reply and with `commit` equal to `sourceCommit`. Otherwise `reply`.

### Findings outside the diff

Use the first option that fits:

1. If a changed line reaches the root cause, anchor on that line.
2. If the file is in the PR, post a file-level comment that names the line.
3. If another changed file concerns the finding, such as the caller of an unchanged method, post a
   file-level comment there with a permalink:
   `https://github.com/AKlaus/DomainResult/blob/<sourceCommit>/<path>#L<n>`.
4. Otherwise, post a general comment with the permalink.

### Check the drafts before posting

Check each draft:

- **Severity**: the marker matches the severity table in `writing.md` for the effect it states.
- **Statements**: every line number, symbol, quote, count, and claim matches the code at
  `sourceCommit`, a command result, or the PR text. Drop a statement you cannot check, or mark it
  as unchecked and say why.
- **Remedy**: the remedy removes the defect at every place that reaches it, and passes **Design
  and data model**, even when the user or the author proposed it.
- **Example**: the comment has the example that `writing.md` requires.

Then check all drafts together with the open threads and the outcomes of resolved ones:

- No draft repeats a finding that an open thread raises.
- No two comments state different facts about the same code, give different remedies for one
  problem, or grade the same effect differently.
- Applying every remedy together leaves each one intact.
- A draft that reverses what a resolved thread agreed says so and gives the reason.

If a check fails, fix the drafts and run both checks again. After posting, read each new or edited
comment back and confirm that it matches the draft.

## 5. Present the review

Confirm that every worksheet section is filled or says why it is empty. Write the report from the
worksheet, as "Review reports" in `writing.md` describes.
