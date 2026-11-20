#!/usr/bin/env bash
# vote_pr.sh — Submit a review on a GitHub pull request
# Usage: bash vote_pr.sh <OWNER/REPO> <PR> <approve|request-changes|comment> [BODY]
#
# approve submits APPROVE, request-changes REQUEST_CHANGES, and comment COMMENT. The last two need
# a BODY. A submitted review stays; a later review by the same user replaces its state.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
PR="${2:?Missing PR}"
VOTE="${3:?Missing EVENT}"
REVIEW_BODY="${4:-}"

gh_require_pr_number "$PR"
case "$VOTE" in
  approve)         EVENT=APPROVE ;;
  request-changes) EVENT=REQUEST_CHANGES ;;
  comment)         EVENT=COMMENT ;;
  *)
    echo "ERROR: invalid vote '${VOTE}'. Valid: approve, request-changes, comment." >&2
    exit 1
    ;;
esac
# GitHub rejects these two events without a body, so the error is raised before the request.
if [[ "$EVENT" != APPROVE && -z "$REVIEW_BODY" ]]; then
  echo "ERROR: ${VOTE} needs a body that states the reason." >&2
  exit 1
fi

gh_init "$REPOSITORY"
warn_body_text "the review" "$REVIEW_BODY"

BODY=$(jq -n --arg event "$EVENT" --arg body "$REVIEW_BODY" \
  '{event: $event} + (if $body == "" then {} else {body: $body} end)')

# GitHub refuses an approval or a change request from the PR's author with HTTP 422; the reason
# is in .errors, which fail_with_body prints.
RESPONSE=$(gh_api POST "${REPO_URL}/pulls/${PR}/reviews" "$BODY") \
  || fail_with_body "$RESPONSE" "review was not submitted on PR ${PR}."
require_json_field "$RESPONSE" '.state' "review was not submitted on PR ${PR}."

printf '%s' "$RESPONSE" | jq '{id: .node_id, reviewer: .user.login, state: .state, url: .html_url}'
