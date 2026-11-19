# GitHub API Reference

REST endpoints below are relative to `https://api.github.com/repos/{owner}/{repo}`, which the
scripts build from `GITHUB_REPOSITORY` as `REPO_URL`. GraphQL operations go to
`POST https://api.github.com/graphql`.

Every request sends these headers:
```
Authorization: Bearer <GITHUB_TOKEN>
Accept: application/vnd.github+json
X-GitHub-Api-Version: 2022-11-28
```

`fetch` sends `Accept: application/vnd.github.raw+json` instead, so the body is the file itself.

---

## Paging

- REST lists: the scripts append `per_page=100&page=<n>`, starting at `1`. A page with fewer than
  100 items is the last. If any page fails, nothing is printed, so a partial list never reads as whole.
- GraphQL connections: the scripts request `first: 100` and follow `pageInfo { hasNextPage endCursor }`
  through `after`. Review thread comments are read 100 per thread, without further pages.

---

## Pull Requests

### List PRs (`list`)
```
GET /pulls?state=open|closed|all&per_page=100&page={n}
```
`merged` and `closed` both request `state=closed`; the script splits them by `merged_at`.

### Create a PR (`create`)
```
POST /pulls
```
Body:
```json
{
  "head": "feature/my-change",
  "base": "master",
  "title": "Describe the change",
  "body": "Optional PR description",
  "draft": true
}
```

`head` and `base` are branch names. The `create` action strips a `refs/heads/` prefix and
defaults to a draft PR. Pass `--ready` to create a non-draft PR; the flag is independent of the
optional description. Use `--` before a description that begins with `--`.

### Get PR Details (`review`, `publish`, `complete`)
```
GET /pulls/{number}
```
`review` reads `head.sha` (the `sourceCommit`), `state`, `base.ref`, `base.sha`, `draft`,
`mergeable`, and requested reviewers. `publish` reads `draft` and `node_id`. `complete` reads
`state`, `merged`, `draft`, `title`, `body`, `html_url`, `head.sha`, `head.ref`,
`head.repo.full_name`, `base.ref`, and `base.repo` (`full_name`, `default_branch`).

### Merge base and target commit (`review`)
```
GET /compare/{base.ref}...{head.sha}?per_page=1     # open PR
GET /compare/{base.sha}...{head.sha}?per_page=1     # closed PR
```
`merge_base_commit.sha` is the `baseCommit`: the diff the PR shows is `baseCommit...sourceCommit`.
`base_commit.sha` is the `targetCommit`. An open PR is compared with its target branch, because
`base.sha` can name a commit that the repository no longer holds, and that compare answers `404`.
`per_page=1` keeps the commit list short; the response still carries patches.

After the files, threads, comments, and reviews are read, `review` reads `GET /pulls/{number}` and
the compare again. If the head, the target branch, the state, or the target commit changed, it
fails and prints nothing: the file list and threads would describe another revision than the
snapshot.

### Changed files (`review`)
```
GET /pulls/{number}/files?per_page=100&page={n}
```
Returns `filename`, `previous_filename` (renames), and `status` (`added`, `modified`, `removed`,
`renamed`, `copied`, `changed`, `unchanged`). GitHub lists at most 3,000 files.

### Threads, general comments, and reviews (`review`)
GraphQL on `repository(owner, name) { pullRequest(number) { … } }`:

- `reviewThreads`: `id` (`PRRT_…`), `isResolved`, `isOutdated`, `path`, `line`, `originalLine`,
  `subjectType`, and `comments(first: 100)` with `id` (`PRRC_…`), `body`, `createdAt`,
  `author { login __typename }`, `originalCommit { oid }`.
- `comments`: general conversation comments, `id` (`IC_…`), `body`, `createdAt`, `author`, and
  `isMinimized` with `minimizedReason`. A comment hidden with the reason `resolved` reports
  `isResolved: true`; one hidden for another reason reports that reason as `hidden`.
- `reviews`: `id` (`PRR_…`), `body`, `state`, `submittedAt`, `author`.

Comments by a `Bot` author are dropped. A review is listed only if its body is not empty.

### Update title or description (`title`, `describe`)
```
PATCH /pulls/{number}
```
Body: `{ "title": "New title" }` or `{ "body": "New description" }`.

