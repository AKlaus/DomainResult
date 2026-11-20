#!/usr/bin/env bash
# create_pull_request.sh — Create a GitHub pull request
# Usage: bash create_pull_request.sh <OWNER/REPO> <SOURCE_BRANCH> <TARGET_BRANCH> <TITLE> [DESCRIPTION] [--ready]
#        Use -- before a DESCRIPTION that begins with --. Branches may be short names or refs/heads/ refs.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
SOURCE_BRANCH="${2:?Missing SOURCE_BRANCH}"
TARGET_BRANCH="${3:?Missing TARGET_BRANCH}"
TITLE="${4:?Missing TITLE}"
shift 4

DESCRIPTION=""
DESCRIPTION_SET=false
IS_DRAFT=true

while (( $# > 0 )); do
  case "$1" in
    --)
      shift
      while (( $# > 0 )); do
        if [[ "$DESCRIPTION_SET" == true ]]; then
          echo "ERROR: create accepts only one description." >&2
          exit 1
        fi
        DESCRIPTION="$1"
        DESCRIPTION_SET=true
        shift
      done
      break
      ;;
    --ready)
      IS_DRAFT=false
      ;;
    --*)
      echo "ERROR: unknown create option '$1'. Valid options: --ready, --." >&2
      exit 1
      ;;
    *)
      if [[ "$DESCRIPTION_SET" == true ]]; then
        echo "ERROR: create accepts only one description." >&2
        exit 1
      fi
      DESCRIPTION="$1"
      DESCRIPTION_SET=true
      ;;
  esac
  shift
done

gh_init "$REPOSITORY"
warn_unexpanded_quoting "the title" "$TITLE"
warn_body_text "the description" "$DESCRIPTION"

BODY=$(jq -n \
  --arg head "$(gh_ref_to_branch "$SOURCE_BRANCH")" \
  --arg base "$(gh_ref_to_branch "$TARGET_BRANCH")" \
  --arg title "$TITLE" \
  --arg body "$DESCRIPTION" \
  --argjson draft "$IS_DRAFT" \
  '{head: $head, base: $base, title: $title, body: $body, draft: $draft}')

RESPONSE=$(gh_api POST "${REPO_URL}/pulls" "$BODY") || fail_with_body "$RESPONSE" "pull request was not created."
require_json_field "$RESPONSE" '.number' "pull request was not created."

printf '%s' "$RESPONSE" \
  | jq '{
      id: .number,
      title: .title,
      source: .head.ref,
      target: .base.ref,
      status: .state,
      isDraft: .draft,
      url: .html_url
    }'
