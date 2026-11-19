#!/usr/bin/env bash
# edit_comment.sh — Replace the text of an existing comment on a GitHub pull request
# Usage: bash edit_comment.sh <GITHUB_TOKEN> <OWNER/REPO> <COMMENT_ID> <COMMENT>
#
# COMMENT_ID is a comment id as review prints it: IC_… for a general comment, PRRC_… for a review
# comment. The comment keeps its id, position and replies. GitHub lets only the author, or a
# maintainer, edit it.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
COMMENT_ID="${3:?Missing COMMENT_ID}"
COMMENT="${4:?Missing COMMENT}"

gh_init "$TOKEN" "$REPOSITORY"
warn_body_text "the comment" "$COMMENT"

# The id alone names its kind: an issue comment (a general comment) and a review comment use
# different mutations, and asking the node for its type avoids parsing the id format.
KIND=$(gh_graphql 'query($id: ID!) { node(id: $id) { __typename } }' \
  "$(jq -n --arg id "$COMMENT_ID" '{id: $id}')" "comment ${COMMENT_ID} was not found." \
  | jq -r '.node.__typename // empty')
VARIABLES=$(jq -n --arg id "$COMMENT_ID" --arg body "$COMMENT" '{id: $id, body: $body}')
case "$KIND" in
  IssueComment)
    DATA=$(gh_graphql '
      mutation($id: ID!, $body: String!) {
        updateIssueComment(input: {id: $id, body: $body}) { issueComment { id body } }
      }' "$VARIABLES" "comment ${COMMENT_ID} was not updated.")
    COMMENT_JSON=$(printf '%s' "$DATA" | jq '.updateIssueComment.issueComment')
    ;;
  PullRequestReviewComment)
    DATA=$(gh_graphql '
      mutation($id: ID!, $body: String!) {
        updatePullRequestReviewComment(input: {pullRequestReviewCommentId: $id, body: $body}) {
          pullRequestReviewComment { id body }
        }
      }' "$VARIABLES" "comment ${COMMENT_ID} was not updated.")
    COMMENT_JSON=$(printf '%s' "$DATA" | jq '.updatePullRequestReviewComment.pullRequestReviewComment')
    ;;
  *)
    echo "ERROR: '${COMMENT_ID}' is not a pull request comment (found: ${KIND:-nothing})." >&2
    exit 1
    ;;
esac

# The stored text is the check: anything but the new content means the edit did not take effect.
printf '%s' "$COMMENT_JSON" | jq -e --arg content "$COMMENT" '.body == $content' >/dev/null \
  || fail_with_body "$COMMENT_JSON" "comment ${COMMENT_ID} does not hold the new text."

printf '%s' "$COMMENT_JSON" | jq '{id: .id, content: .body}'
