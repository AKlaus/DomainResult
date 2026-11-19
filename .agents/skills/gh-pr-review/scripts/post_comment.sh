#!/usr/bin/env bash
# post_comment.sh — Post a general, file-level, or line comment on a GitHub pull request
# Usage: bash post_comment.sh <GITHUB_TOKEN> <OWNER/REPO> <PR> <COMMENT> [COMMIT FILE_PATH [LINE]]
#
# Without COMMIT, posts a general comment. With COMMIT and FILE_PATH, posts a file-level review
# comment; with LINE too, a review comment on LINE of that commit's version of the file. COMMIT is
# the full SHA of the commit the reviewer read: sourceCommit from review.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
PR="${3:?Missing PR}"
COMMENT="${4:?Missing COMMENT}"
COMMIT="${5:-}"
FILE_PATH="${6:-}"
LINE="${7:-}"

gh_require_pr_number "$PR"
# Any anchor argument makes this a review comment, which needs both COMMIT and FILE_PATH. An empty
# COMMIT, such as an unset shell variable, must not quietly turn a line comment into a general one.
if [[ -n "${COMMIT}${FILE_PATH}${LINE}" && ( -z "$COMMIT" || -z "$FILE_PATH" ) ]]; then
  echo "ERROR: a review comment needs COMMIT and FILE_PATH; a general comment needs neither." >&2
  exit 1
fi
if [[ -n "$COMMIT" && ! "$COMMIT" =~ ^[0-9a-f]{40}$ ]]; then
  echo "ERROR: COMMIT must be the full 40-character SHA that review prints as sourceCommit, got '${COMMIT}'." >&2
  exit 1
fi
if [[ -n "$LINE" && ! "$LINE" =~ ^[1-9][0-9]*$ ]]; then
  echo "ERROR: LINE must be a positive line number, got '${LINE}'." >&2
  exit 1
fi

gh_init "$TOKEN" "$REPOSITORY"
warn_body_text "the comment" "$COMMENT"

if [[ -z "$COMMIT" ]]; then
  # A general comment is an issue comment: flat, with no thread. reply answers it with a quoting
  # comment, and resolve hides it as resolved.
  BODY=$(jq -n --arg body "$COMMENT" '{body: $body}')
  RESPONSE=$(gh_api POST "${REPO_URL}/issues/${PR}/comments" "$BODY") \
    || fail_with_body "$RESPONSE" "comment was not posted."
  require_json_field "$RESPONSE" '.node_id' "comment was not posted."
  printf '%s' "$RESPONSE" | jq '{id: .node_id, kind: "general", url: .html_url}'
  exit 0
fi

# A review comment is anchored to the commit the reviewer read, not to the head at posting time: a
# push in between would otherwise move it to another line, or fail it with 422. GitHub marks it
# outdated once a later commit changes the line.

if [[ -n "$LINE" ]]; then
  BODY=$(jq -n --arg body "$COMMENT" --arg commit "$COMMIT" --arg path "$(gh_repo_path "$FILE_PATH")" \
    --argjson line "$LINE" '{body: $body, commit_id: $commit, path: $path, line: $line, side: "RIGHT"}')
else
  BODY=$(jq -n --arg body "$COMMENT" --arg commit "$COMMIT" --arg path "$(gh_repo_path "$FILE_PATH")" \
    '{body: $body, commit_id: $commit, path: $path, subject_type: "file"}')
fi

# GitHub answers 422 when a line lies outside every diff hunk, and when the account has an
# unsubmitted (pending) review on the PR, which blocks a standalone review comment. fail_with_body
# prints the cause from .errors; the hint names the way out for the two known causes.
if ! RESPONSE=$(gh_api POST "${REPO_URL}/pulls/${PR}/comments" "$BODY"); then
  case "$RESPONSE" in
    *'pending review'*)
      echo "HINT: your account has a pending review on PR ${PR}. Submit or discard it on GitHub or in" >&2
      echo "      your editor, or post a general comment with a permalink to the line." >&2
      ;;
    *'part of the diff'*)
      echo "HINT: a line comment needs a line inside a diff hunk. Omit LINE for a file-level comment on a" >&2
      echo "      changed file, or post a general comment with a permalink to the line." >&2
      ;;
  esac
  fail_with_body "$RESPONSE" "review comment was not posted."
fi
require_json_field "$RESPONSE" '.node_id' "review comment was not posted."

printf '%s' "$RESPONSE" \
  | jq '{id: .node_id, kind: "review", file: .path, line: .line, url: .html_url}'
