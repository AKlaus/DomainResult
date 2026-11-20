#!/usr/bin/env bash
# list_prs.sh — List pull requests in the GitHub repository
# Usage: bash list_prs.sh <OWNER/REPO> [STATUS]
#
# STATUS: open (default) | merged | closed | all. closed lists PRs closed without a merge.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
STATUS="${2:-open}"

# GitHub knows open and closed; merged is a closed PR with merged_at set. The statuses split closed
# in two, so a merged PR and an abandoned one can be listed apart.
case "$STATUS" in
  open)   API_STATE=open ;;
  merged) API_STATE=closed ;;
  closed) API_STATE=closed ;;
  all)    API_STATE=all ;;
  *)
    echo "ERROR: invalid status '${STATUS}'. Valid: open, merged, closed, all." >&2
    exit 1
    ;;
esac

gh_init "$REPOSITORY"

PR_ITEMS=$(gh_get_pages "${REPO_URL}/pulls?state=${API_STATE}" "Pull requests were not listed.")

printf '%s' "$PR_ITEMS" | jq --arg status "$STATUS" '[.[]
    | . + {status: (if .merged_at then "merged" elif .state == "closed" then "closed" else "open" end)}
    | select($status == "all" or .status == $status)
    | {
        id: .number,
        title: .title,
        author: .user.login,
        source: .head.ref,
        target: .base.ref,
        status: .status,
        isDraft: .draft,
        created: .created_at,
        reviewers: ([.requested_reviewers[]?.login] + [.requested_teams[]?.slug])
      }]'
