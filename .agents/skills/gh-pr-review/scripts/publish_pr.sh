#!/usr/bin/env bash
# publish_pr.sh — Take a GitHub pull request out of draft, so reviewers are asked for review
# Usage: bash publish_pr.sh <GITHUB_TOKEN> <OWNER/REPO> <PR>

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
PR="${3:?Missing PR}"

gh_require_pr_number "$PR"
gh_init "$TOKEN" "$REPOSITORY"

PULL=$(gh_get "${REPO_URL}/pulls/${PR}" "pull request ${PR} was not found.")

# The REST API cannot clear the draft flag; only the GraphQL mutation can. A PR that is already
# open for review needs no call, and the reported state is the same either way.
if printf '%s' "$PULL" | jq -e '.draft == true' >/dev/null; then
  NODE_ID=$(printf '%s' "$PULL" | jq -r '.node_id')
  DATA=$(gh_graphql '
    mutation($id: ID!) {
      markPullRequestReadyForReview(input: {pullRequestId: $id}) { pullRequest { isDraft } }
    }' "$(jq -n --arg id "$NODE_ID" '{id: $id}')" "pull request ${PR} was not published.")
  require_json_field "$DATA" '.markPullRequestReadyForReview.pullRequest.isDraft == false' \
    "pull request ${PR} was not published."
fi

printf '%s' "$PULL" | jq '{id: .number, title: .title, isDraft: false, status: .state, url: .html_url}'
