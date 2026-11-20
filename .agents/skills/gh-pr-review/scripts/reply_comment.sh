#!/usr/bin/env bash
# reply_comment.sh — Reply to a review thread or a general comment on a GitHub pull request
# Usage: bash reply_comment.sh <OWNER/REPO> <ID> <COMMENT>
#
# ID is an id as review prints it. A review thread (PRRT_…) gets the reply nested in the thread. A
# general comment (IC_…) has no thread on GitHub, so the reply is a new general comment that quotes
# the first line of the original, links to it, and mentions its author: GitHub's own "Quote reply"
# form, which notifies the author as a thread reply would.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
TARGET_ID="${2:?Missing THREAD_OR_COMMENT_ID}"
COMMENT="${3:?Missing COMMENT}"

gh_init "$REPOSITORY"
warn_body_text "the reply" "$COMMENT"

NODE=$(gh_graphql '
  query($id: ID!) {
    node(id: $id) {
      __typename
      ... on IssueComment { url body author { login } pullRequest { id } }
    }
  }' "$(jq -n --arg id "$TARGET_ID" '{id: $id}')" "'${TARGET_ID}' was not found.")
KIND=$(printf '%s' "$NODE" | jq -r '.node.__typename // empty')

case "$KIND" in
  PullRequestReviewThread)
    DATA=$(gh_graphql '
      mutation($thread: ID!, $body: String!) {
        addPullRequestReviewThreadReply(input: {pullRequestReviewThreadId: $thread, body: $body}) {
          comment { id body url }
        }
      }' "$(jq -n --arg thread "$TARGET_ID" --arg body "$COMMENT" '{thread: $thread, body: $body}')" \
      "reply was not posted to thread ${TARGET_ID}.")
    require_json_field "$DATA" '.addPullRequestReviewThreadReply.comment.id' "reply was not posted to thread ${TARGET_ID}."
    printf '%s' "$DATA" | jq --arg thread "$TARGET_ID" \
      '.addPullRequestReviewThreadReply.comment | {kind: "thread", inReplyTo: $thread, id: .id, content: .body, url: .url}'
    ;;
  IssueComment)
    require_json_field "$NODE" '.node.pullRequest.id' "'${TARGET_ID}' is not a comment on a pull request."
    # The quote is the first non-empty line of the Markdown body that is not itself a quote, so a
    # reply to a quote-reply quotes its answer rather than its "@x wrote" lead. It comes from the
    # Markdown, not the rendered text, so a code span such as `@name` or `#12` stays code instead
    # of turning into a live mention or cross-reference. The line is cut at 200 characters, and a
    # code span that the cut leaves open is closed. A deleted account has no login, so the mention
    # falls back to a link.
    BODY=$(printf '%s' "$NODE" | jq --arg reply "$COMMENT" '
      .node as $c
      | ($c.body | split("\n") | map(select(test("\\S") and (test("^\\s*>") | not))) | first // "")
          as $line
      | (if ($line | length) > 200 then $line[0:200] + "…" else $line end) as $cut
      | (if ([$cut | scan("`")] | length) % 2 == 1 then $cut + "`" else $cut end) as $quote
      | (if $c.author.login then "@\($c.author.login) [wrote](\($c.url)):" else "[Comment](\($c.url)):" end) as $lead
      | "> \($lead)\n> \($quote)\n\n\($reply)"')
    # The reply goes to the pull request that holds the comment, as the lookup names it, rather than
    # to a PR number in the configured repository: an id from another repository would otherwise
    # land on an unrelated PR with the same number.
    DATA=$(gh_graphql '
      mutation($subject: ID!, $body: String!) {
        addComment(input: {subjectId: $subject, body: $body}) { commentEdge { node { id body url } } }
      }' "$(printf '%s' "$NODE" | jq --argjson body "$BODY" '{subject: .node.pullRequest.id, body: $body}')" \
      "reply to ${TARGET_ID} was not posted.")
    require_json_field "$DATA" '.addComment.commentEdge.node.id' "reply to ${TARGET_ID} was not posted."
    printf '%s' "$DATA" | jq --arg target "$TARGET_ID" \
      '.addComment.commentEdge.node | {kind: "general", inReplyTo: $target, id: .id, content: .body, url: .url}'
    ;;
  *)
    echo "ERROR: '${TARGET_ID}' is not a review thread (PRRT_…) or a general comment (IC_…) (found: ${KIND:-nothing})." >&2
    echo "       To answer a review comment (PRRC_…), reply to its thread." >&2
    exit 1
    ;;
esac
