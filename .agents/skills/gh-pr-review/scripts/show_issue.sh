#!/usr/bin/env bash
# show_issue.sh — Show a GitHub issue with its comments, for the review of a PR that references it
# Usage: bash show_issue.sh <GITHUB_TOKEN> <OWNER/REPO> <ISSUE>
#
# The issues endpoint also answers for a pull request number, because GitHub numbers issues and
# pull requests together. Such a number is refused: `review` reads a pull request.

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
ISSUE="${3:?Missing ISSUE}"

if [[ ! "$ISSUE" =~ ^[1-9][0-9]*$ ]]; then
  echo "ERROR: '${ISSUE}' is not an issue number." >&2
  exit 1
fi
gh_init "$TOKEN" "$REPOSITORY"

ITEM=$(gh_get "${REPO_URL}/issues/${ISSUE}" "issue ${ISSUE} was not found.")
if printf '%s' "$ITEM" | jq -e 'has("pull_request")' >/dev/null; then
  echo "ERROR: #${ISSUE} is a pull request, not an issue. Run: run.sh review ${ISSUE}" >&2
  exit 1
fi

COMMENTS=$(gh_get_pages "${REPO_URL}/issues/${ISSUE}/comments?sort=created" \
  "the comments of issue ${ISSUE} were not listed.")

# The comments reach jq on stdin. As an argument, a long list exceeds the argument limit.
printf '%s\n%s\n' "$ITEM" "$COMMENTS" | jq -s '.[1] as $comments | .[0] | {
    id: .number,
    title: .title,
    state: .state,
    stateReason: .state_reason,
    author: .user.login,
    labels: [.labels[]?.name],
    url: .html_url,
    body: (.body // ""),
    comments: [$comments[] | {author: .user.login, created: .created_at, body: (.body // "")}]
  }'
