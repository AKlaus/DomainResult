#!/usr/bin/env bash
# stack_prs.sh — Read and manage GitHub pull request stacks
# Usage: bash stack_prs.sh <GITHUB_TOKEN> <OWNER/REPO> list
#        bash stack_prs.sh <GITHUB_TOKEN> <OWNER/REPO> show <pr>
#        bash stack_prs.sh <GITHUB_TOKEN> <OWNER/REPO> create <pr> [<pr> ...]   # bottom to top
#        bash stack_prs.sh <GITHUB_TOKEN> <OWNER/REPO> add <stack> <pr> [<pr> ...]   # above the current top
#        bash stack_prs.sh <GITHUB_TOKEN> <OWNER/REPO> unstack <stack>
#
# A stack is GitHub's own object, numbered per repository; `show <pr>` finds the one that holds a
# pull request. `create` and `add` take pull request numbers from the bottom up, and GitHub
# requires each one's target branch to be the head branch of the one below it, so retarget first.
# `unstack` removes every unmerged pull request from a stack; the pull requests themselves stay
# open with their targets unchanged. Merged and queued pull requests stay in the stack, which is
# then closed, so a stack whose bottom has merged survives `unstack` holding that merged pull
# request alone; GitHub dissolves the stack (204) only when nothing remains.
#
# Every stack response is printed in one shape: the stack number, its base branch, whether it is
# open, and its pull requests from the bottom up with their state, head branch and head commit.

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

TOKEN="${1:?Missing GITHUB_TOKEN}"
REPOSITORY="${2:?Missing GITHUB_REPOSITORY}"
SUBCOMMAND="${3:?Missing subcommand: list | show | create | add | unstack}"
shift 3

gh_init "$TOKEN" "$REPOSITORY"
GH_API_VERSION="$GH_STACK_API_VERSION"

# STACK_JQ — the one shape a stack prints in, and the status of a pull request in it, shared by
# every subcommand so that list, show and the unstack note cannot disagree.
STACK_JQ='
  def pr_status: if .merged_at then "merged" else .state end;
  def stack: {
    stack: .number, base: .base.ref, open: .open,
    pullRequests: [.pull_requests[] | {id: .number, status: pr_status, isDraft: .draft, head: .head.ref, headCommit: .head.sha}]
  };'

# print_stack <stack json>
print_stack() {
  printf '%s' "$1" | jq "${STACK_JQ} stack"
}

# require_stack_number <value>
require_stack_number() {
  [[ "$1" =~ ^[1-9][0-9]*$ ]] || { echo "ERROR: STACK must be a positive integer, got '$1'." >&2; exit 1; }
}

# pull_requests_body <pr>...
pull_requests_body() {
  for pr in "$@"; do gh_require_pr_number "$pr"; done
  jq -n --args '{pull_requests: ($ARGS.positional | map(tonumber))}' "$@"
}

case "$SUBCOMMAND" in
  list)
    (( $# == 0 )) || { echo "ERROR: list takes no argument." >&2; exit 1; }
    RESPONSE=$(gh_get_pages "${REPO_URL}/stacks?" "pull request stacks were not listed.")
    printf '%s' "$RESPONSE" | jq "${STACK_JQ} [.[] | stack]"
    ;;
  show)
    PR="${1:?Missing PR}"
    (( $# == 1 )) || { echo "ERROR: show takes one pull request number." >&2; exit 1; }
    gh_require_pr_number "$PR"
    RESPONSE=$(gh_get "${REPO_URL}/stacks?pull_request=${PR}&per_page=100" "the stack of pull request ${PR} was not read.")
    require_json_field "$RESPONSE" 'arrays' "the stack of pull request ${PR} was not read."
    if printf '%s' "$RESPONSE" | jq -e 'length == 0' >/dev/null; then
      echo "ERROR: pull request ${PR} is not in a stack." >&2
      exit 1
    fi
    print_stack "$(printf '%s' "$RESPONSE" | jq '.[0]')"
    ;;
  create)
    (( $# >= 2 )) || { echo "ERROR: create takes at least two pull request numbers, bottom first." >&2; exit 1; }
    BODY=$(pull_requests_body "$@")
    # 422: a listed pull request does not exist, or its target is not the head of the one below it.
    RESPONSE=$(gh_api POST "${REPO_URL}/stacks" "$BODY") \
      || fail_with_body "$RESPONSE" "the stack was not created."
    require_json_field "$RESPONSE" '.number' "the stack was not created."
    print_stack "$RESPONSE"
    ;;
  add)
    STACK="${1:?Missing STACK}"
    shift
    require_stack_number "$STACK"
    (( $# >= 1 )) || { echo "ERROR: add takes the stack number and at least one pull request number." >&2; exit 1; }
    BODY=$(pull_requests_body "$@")
    # 409: another request is changing the stack. 422: the first listed pull request does not target the stack's top.
    RESPONSE=$(gh_api POST "${REPO_URL}/stacks/${STACK}/add" "$BODY") \
      || fail_with_body "$RESPONSE" "the pull requests were not added to stack ${STACK}."
    require_json_field "$RESPONSE" '.number' "the pull requests were not added to stack ${STACK}."
    print_stack "$RESPONSE"
    ;;
  unstack)
    STACK="${1:?Missing STACK}"
    (( $# == 1 )) || { echo "ERROR: unstack takes one stack number." >&2; exit 1; }
    require_stack_number "$STACK"
    # 200: merged or queued pull requests stay and the stack remains. 204: the stack is dissolved.
    RESPONSE=$(gh_api POST "${REPO_URL}/stacks/${STACK}/unstack") \
      || fail_with_body "$RESPONSE" "stack ${STACK} was not unstacked."
    if [[ -z "$RESPONSE" ]]; then
      jq -n --argjson stack "$STACK" '{stack: $stack, dissolved: true}'
    else
      require_json_field "$RESPONSE" '.number' "stack ${STACK} was not unstacked."
      echo "NOTE: stack ${STACK} still holds $(printf '%s' "$RESPONSE" | jq -r "${STACK_JQ}"' [.pull_requests[] | "#\(.number) (\(pr_status))"] | join(", ")'); merged and queued pull requests are not unstacked." >&2
      print_stack "$RESPONSE"
    fi
    ;;
  *)
    echo "ERROR: Unknown stack subcommand '${SUBCOMMAND}'. Valid: list | show | create | add | unstack" >&2
    exit 1
    ;;
esac
