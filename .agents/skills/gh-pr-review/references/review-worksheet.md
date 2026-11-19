# Review Worksheet

Copy this file to a scratch path, such as `/tmp/pr-<n>-worksheet.md`, at the start of each review
pass. Fill a section when you finish its step in `review.md`. If a section has no rows, write
"None" and the reason. Build the comments and the report from this file.

PR #<n>, base <baseCommit>, source <sourceCommit>

## 1. Issues

| Issue | What it asks | Met | Evidence |
|---|---|---|---|

### Description claims

| Claim | Checked against | Holds |
|---|---|---|

## 2. Audit of your earlier threads

| Thread (`PRRT_…`) or general comment (`IC_…`) | isResolved, isOutdated | Requested change | Verdict | Evidence |
|---|---|---|---|---|

## 3. Analysis

### Public API

| Public type or member (file:line) | Added, changed, or removed | Breaking | Marked in the description | Verdict |
|---|---|---|---|---|

| Result shape or conversion | Covered by the change | Verdict |
|---|---|---|

README and samples: <updated | not needed, and why | missing>

### Design and data model

Existing structure of the concept: <where the code already expresses it>

| New stored value | Existing data that gives it | Verdict |
|---|---|---|

| Rule | Every place that encodes it | Verdict |
|---|---|---|

| New member of a shared type | Cases it serves | Verdict |
|---|---|---|

| Policy | Layer it sits in | Layer that knows what it needs | Verdict |
|---|---|---|---|

| Design | Stores | Layers touched | Verdict |
|---|---|---|---|
| The PR | | | |
| Alternative | | | |

### Size and changes at a glance

Budget <within | over target | over ceiling>, reason or exemption <text | none>, atomic <yes |
what is mixed in>

| Tangible change (`scope` or diff) | In the description | Verdict |
|---|---|---|

### Build warnings

| New warning | Location | Verdict |
|---|---|---|

### Findings

| # | Location | Finding and effect | Marker |
|---|---|---|---|

### Use sites

| Changed symbol | Use sites checked | Result |
|---|---|---|

### Reuse

| Concept | Where searched | Result |
|---|---|---|

### Naming inventory

One row per symbol. In the verdict, write `ok` or the name of the failed check from
**Naming and placement** in `review.md`.

| Symbol (file:line) | Kind | Holds or does | Same value or term elsewhere | Expected place | Verdict |
|---|---|---|---|---|---|

### Comments and documentation

Verdict: `ok`, `missing reason`, `stale`, `stack-rule breach`, or `excess` with the part to cut.

| Location | Verdict |
|---|---|

### Test coverage

| Changed branch | Test that fails when it breaks, or status |
|---|---|

## 4. Drafts

In each check column, write `ok` or what you changed.

| # | Finding | New thread, general comment, edit, or reply | Location (line, file-level, or permalink) | Severity | Statements | Remedy | Remedy design | Example |
|---|---|---|---|---|---|---|---|---|

| Set check | Result |
|---|---|
| One finding, one thread | |
| No contradictions | |
| Remedies combine | |
| Resolved outcomes stand | |

| # | Thread | Read back |
|---|---|---|

## Not verified
