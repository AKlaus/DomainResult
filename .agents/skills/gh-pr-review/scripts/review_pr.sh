#!/usr/bin/env bash
# review_pr.sh — Fetch a GitHub pull request with its snapshot commits, changed files and discussion
# Usage: bash review_pr.sh <GITHUB_TOKEN> <OWNER/REPO> <PR>
#
# Prints the PR metadata, the snapshot commits (merge base and head), the changed files, the
# review threads, the general comments, and the review bodies. REST lists and GraphQL connections
# are paged to the end; each thread carries its first 100 comments. Ids are GraphQL node ids:
# PRRT_ for a thread, PRRC_ for a review comment, IC_ for a general comment, PRR_ for a review.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
PR="${3:?Missing PR}"

gh_require_pr_number "$PR"
gh_init "$TOKEN" "$REPOSITORY"

# graphql_connection <query> <jq path to the connection> <message>
# Run <query> once per page of the connection at <path>, following pageInfo.endCursor through the
# $after variable, and print all its nodes as one array. GraphQL returns at most 100 nodes a page.
graphql_connection() {
  local query="$1" path="$2" message="$3" after=null page nodes='[]' variables
  while true; do
    variables=$(jq -n --arg owner "$GH_OWNER" --arg repo "$GH_REPO" --argjson number "$PR" \
      --argjson after "$after" '{owner: $owner, repo: $repo, number: $number, after: $after}')
    page=$(gh_graphql "$query" "$variables" "$message")
    require_json_field "$page" "${path} | .nodes | arrays" "$message"
    nodes=$(printf '%s\n%s\n' "$nodes" "$page" | jq -s --arg p "$path" '.[0] + (.[1] | getpath($p | ltrimstr(".") | split("."))).nodes')
    printf '%s' "$page" | jq -e "${path}.pageInfo.hasNextPage" >/dev/null || break
    after=$(printf '%s' "$page" | jq "${path}.pageInfo.endCursor")
  done
  printf '%s' "$nodes"
}

PULL=$(gh_get "${REPO_URL}/pulls/${PR}" "pull request ${PR} was not found.")
HEAD_SHA=$(printf '%s' "$PULL" | jq -r '.head.sha')

# The diff a PR shows runs from the merge base of the two branches, not from the target's tip, so
# the snapshot reports the merge base as baseCommit. An open PR is compared with its target branch
# as it is now: base.sha can name a commit that the repository no longer holds, and a compare
# against it answers 404. A closed PR keeps base.sha, the target as it was when it closed.
if [[ "$(printf '%s' "$PULL" | jq -r '.state')" == open ]]; then
  BASE_SPEC=$(printf '%s' "$PULL" | jq -r '.base.ref | @uri')
else
  BASE_SPEC=$(printf '%s' "$PULL" | jq -r '.base.sha')
fi
# per_page=1 keeps the commit list short. The response still carries patches, so only the two
# fields are kept: the whole body overflows a jq argument.
COMPARE=$(gh_get "${REPO_URL}/compare/${BASE_SPEC}...${HEAD_SHA}?per_page=1" "the PR's merge base was not resolved.")
require_json_field "$COMPARE" '.merge_base_commit.sha and .base_commit.sha' "the PR's merge base was not resolved."
MERGE_BASE_SHA=$(printf '%s' "$COMPARE" | jq -r '.merge_base_commit.sha')
TARGET_SHA=$(printf '%s' "$COMPARE" | jq -r '.base_commit.sha')

FILES=$(gh_get_pages "${REPO_URL}/pulls/${PR}/files?" "Pull request files were not listed.")