### Change the target branch (`retarget`)
```
PATCH /pulls/{number}
```
Body: `{ "base": "<branch>" }`. The script strips a `refs/heads/` prefix. GitHub answers `422` when
the branch does not exist, is the PR's own head, or the PR is closed. GitHub also refuses a PR that
is in a stack; run `stack unstack` first. The response carries the new `base.ref`, which the script
prints with `head.ref` and `state`.

### Stacks (`stack`)
```
GET  /stacks?per_page=100                 # list
GET  /stacks?pull_request={number}        # show: the stack that holds a PR, or []
POST /stacks                              # create: { "pull_requests": [bottom, ..., top] }
POST /stacks/{stack_number}/add           # add:    { "pull_requests": [above the top, ...] }
POST /stacks/{stack_number}/unstack       # unstack: no body; 200 with the merged/queued PRs that stay, 204 when dissolved
```
All five send `X-GitHub-Api-Version: 2026-03-10`. GitHub answers `422` when a listed PR does not
exist or does not target the head branch of the PR below it, and `409` when another request is
changing the stack.

### Publish a draft (`publish`)
```graphql
mutation($id: ID!) {
  markPullRequestReadyForReview(input: {pullRequestId: $id}) { pullRequest { isDraft } }
}
```
`$id` is the PR's `node_id`. The script skips the mutation when `draft` is already `false`.

### Squash-merge (`complete`)
```
PUT /pulls/{number}/merge-async
GET /pulls/{number}/merge-async/{uuid}
```
Both calls send `X-GitHub-Api-Version: 2026-03-10`, the version that holds the asynchronous merge
endpoints and the `stack` object on a pull request; `complete` reads the pull request with that
version too. Body: `{ "merge_method": "squash", "merge_action": "direct_merge", "commit_title":
"<title> (#<number>)", "commit_message": "<description>", "sha": "<head.sha>" }`. `sha` is the
head that `complete` read; GitHub cancels the merge if the head moves. The `PUT` answers `202`
with `{ "status": "pending", "details": { "uuid": ... } }`, `200` when the pull request is already
merged, `400` when it is not ready, and `409`, with the pending request's UUID, when a request is
already pending; `complete` polls that one. The `GET` answers the same shape; the final `status`
is `merged` (with `details.sha`), `failed` (with `details.message`: a failed required check, a
missing approval, a conflict, or squash merges turned off), or `enqueued` for a merge queue, which
`complete` reports as not merged. The merge needs **Contents → Read and write**.

A pull request in a stack carries `stack.base.ref`, the branch the whole stack targets. The
asynchronous merge of a stacked pull request merges every open pull request below it as well, so
`complete` refuses a pull request whose `base.ref` differs from `stack.base.ref`.

After the merge, `complete` lists the open PRs that target the head branch, and keeps the branch
if any exist:
```
GET /pulls?state=open&per_page=100&base={branch}
```
Otherwise it deletes the head branch with Git, not the REST API, because only a push can make the
deletion conditional:
```
git push --force-with-lease=refs/heads/{branch}:{head.sha} https://github.com/{owner}/{repo}.git :refs/heads/{branch}
```
The lease deletes the branch only while it points at the merged head, so a push after the merge
keeps it. Git rejects the lease as "stale info" both for a moved branch and for a branch that is
already gone, such as by the repository's auto-delete setting; `git ls-remote` then tells the two
apart. The push runs from an empty scratch repository, and needs **Contents → Read and write**.

The local steps then run `git fetch` for the target branch and `refs/pull/{number}/head`, from the
remote whose configured URL is `https://github.com/{owner}/{repo}` or its SSH form. Every Git call
goes through `gh_git` in `lib.sh`. Its credential helper reads the token from that one command's
environment, so the token stays out of the command line and the Git config. The helper answers
only `protocol=https` and `host=github.com`, so another host never receives the token.

---

## Comments

### General comment (`comment <pr> <text>`)
```
POST /issues/{number}/comments
```
Body: `{ "body": "Your comment" }`. Returns `node_id` (`IC_…`).

### Review comment (`comment <pr> <text> <commit> <file> [line]`)
```
POST /pulls/{number}/comments
```
Line comment:
```json
{ "body": "Your comment", "commit_id": "<commit>", "path": "src/main.py", "line": 10, "side": "RIGHT" }
```
File-level comment:
```json
{ "body": "Your comment", "commit_id": "<commit>", "path": "src/main.py", "subject_type": "file" }
```
`<commit>` is the `sourceCommit` the reviewer read. `line` must sit inside a diff hunk on the new
side, and `path` must be a file of the PR. Otherwise GitHub answers `422`. It also answers `422`
("one pending review per pull request") while the account has an unsubmitted review on the PR.
Returns `node_id` (`PRRC_…`); the new thread's id appears in `review`.

