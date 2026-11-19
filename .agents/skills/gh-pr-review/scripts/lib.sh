#!/usr/bin/env bash
# lib.sh — helpers for the GitHub pull request scripts.
#
# This file is *sourced* by the worker scripts, never executed directly. It sets no
# `set -euo pipefail` of its own — it inherits the caller's shell options — and defines only
# functions and the REPO_URL/GRAPHQL_URL/GH_AUTH_TOKEN/GH_OWNER/GH_REPO globals via gh_init.
#
# Every worker sources it with:
#   source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

# The shared scripts sit in .agents/skills/_shared. The path is resolved physically (pwd -P),
# because a .claude/skills/<skill>/ folder can be a set of symlinks into .agents/skills/<skill>/.
SHARED_SCRIPTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)/../../_shared/scripts"
if [[ ! -f "${SHARED_SCRIPTS_DIR}/common.sh" ]]; then
  echo "ERROR: the shared skill scripts were not found at ${SHARED_SCRIPTS_DIR}." >&2
  echo "       Keep .agents/skills/_shared beside the skill folder, or link it beside a copied one." >&2
  exit 1
fi
source "${SHARED_SCRIPTS_DIR}/common.sh"

GH_API_URL='https://api.github.com'
GH_API_VERSION='2022-11-28'
# GH_STACK_API_VERSION — the API version that holds the stack endpoints, the asynchronous merge
# endpoints and the `stack` object on a pull request. complete and stack switch to it.
GH_STACK_API_VERSION='2026-03-10'

