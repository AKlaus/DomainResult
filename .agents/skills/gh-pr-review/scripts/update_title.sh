#!/usr/bin/env bash
# update_title.sh — Overwrite the title of a GitHub pull request
# Usage: bash update_title.sh <OWNER/REPO> <PR> <TITLE>

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
PR="${2:?Missing PR}"
TITLE="${3:?Missing TITLE}"

gh_require_pr_number "$PR"
gh_init "$REPOSITORY"
warn_unexpanded_quoting "the title" "$TITLE"

BODY=$(jq -n --arg title "$TITLE" '{title: $title}')
RESPONSE=$(gh_api PATCH "${REPO_URL}/pulls/${PR}" "$BODY") \
  || fail_with_body "$RESPONSE" "pull request title was not updated."
require_json_field "$RESPONSE" '.number' "pull request title was not updated."

printf '%s' "$RESPONSE" | jq '{id: .number, title: .title}'
