#!/usr/bin/env bash
# update_description.sh — Overwrite the description of a GitHub pull request
# Usage: bash update_description.sh <OWNER/REPO> <PR> <DESCRIPTION>

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
PR="${2:?Missing PR}"
DESCRIPTION="${3:?Missing DESCRIPTION}"

gh_require_pr_number "$PR"
gh_init "$REPOSITORY"
warn_body_text "the description" "$DESCRIPTION"

BODY=$(jq -n --arg body "$DESCRIPTION" '{body: $body}')
RESPONSE=$(gh_api PATCH "${REPO_URL}/pulls/${PR}" "$BODY") \
  || fail_with_body "$RESPONSE" "pull request description was not updated."
require_json_field "$RESPONSE" '.number' "pull request description was not updated."

printf '%s' "$RESPONSE" | jq '{id: .number, title: .title, description: .body}'
