# Addressing Review Comments

Use this workflow when the developer asks to address the open comments on their PR. The request
includes the commit and the push.

Run the steps in order. Do not start a step until the gate before it has passed. A failed gate
stops the workflow, and the report names it.

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
- Skip a general comment that an earlier reply already answered. A quote-reply from `reply`
  opens with `> @<login> [wrote](<url>):`, or `> [Comment](<url>):` for a deleted account. If
  the text after the quote starts with `[Info]`, it is an earlier answer. Any other marker is new
  feedback, such as a reviewer who disputes a decline.
- Check that the local branch holds `sourceCommit`:
  `git merge-base --is-ancestor <sourceCommit> HEAD`. If it does not, stop and ask.
- If the developer named a number of comments and `review` finds a different number, address
  every open comment and say so in the report.

## 1. Judge each comment

Check every claim in the comment against the code at `HEAD`, not against its wording.

- Judge the remedy apart from the finding. Check it against `rules.library.md`, and against the
  public API and design checks in step 3 of `review.md`.
- Give each comment one verdict:

  | Verdict | Next |
  |---|---|
  | Valid | Fix it in step 2 |
  | Partly valid | Fix the valid part; the reply in step 5 gives the reason for the rest |
  | Not valid | Change no code; the reply in step 5 gives the evidence |
  | Needs a decision | Ask the developer before step 2, for a scope or product rule |

## 2. Fix the problem

- Fix the cause, and search the repository for the same defect.
- Keep the fix inside the PR's purpose. A defect that the PR did not add gets its own PR.
- Change the tests with the code, test names included.

## 3. Review the fix

- The fix covers every part of the comment, including the tests, README text, and samples it
  asked for.
- No copy of the defect remains.
- The XML comments, remarks, and test names that described the old behaviour are updated.
- The fix passes the **Public API** checks in step 3 of `review.md`.
- `dotnet build` adds no warning, and `dotnet test` passes. If a framework's SDK is missing here,
  say so in the report.

If this step finds a problem, go back to step 2.

## 4. Commit and push

- Stage only the files of the fix.
- Write one commit per comment, or per group of comments with one cause. The subject follows
  "Title areas" in `.agents/skills/_shared/references/text.md`. The body says why, and names
  the thread ids it addresses.
- If the push fails, stop. Do not reply, resolve, or run `describe`. Report the failure.

## 5. Reply and resolve

- Follow "Review comments and replies" in `writing.md`. A fixed comment gets `[Info]`, what
  changed, and the commit SHA. A declined comment gets `[Info] Not changed.` and the reason, with
  its evidence.
- After the reply, `resolve` the comment, fixed or declined. A comment that waits on the
  developer's decision stays open.

## 6. Check the description

- Run `scope`, and compare its output with **Changes at a glance**.
- Check each statement in the description against the new code, and fix the ones that changed.
- If the PR is now over the target or the ceiling, add what **Pull request scope** in `AGENTS.md`
  asks for.
- Run `describe` only when a statement changed. Keep the developer's text that is still true.

## Report

Lead with the outcome. Then give, per comment, the verdict, the change, the commit, and whether
it is resolved. Then name every gate that did not pass, every test that did not run, and whether
the description changed.