# gh_init <TOKEN> <OWNER/REPO>
# Validates the two arguments common to every script and populates the globals the other helpers
# rely on. REPO_URL is the repository-scoped REST root.
gh_init() {
  GH_AUTH_TOKEN="${1:?Missing GITHUB_TOKEN}"
  local repository="${2:?Missing GITHUB_REPOSITORY}"
  if [[ ! "$repository" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
    echo "ERROR: GITHUB_REPOSITORY must be <owner>/<repo>, got '${repository}'." >&2
    exit 1
  fi
  GH_OWNER="${repository%%/*}"
  GH_REPO="${repository#*/}"
  REPO_URL="${GH_API_URL}/repos/${GH_OWNER}/${GH_REPO}"
  GRAPHQL_URL="${GH_API_URL}/graphql"
}

# gh_api <VERB> <url> [body]
# Authenticated REST request. Prints the response body on stdout and returns non-zero on an HTTP
# error, with the error body still on stdout so the caller can pass it to fail_with_body.
#
# A body is sent on stdin via --data-binary @-, not as a `-d "$body"` argument: on Windows a curl
# argument containing non-ASCII text is re-decoded via the ANSI codepage, so an em dash or curly
# quote would reach the API as invalid UTF-8.
gh_api() {
  local verb="$1" url="$2"
  local args=(--fail-with-body -sS -X "$verb"
    -H "Authorization: Bearer ${GH_AUTH_TOKEN}"
    -H 'Accept: application/vnd.github+json'
    -H "X-GitHub-Api-Version: ${GH_API_VERSION}")
  if (( $# >= 3 )); then
    printf '%s' "$3" | curl "${args[@]}" -H 'Content-Type: application/json' --data-binary @- "$url"
  else
    curl "${args[@]}" "$url"
  fi
}

# gh_get <url> <message>
# REST GET that prints the body, or fails with <message> and the error detail.
gh_get() {
  local response
  response=$(gh_api GET "$1") || fail_with_body "$response" "$2"
  printf '%s' "$response"
}

# gh_get_raw <url>
# GET with the raw media type, for file content. Streams the body unchanged; an HTTP error returns
# non-zero with the error body on stdout, so a failed fetch cannot be mistaken for file content.
gh_get_raw() {
  curl --fail-with-body -sS \
    -H "Authorization: Bearer ${GH_AUTH_TOKEN}" \
    -H 'Accept: application/vnd.github.raw+json' \
    -H "X-GitHub-Api-Version: ${GH_API_VERSION}" \
    "$1"
}

# gh_get_pages <url> <message>
# GET every page of a REST list endpoint and print one JSON array. <url> must already hold a query
# string. Pages hold 100 items; a shorter page is the last. Nothing is printed if a page fails, so
# a caller never reads a partial list as the whole.
gh_get_pages() {
  local url="$1" message="$2" page=1 page_body count items='[]'
  while true; do
    page_body=$(gh_get "${url}&per_page=100&page=${page}" "$message")
    require_json_field "$page_body" 'arrays' "$message"
    items=$(printf '%s\n%s\n' "$items" "$page_body" | jq -s '.[0] + .[1]')
    count=$(printf '%s' "$page_body" | jq 'length')
    (( count < 100 )) && break
    page=$((page + 1))
  done
  printf '%s' "$items"
}

# gh_graphql <query> <variables_json> <message>
# Run one GraphQL operation and print its `.data`. GraphQL reports most failures with HTTP 200 and
# an `.errors` array, so a response with errors fails here too, rather than reading as empty data.
gh_graphql() {
  local query="$1" variables="$2" message="$3" body response
  body=$(jq -n --arg query "$query" --argjson variables "$variables" '{query: $query, variables: $variables}')
  response=$(gh_api POST "$GRAPHQL_URL" "$body") || fail_with_body "$response" "$message"
  if ! printf '%s' "$response" | jq -e '(.errors // []) | length == 0' >/dev/null 2>&1; then
    fail_with_body "$response" "$message"
  fi
  require_json_field "$response" '.data' "$message"
  printf '%s' "$response" | jq '.data'
}

# gh_git <git arguments>...
# Run git with the token as the credential for https://github.com only. The helper reads the token
# from the environment of this one command, so it stays out of the command line and the Git config.
# It answers no other protocol or host, so a remote elsewhere never receives the token. A remote
# over SSH ignores the helper.
gh_git() {
  # shellcheck disable=SC2016 # The helper's variables expand when Git runs it, not here.
  local helper='!f() { test "$1" = get || exit 0; protocol=; host=; while IFS== read -r key value; do case "$key" in protocol) protocol=$value ;; host) host=$value ;; "") break ;; esac; done; test "$protocol" = https && test "$host" = github.com || exit 0; echo username=x-access-token; echo "password=${GH_GIT_TOKEN}"; }; f'
  # No terminal prompt: when GitHub rejects the token, Git fails instead of waiting for a username.
  GH_GIT_TOKEN="$GH_AUTH_TOKEN" GIT_TERMINAL_PROMPT=0 git -c credential.helper= -c "credential.helper=${helper}" "$@"
}

# gh_require_pr_number <value>
# Reject a pull request number that is not a positive integer before it reaches a URL.
gh_require_pr_number() {
  if [[ ! "$1" =~ ^[1-9][0-9]*$ ]]; then
    echo "ERROR: '$1' is not a pull request number." >&2
    exit 1
  fi
}

# gh_ref_to_branch <ref>
# Strip a refs/heads/ prefix, so a caller can pass a full ref as well as a branch name.
gh_ref_to_branch() {
  printf '%s' "${1#refs/heads/}"
}

# gh_repo_path <path>
# GitHub paths are repository-relative, so strip a leading slash a caller may pass.
gh_repo_path() {
  printf '%s' "${1#/}"
}

# warn_unlinked_pr_reference <label> <text>
# Warn on stderr when names_unlinked_pr matches <text>, and return 0 regardless. GitHub links only
# the #12 form. The text is not rewritten: it can quote a commit subject, and that quotation must
# post as written. Titles are left out: GitHub appends " (#<n>)" to the squash commit itself.
warn_unlinked_pr_reference() {
  names_unlinked_pr "$2" || return 0
  printf 'WARNING: %s names a pull request as plain text, which GitHub does not link.\n' "$1" >&2
  printf '         Write #<n>, such as #42. Ignore this if the text quotes a commit subject.\n' >&2
  printf '         See "References" in .agents/skills/gh-pr-review/references/writing.md.\n' >&2
}

# GH_COMMENT_RESOLVED_JQ — the jq definition of a resolved general comment, shared by review and
# resolve so that the two cannot disagree. A general comment has no thread; resolve hides it with
# the classifier RESOLVED, which GitHub reports back as the minimizedReason "resolved".
GH_COMMENT_RESOLVED_JQ='def comment_resolved: .isMinimized and .minimizedReason == "resolved";'

# warn_body_text <label> <text>
# Run every guard that applies to body text: descriptions, comments, and replies. A worker calls
# this for a body and warn_unexpanded_quoting for a title, so that a guard added later reaches
# every body without an edit to each worker.
warn_body_text() {
  warn_unexpanded_quoting "$1" "$2"
  warn_unlinked_pr_reference "$1" "$2"
}