THREADS=$(graphql_connection '
  query($owner: String!, $repo: String!, $number: Int!, $after: String) {
    repository(owner: $owner, name: $repo) { pullRequest(number: $number) {
      reviewThreads(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes {
          id isResolved isOutdated path line originalLine subjectType
          comments(first: 100) { nodes {
            id body createdAt author { login __typename } originalCommit { oid }
          } }
        }
      }
    } }
  }' '.repository.pullRequest.reviewThreads' "Pull request threads were not listed.")

COMMENTS=$(graphql_connection '
  query($owner: String!, $repo: String!, $number: Int!, $after: String) {
    repository(owner: $owner, name: $repo) { pullRequest(number: $number) {
      comments(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes { id body createdAt isMinimized minimizedReason author { login __typename } }
      }
    } }
  }' '.repository.pullRequest.comments' "Pull request comments were not listed.")

REVIEWS=$(graphql_connection '
  query($owner: String!, $repo: String!, $number: Int!, $after: String) {
    repository(owner: $owner, name: $repo) { pullRequest(number: $number) {
      reviews(first: 100, after: $after) {
        pageInfo { hasNextPage endCursor }
        nodes { id body state submittedAt author { login __typename } }
      }
    } }
  }' '.repository.pullRequest.reviews' "Pull request reviews were not listed.")

# The file list and the threads always describe the PR as it is now, but the snapshot describes the
# head and target read at the start. A push or a retarget in between would pair head A's snapshot
# with head B's files, so read both again and fail rather than print a mixed review.
FINAL_PULL=$(gh_get "${REPO_URL}/pulls/${PR}" "the PR revision could not be checked.")
FINAL_COMPARE=$(gh_get "${REPO_URL}/compare/${BASE_SPEC}...${HEAD_SHA}?per_page=1" "the target revision could not be checked.")
if [[ "$(printf '%s' "$FINAL_PULL" | jq -c '[.head.sha, .base.ref, .state]')" \
      != "$(printf '%s' "$PULL" | jq -c '[.head.sha, .base.ref, .state]')" ]] ||
   [[ "$(printf '%s' "$FINAL_COMPARE" | jq -r '.base_commit.sha')" != "$TARGET_SHA" ]]; then
  echo "ERROR: PR ${PR} changed while review was reading it (a push, a retarget, or a new target commit). Run review again." >&2
  exit 1
fi

echo "=== PR METADATA ==="
# reviewers: each reviewer's latest deciding review (approved, changes requested, dismissed), plus
# the reviewers still requested. A COMMENTED review decides nothing, as in the GitHub UI.
printf '%s\n%s\n' "$PULL" "$REVIEWS" | jq -s '.[0] as $pr | .[1] as $reviews | {
    id: $pr.number,
    title: $pr.title,
    description: $pr.body,
    author: $pr.user.login,
    source: $pr.head.ref,
    target: $pr.base.ref,
    status: (if $pr.merged_at then "merged" else $pr.state end),
    isDraft: $pr.draft,
    created: $pr.created_at,
    mergeable: $pr.mergeable,
    mergeableState: $pr.mergeable_state,
    url: $pr.html_url,
    # One pair of parentheses around the whole value: jq 1.7, which macOS ships, rejects a bare
    # operator after a parenthesised object value.
    reviewers: ((
      [$reviews[] | select(.state != "COMMENTED" and .state != "PENDING" and .author != null)]
      | group_by(.author.login) | map(max_by(.submittedAt) | {name: .author.login, state: .state})
    ) + [$pr.requested_reviewers[]? | {name: .login, state: "REQUESTED"}]
      + [$pr.requested_teams[]? | {name: .slug, state: "REQUESTED"}])
  }'

echo ""
echo "=== SNAPSHOT ==="
jq -n --arg base "$MERGE_BASE_SHA" --arg source "$HEAD_SHA" --arg target "$TARGET_SHA" '{
    baseCommit: $base,
    sourceCommit: $source,
    targetCommit: $target
  }'

echo ""
echo "=== CHANGED FILES ==="
printf '%s' "$FILES" | jq '[.[] | {
    changeType: .status,
    path: .filename,
    originalPath: (.previous_filename // null)
  }]'

echo ""
echo "=== REVIEW THREADS ==="
# Comments by bots, such as Coveralls and other GitHub Apps, are left out; a thread
# with no other comment is left out with them.
printf '%s' "$THREADS" | jq '[.[] | {
    id: .id,
    isResolved: .isResolved,
    isOutdated: .isOutdated,
    file: .path,
    line: (.line // .originalLine),
    subject: (.subjectType | ascii_downcase),
    comments: [.comments.nodes[] | select(.author.__typename != "Bot") | {
      id: .id,
      author: .author.login,
      content: .body,
      date: .createdAt,
      commit: .originalCommit.oid
    }]
  } | select(.comments | length > 0)]'

echo ""
echo "=== GENERAL COMMENTS ==="
# A comment hidden for another reason than resolved, such as outdated, reports that reason.
printf '%s' "$COMMENTS" | jq "${GH_COMMENT_RESOLVED_JQ}"' [.[] | select(.author.__typename != "Bot") | {
    id: .id,
    isResolved: comment_resolved,
    hidden: (if .isMinimized then (.minimizedReason // "hidden") else null end),
    author: .author.login,
    content: .body,
    date: .createdAt
  }]'

echo ""
echo "=== REVIEWS ==="
# A review without a body only carries its line comments, which the threads above already show.
printf '%s' "$REVIEWS" | jq '[.[] | select((.body // "") != "" and .author.__typename != "Bot") | {
    id: .id,
    author: .author.login,
    state: .state,
    content: .body,
    date: .submittedAt
  }]'
