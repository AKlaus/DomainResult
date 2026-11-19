#!/usr/bin/env bash
# scripts/run.sh — Single entry point for all GitHub pull request actions.
# Agents should always use this script. Never call other scripts directly.
#
# Usage:
#   bash scripts/run.sh list   [open|merged|closed|all]                   # list PRs (default: open)
#   bash scripts/run.sh create <source> <target> <title> [desc] [--ready] # create a PR (draft unless --ready; use -- before dash-prefixed desc)
#   bash scripts/run.sh review <pr>                                       # PR metadata, snapshot commits, changed files, threads, comments, reviews
#   bash scripts/run.sh issue <issue>                                     # an issue that a PR references, with its comments
#   bash scripts/run.sh scope [base] [head]                               # size a change and list its tangible changes (local Git only)
#   bash scripts/run.sh fetch <path> "<ref>"                              # fetch a file at a branch or commit
#   bash scripts/run.sh comment <pr> <message> [<commit> <file> [line]]   # general, or file-level/line comment on the reviewed commit
#   bash scripts/run.sh reply <id> <message>                              # reply in a review thread (PRRT_…), or quote-reply to a general comment (IC_…)
#   bash scripts/run.sh edit <comment_id> <message>                       # replace the text of your own comment (IC_… or PRRC_…)
#   bash scripts/run.sh delete <comment_id>                               # delete your own comment (IC_… or PRRC_…)
#   bash scripts/run.sh vote <pr> <approve|request-changes|comment> [body] # submit a review
#   bash scripts/run.sh describe <pr> <description>                       # overwrite the PR description
#   bash scripts/run.sh title <pr> <title>                                # overwrite the PR title
#   bash scripts/run.sh retarget <pr> <target>                           # change the PR's target branch (short name or refs/heads/ ref)
#   bash scripts/run.sh stack list                                       # list the repository's PR stacks
#   bash scripts/run.sh stack show <pr>                                  # the stack that holds a PR
#   bash scripts/run.sh stack create <pr> <pr> [<pr> ...]                # form a stack, bottom first; each PR must target the one below
#   bash scripts/run.sh stack add <stack> <pr> [<pr> ...]                # append PRs above a stack's top
#   bash scripts/run.sh stack unstack <stack>                            # remove the unmerged PRs from a stack
#   bash scripts/run.sh publish <pr>                                      # take a PR out of draft
#   bash scripts/run.sh complete <pr>                                     # squash-merge a PR; the commit message is its description
#   bash scripts/run.sh resolve <id>                                      # resolve a review thread (PRRT_…), or hide a general comment (IC_…) as resolved
#   bash scripts/run.sh unresolve <id>                                    # reopen a review thread, or show a hidden general comment again
#
# NOTE: always double-quote a branch argument — it may contain special
#       characters such as # (e.g. "feature/#88")

set -euo pipefail

# Git Bash rewrites POSIX-looking arguments before invoking native Windows tools.
# API paths and JSON values must reach jq/curl unchanged. This variable is ignored
# by macOS and Linux, and every child script inherits it from this entry point.
export MSYS2_ARG_CONV_EXCL='*'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

source "${SCRIPT_DIR}/config.sh"

# scope reads the local clone and never calls GitHub, so it runs before the configuration loads: a
# clone without a token can still size a branch before it has a PR.
if [[ "${1:-}" == scope ]]; then
  shift
  exec bash "${SCRIPT_DIR}/scope/pr_scope.sh" "$@"
fi

load_project_config "$PROJECT_ROOT"

# The token comes from the private file only. An exported GITHUB_TOKEN belongs to other tools, such
# as Copilot CLI or a CI job, and must not stand in for a file that lacks the key.
unset GITHUB_TOKEN
load_private_config "$PROJECT_ROOT" github
: "${GITHUB_TOKEN:?GITHUB_TOKEN is not set in PROJECT_CFG.Local.md — see .agents/skills/gh-pr-review/references/setup.md}"
: "${GITHUB_REPOSITORY:?GITHUB_REPOSITORY is not set in PROJECT_CFG.md}"

ACTION="${1:?Usage: bash scripts/run.sh <list|create|review|issue|scope|fetch|comment|reply|edit|delete|vote|describe|title|retarget|stack|publish|complete|resolve|unresolve> [args...]}"
shift

