#!/usr/bin/env bash
# retarget_pr.sh — Change the target (base) branch of a GitHub pull request
# Usage: bash retarget_pr.sh <OWNER/REPO> <PR> <TARGET_BRANCH>
#
# The target may be a short branch name or a refs/heads/ ref. GitHub recomputes the PR's diff and
# merge base against the new target; the head branch is not touched, so rebase it onto the new
# target first when the old and new targets have diverged, or the PR shows the difference.

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
PR="${2:?Missing PR}"
TARGET="${3:?Missing TARGET_BRANCH}"

gh_require_pr_number "$PR"
gh_init "$REPOSITORY"

BODY=$(jq -n --arg base "$(gh_ref_to_branch "$TARGET")" '{base: $base}')
# 422: the branch does not exist, is the head branch itself, the PR is closed, or the PR is in a stack
# (run `stack unstack` first). fail_with_body prints the reason.
RESPONSE=$(gh_api PATCH "${REPO_URL}/pulls/${PR}" "$BODY") \
  || fail_with_body "$RESPONSE" "pull request ${PR} was not retargeted."
require_json_field "$RESPONSE" '.number' "pull request ${PR} was not retargeted."

printf '%s' "$RESPONSE" | jq '{id: .number, source: .head.ref, target: .base.ref, status: .state}'