### Reply (`reply`)
The script first reads the node:
```graphql
node(id: $id) {
  __typename
  ... on IssueComment { url body author { login } pullRequest { id } }
}
```
A review thread (`PullRequestReviewThread`) gets a nested reply:
```graphql
mutation($thread: ID!, $body: String!) {
  addPullRequestReviewThreadReply(input: {pullRequestReviewThreadId: $thread, body: $body}) {
    comment { id body url }
  }
}
```
A general comment (`IssueComment`) has no thread, so the reply is a new comment on the pull request
that holds it, through `addComment(input: {subjectId: <pullRequest.id>, body: $body})`. Posting to
that PR node, not to a PR number in the configured repository, keeps an id from another repository
off an unrelated PR. The body opens with a quote: `> @author [wrote](url):`, then `> ` and the first
non-empty line of the Markdown `body` that is not itself a quote, cut at 200 characters with an
open code span closed. Quoting the Markdown keeps code spans such as `` `@name` `` as code. The
mention notifies the author.

### Edit a comment (`edit`)
The script first reads `node(id) { __typename }`, then runs one of:
```graphql
updateIssueComment(input: {id: $id, body: $body}) { issueComment { id body } }
updatePullRequestReviewComment(input: {pullRequestReviewCommentId: $id, body: $body}) {
  pullRequestReviewComment { id body }
}
```
Only the comment's author can edit it. The thread keeps its id, state, position, and replies.

### Delete a comment (`delete`)
Same type check, then:
```graphql
deleteIssueComment(input: {id: $id}) { clientMutationId }
deletePullRequestReviewComment(input: {id: $id}) { clientMutationId }
```
Deleting the only comment of a review thread removes the thread.

### Resolve or reopen (`resolve`, `unresolve`)
The script reads `node(id) { __typename }`, then runs, for a review thread:
```graphql
resolveReviewThread(input: {threadId: $id}) { thread { id isResolved path } }
unresolveReviewThread(input: {threadId: $id}) { thread { id isResolved path } }
```
and for a general comment, which has no thread:
```graphql
minimizeComment(input: {subjectId: $id, classifier: RESOLVED}) { minimizedComment { isMinimized minimizedReason } }
unminimizeComment(input: {subjectId: $id}) { unminimizedComment { isMinimized } }
```
Resolving or reopening a review thread needs **Contents → Read and write**; with only **Pull
requests → Read and write**, GitHub answers "Resource not accessible by personal access token".
Hiding a comment needs write access to the repository. The script fails when the response does not
report the new state.

---

## Reviews (`vote`)
```
POST /pulls/{number}/reviews
```
Body: `{ "event": "APPROVE" | "REQUEST_CHANGES" | "COMMENT", "body": "…" }`. `body` is omitted
when empty, which only `APPROVE` allows. GitHub refuses `APPROVE` and `REQUEST_CHANGES` from the
PR's author with `422`.

---

## File Content (`fetch`)
```
GET /contents/{path}?ref={branch-or-sha}
Accept: application/vnd.github.raw+json
```
Each path segment and the ref are URL-encoded. The raw media type returns files up to 100 MB.

---

## Errors

REST errors carry `.message`, often with `.errors[]` details and `.documentation_url`. GraphQL
usually answers HTTP `200` with an `.errors[]` array; the scripts treat any `.errors` as a failure.
`fail_with_body` prints `.message` and each entry of `.errors[]`.

| Code | Meaning |
|------|---------|
| 401  | Token missing, invalid, or expired |
| 403  | Token lacks a permission; the organisation has not approved the fine-grained token; a classic token is not SSO-authorised; or a rate limit (`x-ratelimit-remaining: 0`, or a secondary limit message) |
| 404  | PR, file, or ref not found — or the token cannot see the repository: GitHub hides private repositories with `404` |
| 405  | The PR cannot be merged: a failed required check, a missing approval, a conflict, or a disabled merge method |
| 409  | The head commit moved after `complete` read it |
| 422  | Validation failed: a line outside the diff, a review comment while you have a pending review, a review event on your own PR, a branch that does not exist, or a PR that already exists |
| 429  | Secondary rate limit; wait for `retry-after` |

The primary rate limit is 5,000 requests per hour per user token. GraphQL counts points per query
instead.