# create and stack are absent deliberately: each parses its own arguments and already rejects an
# unexpected one, so a second body there is an error rather than a silent drop.
case "$ACTION" in
  list | review | issue | publish | complete | delete | resolve | unresolve) require_max_positionals "$ACTION" 1 $# ;;
  fetch | reply | edit | describe | title | retarget)                require_max_positionals "$ACTION" 2 $# ;;
  vote)                                                              require_max_positionals "$ACTION" 3 $# ;;
  comment)                                                           require_max_positionals "$ACTION" 5 $# ;;
esac

GH=("$GITHUB_TOKEN" "$GITHUB_REPOSITORY")

case "$ACTION" in
  list)
    bash "${SCRIPT_DIR}/list_prs.sh" "${GH[@]}" "${1:-open}"
    ;;
  create)
    CREATE_SOURCE_BRANCH="${1:?Missing SOURCE_BRANCH}"
    CREATE_TARGET_BRANCH="${2:?Missing TARGET_BRANCH}"
    CREATE_TITLE="${3:?Missing TITLE}"
    shift 3
    bash "${SCRIPT_DIR}/create_pull_request.sh" "${GH[@]}" \
      "$CREATE_SOURCE_BRANCH" "$CREATE_TARGET_BRANCH" "$CREATE_TITLE" "$@"
    ;;
  review)
    bash "${SCRIPT_DIR}/review_pr.sh" "${GH[@]}" "${1:?Missing PR}"
    ;;
  issue)
    bash "${SCRIPT_DIR}/show_issue.sh" "${GH[@]}" "${1:?Missing ISSUE}"
    ;;
  fetch)
    # Refs must be double-quoted to protect special characters such as #.
    # fetch_file.sh strips refs/heads/ and URL-encodes the ref internally.
    bash "${SCRIPT_DIR}/fetch_file.sh" "${GH[@]}" \
      "${1:?Missing FILE_PATH}" "${2:?Missing REF — usage: bash scripts/run.sh fetch <file_path> \"<ref>\"}"
    ;;
  comment)
    bash "${SCRIPT_DIR}/post_comment.sh" "${GH[@]}" \
      "${1:?Missing PR}" "${2:?Missing COMMENT}" "${3:-}" "${4:-}" "${5:-}"
    ;;
  reply)
    bash "${SCRIPT_DIR}/reply_comment.sh" "${GH[@]}" "${1:?Missing ID (PRRT_… or IC_…)}" "${2:?Missing COMMENT}"
    ;;
  edit)
    bash "${SCRIPT_DIR}/edit_comment.sh" "${GH[@]}" "${1:?Missing COMMENT_ID}" "${2:?Missing COMMENT}"
    ;;
  delete)
    bash "${SCRIPT_DIR}/delete_comment.sh" "${GH[@]}" "${1:?Missing COMMENT_ID}"
    ;;
  vote)
    bash "${SCRIPT_DIR}/vote_pr.sh" "${GH[@]}" "${1:?Missing PR}" "${2:?Missing EVENT (approve|request-changes|comment)}" "${3:-}"
    ;;
  describe)
    bash "${SCRIPT_DIR}/update_description.sh" "${GH[@]}" "${1:?Missing PR}" "${2:?Missing DESCRIPTION}"
    ;;
  title)
    bash "${SCRIPT_DIR}/update_title.sh" "${GH[@]}" "${1:?Missing PR}" "${2:?Missing TITLE}"
    ;;
  retarget)
    bash "${SCRIPT_DIR}/retarget_pr.sh" "${GH[@]}" "${1:?Missing PR}" "${2:?Missing TARGET_BRANCH}"
    ;;
  stack)
    bash "${SCRIPT_DIR}/stack_prs.sh" "${GH[@]}" "${1:?Missing subcommand: list | show | create | add | unstack}" "${@:2}"
    ;;
  publish)
    bash "${SCRIPT_DIR}/publish_pr.sh" "${GH[@]}" "${1:?Missing PR}"
    ;;
  complete)
    bash "${SCRIPT_DIR}/complete_pr.sh" "${GH[@]}" "${1:?Missing PR}"
    ;;
  resolve | unresolve)
    bash "${SCRIPT_DIR}/resolve_thread.sh" "${GH[@]}" "${1:?Missing ID (PRRT_… or IC_…)}" "$ACTION"
    ;;
  *)
    echo "ERROR: Unknown action '${ACTION}'. Valid: list | create | review | issue | scope | fetch | comment | reply | edit | delete | vote | describe | title | retarget | stack | publish | complete | resolve | unresolve" >&2
    exit 1
    ;;
esac
