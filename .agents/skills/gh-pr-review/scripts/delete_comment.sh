#!/usr/bin/env bash
# delete_comment.sh — Delete a comment from a GitHub pull request
# Usage: bash delete_comment.sh <OWNER/REPO> <COMMENT_ID>
#
# COMMENT_ID is IC_… (general comment) or PRRC_… (review comment), as review prints it. Deleting
# the only comment of a review thread removes the thread.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
COMMENT_ID="${2:?Missing COMMENT_ID}"

gh_init "$REPOSITORY"

# The id alone names its kind: an issue comment (a general comment) and a review comment use
# different mutations, and asking the node for its type avoids parsing the id format.
KIND=$(gh_graphql 'query($id: ID!) { node(id: $id) { __typename } }' \
  "$(jq -n --arg id "$COMMENT_ID" '{id: $id}')" "comment ${COMMENT_ID} was not found." \
  | jq -r '.node.__typename // empty')
VARIABLES=$(jq -n --arg id "$COMMENT_ID" '{id: $id}')
case "$KIND" in
  IssueComment)
    gh_graphql 'mutation($id: ID!) { deleteIssueComment(input: {id: $id}) { clientMutationId } }' \
      "$VARIABLES" "comment ${COMMENT_ID} was not deleted." >/dev/null
    ;;
  PullRequestReviewComment)
    gh_graphql 'mutation($id: ID!) { deletePullRequestReviewComment(input: {id: $id}) { clientMutationId } }' \
      "$VARIABLES" "comment ${COMMENT_ID} was not deleted." >/dev/null
    ;;
  *)
    echo "ERROR: '${COMMENT_ID}' is not a pull request comment (found: ${KIND:-nothing})." >&2
    exit 1
    ;;
esac

jq -n --arg id "$COMMENT_ID" '{deleted: true, id: $id}'
