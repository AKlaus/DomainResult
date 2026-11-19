# Addressing Review Comments

Use this workflow when the developer asks to address, fix, or answer the open comments on their
PR. The request includes the commit and the push, so it needs no second request for them.

Run the six steps in order. Do not start a step until the step before it has passed. A failed gate
stops the workflow, and the final report names it.

| Step | Gate to pass before the next step |
|---|---|
| 1. Judge the comment | Each comment has a verdict, with evidence from the code |
| 2. Fix the problem | Each valid comment has a change |
| 3. Review the fix | The fix covers the whole comment, adds no defect, and builds |
| 4. Commit and push | The commits are on GitHub |
| 5. Reply and resolve | Each comment has a reply that names its commit or its reason, and is resolved |
| 6. Check the description | The description matches the pushed code |

## 0. Collect the open comments

- Run `review <pr>`. The open comments are the review threads with `isResolved: false` and the
  general comments that are not resolved.
- Skip a general comment that an earlier reply already answered.
- Skip the replies of earlier passes too. `reply <IC_…>` posts a new general comment that opens
  with `> @<login> [wrote](<url>):`, or `> [Comment](<url>):` for a deleted account, and that
  comment stays open. When the text after the quote starts with `[Info]`, the form step 5 writes,
  it is an answer, not a new comment. A quote-reply with any other marker is new feedback, such
  as a reviewer who disputes a decline.
- Check that the local branch is the PR's head branch and holds `sourceCommit`:
  `git merge-base --is-ancestor <sourceCommit> HEAD`. If it does not, stop and ask.
- Count the comments. If the developer named a number, such as "one more comment", and `review`
  finds a different number, address every open comment and say so in the report.

## 1. Judge each comment

Check every claim in the comment against the code at `HEAD`, not against its wording: names, line
numbers, callers, and statements such as "nothing reads it" or "production cannot send this".

- Judge the remedy apart from the finding. A finding can be right and its remedy wrong. Check the
  remedy against `rules.library.md`, and against the public API and design checks in
  step 3 of `review.md`.
- Give each comment one verdict:

  | Verdict | Next |
  |---|---|
  | Valid | Fix it in step 2 |
  | Partly valid | Fix the valid part; the reply in step 5 gives the reason for the rest |
  | Not valid | Change no code; the reply in step 5 gives the evidence |
  | Needs a decision | Ask the developer before step 2, for a scope or product rule |

## 2. Fix the problem

- Fix the cause, not only the place the comment names. Search the repository for the same defect.
- Keep the fix inside the PR's purpose. A defect that the PR did not add gets its own PR.
- Change the tests with the code: rename a test whose name states the old behaviour, and use
  input that production can produce.
- If the fix changes public behaviour, update `README.md` and `samples/` in the same commit.

## 3. Review the fix

Review your own diff as a reviewer would, before you commit it.

- The fix covers every part of the comment, including the tests, README text, and samples it
  asked for.
- No copy of the defect remains. Search the diff and the repository for the pattern.
- The fix adds no defect: check the callers, null handling, and the contracts and docs that
  describe the old behaviour, such as XML comments, remarks, and test names.
- The fix keeps the public API compatible, or the PR description marks the change **Breaking**.
- Run `dotnet build` with no new warning, and `dotnet test`. Both cover every target framework.
  If a framework's SDK is missing here, say so in the report. Never report an unrun test as passed.

If this step finds a problem, go back to step 2.

## 4. Commit and push

- Stage only the files of the fix. Leave other changes in the working tree as they are.
- Write one commit per comment, or per group of comments with one cause. The subject follows
  "Title areas" in `.agents/skills/_shared/references/text.md`. The body says why, and names
  the thread ids it addresses.
- Push the branch.

If the push fails, stop. Do not reply, resolve, or run `describe`. Report the failure: a reply
that names a commit GitHub does not have cannot link to it.

## 5. Reply and resolve

- Answer a review thread with `reply <PRRT_…>`, and a general comment with `reply <IC_…>`.
- Follow "Review comments and replies" in `writing.md`:
  - A fixed comment gets `[Info]`, what changed, and the commit as a bare SHA.
  - A declined comment gets `[Info] Not changed.` and the reason, with its evidence.
  - Use two or three sentences, and end with `_-- AI Assistant_`.
- After the reply, `resolve` the comment, whether it is fixed or declined. On a repeat pass, the
  reviewer reopens a comment that is not addressed, as step 2 of `review.md` describes.
- A comment that waits on the developer's decision stays open until the decision is made.

## 6. Check the description

The description must match the pushed code, and the squash commit copies it at `complete`.

- Run `scope`, and compare its output with **Changes at a glance**.
- Check each statement in the description against the new code: the **What changed** bullets,
  the notes, the additions that `scope` cannot see, and the follow-up list. A removed public
  member, a renamed overload, or a changed behaviour each needs an edit.
- If the PR is now over the target or the ceiling, add the reason or the exemption, as
  `scope.md` describes.
- Run `describe` only when a statement changed. Keep the developer's text that is still true.

## Report

Lead with the outcome. Then give, per comment, the verdict, the change, the commit, and whether
it is resolved. Then name every gate that did not pass, every test that did not run, and whether
the description changed.
