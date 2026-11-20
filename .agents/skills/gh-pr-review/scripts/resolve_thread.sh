#!/usr/bin/env bash
# resolve_thread.sh — Resolve or reopen a review thread or a general comment on a GitHub pull request
# Usage: bash resolve_thread.sh <OWNER/REPO> <ID> <resolve|unresolve>
#
# ID is an id as review prints it. A review thread (PRRT_…) is resolved or reopened. A general
# comment (IC_…) has no thread on GitHub, so resolve hides it with the reason "Resolved", which
# collapses it in the conversation; unresolve shows it again. Hiding needs write access to the
# repository; anyone who can read the PR can still expand a hidden comment.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
TARGET_ID="${2:?Missing THREAD_OR_COMMENT_ID}"
MODE="${3:-resolve}"

case "$MODE" in
  resolve | unresolve) ;;
  *)
    echo "ERROR: invalid mode '${MODE}'. Valid: resolve, unresolve." >&2
    exit 1
    ;;
esac

gh_init "$REPOSITORY"

VARIABLES=$(jq -n --arg id "$TARGET_ID" '{id: $id}')
KIND=$(gh_graphql 'query($id: ID!) { node(id: $id) { __typename } }' "$VARIABLES" \
  "'${TARGET_ID}' was not found." | jq -r '.node.__typename // empty')

case "$KIND:$MODE" in
  PullRequestReviewThread:resolve)
    DATA=$(gh_graphql 'mutation($id: ID!) { resolveReviewThread(input: {threadId: $id}) { thread { id isResolved path } } }' \
      "$VARIABLES" "thread ${TARGET_ID} was not updated.")
    RESULT=$(printf '%s' "$DATA" | jq '.resolveReviewThread.thread | {kind: "thread", id: .id, isResolved: .isResolved, file: .path}')
    ;;
  PullRequestReviewThread:unresolve)
    DATA=$(gh_graphql 'mutation($id: ID!) { unresolveReviewThread(input: {threadId: $id}) { thread { id isResolved path } } }' \
      "$VARIABLES" "thread ${TARGET_ID} was not updated.")
    RESULT=$(printf '%s' "$DATA" | jq '.unresolveReviewThread.thread | {kind: "thread", id: .id, isResolved: .isResolved, file: .path}')
    ;;
  IssueComment:resolve)
    DATA=$(gh_graphql '
      mutation($id: ID!) {
        minimizeComment(input: {subjectId: $id, classifier: RESOLVED}) {
          minimizedComment { isMinimized minimizedReason }
        }
      }' "$VARIABLES" "comment ${TARGET_ID} was not resolved.")
    RESULT=$(printf '%s' "$DATA" | jq --arg id "$TARGET_ID" "${GH_COMMENT_RESOLVED_JQ}"' .minimizeComment.minimizedComment
      | {kind: "general", id: $id, isResolved: comment_resolved}')
    ;;
  IssueComment:unresolve)
    DATA=$(gh_graphql '
      mutation($id: ID!) { unminimizeComment(input: {subjectId: $id}) { unminimizedComment { isMinimized minimizedReason } } }' \
      "$VARIABLES" "comment ${TARGET_ID} was not reopened.")
    RESULT=$(printf '%s' "$DATA" | jq --arg id "$TARGET_ID" "${GH_COMMENT_RESOLVED_JQ}"' .unminimizeComment.unminimizedComment
      | {kind: "general", id: $id, isResolved: comment_resolved}')
    ;;
  *)
    echo "ERROR: '${TARGET_ID}' is not a review thread (PRRT_…) or a general comment (IC_…) (found: ${KIND:-nothing})." >&2
    exit 1
    ;;
esac

# The reported state is the check: a mutation that answers without the new state did not take.
EXPECTED=$([[ "$MODE" == resolve ]] && echo true || echo false)
printf '%s' "$RESULT" | jq -e --argjson expected "$EXPECTED" '.isResolved == $expected' >/dev/null \
  || fail_with_body "$RESULT" "${TARGET_ID} does not report the ${MODE}d state."
printf '%s\n' "$RESULT"
