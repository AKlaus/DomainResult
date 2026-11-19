#!/usr/bin/env bash
# fetch_file.sh — Fetch the content of a file at a branch or commit
# Usage: bash fetch_file.sh <GITHUB_TOKEN> <OWNER/REPO> <FILE_PATH> <REF>
#
# Use this after review_pr.sh to read changed file contents. FILE_PATH is repository-relative; a
# leading / is accepted and dropped. REF is a branch name, a refs/heads/ ref, or a commit SHA.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
FILE_PATH="${3:?Missing FILE_PATH}"
REF="${4:?Missing REF}"

gh_init "$TOKEN" "$REPOSITORY"

# Each path segment is encoded on its own, so the slashes stay separators. The ref is encoded
# whole: # in a branch name must become %23, or the URL parser reads it as a fragment and
# truncates the ref. Encoded with jq rather than python3: on Windows the `python3` name is
# shadowed by the Store app-execution alias, which exits 49 without running anything.
ENCODED_PATH=$(jq -rn --arg s "$(gh_repo_path "$FILE_PATH")" '$s | split("/") | map(@uri) | join("/")')
ENCODED_REF=$(jq -rn --arg s "$(gh_ref_to_branch "$REF")" '$s | @uri')
gh_get_raw "${REPO_URL}/contents/${ENCODED_PATH}?ref=${ENCODED_REF}"
