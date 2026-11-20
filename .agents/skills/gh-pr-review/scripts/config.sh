#!/usr/bin/env bash
# config.sh — configuration loading and the argument guard of run.sh.
#
# This file is *sourced* by run.sh, never executed directly. It inherits the caller's
# shell options and defines only functions.

# source_config <path>
# Config values are assigned literally instead of being sourced. Sourcing eats backslashes and
# runs the rest of the line as a command, and the corruption is silent. Surrounding quotes are
# stripped, so a quoted value reads the same.
source_config() {
  local config_path="$1" line key value

  while IFS= read -r line || [[ -n "$line" ]]; do
    # No separate CR strip: a CRLF file leaves the CR at the end of the line, where the key and
    # value trims below remove it as whitespace.
    line="${line#"${line%%[![:space:]]*}"}"
    if [[ -z "$line" || "$line" == '#'* || "$line" != *=* ]]; then
      continue
    fi

    key="${line%%=*}"
    key="${key%"${key##*[![:space:]]}"}"
    value="${line#*=}"
    # Both ends: a stray space would reach the GitHub API inside the repository name or the token.
    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"

    # printf -v writes to whatever name it is given, so anything that is not an identifier is
    # reported rather than assigned.
    if [[ ! "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
      echo "WARNING: ignoring '${key}' in ${config_path}: not a configuration key." >&2
      continue
    fi
    if (( ${#value} >= 2 )) && { [[ "$value" == \"*\" ]] || [[ "$value" == \'*\' ]]; }; then
      value="${value:1:${#value}-2}"
    fi

    printf -v "$key" '%s' "$value"
  done <"$config_path"
}

# load_project_config <skill_root>
# Load the committed PROJECT_CFG.md of a skill, or exit with the setup pointer.
load_project_config() {
  local config_path="$1/PROJECT_CFG.md"
  if [[ -f "$config_path" ]]; then
    source_config "$config_path"
  else
    echo "ERROR: PROJECT_CFG.md not found at ${config_path}" >&2
    echo "       Create PROJECT_CFG.md — see .agents/skills/gh-pr-review/references/setup.md" >&2
    exit 1
  fi
}

# load_github_token <skill_root>
# Set GITHUB_TOKEN from the first of these places that holds a token, and GITHUB_TOKEN_SOURCE to
# the place:
#   1. PROJECT_CFG.Local.md in the skill folder
#   2. github/PROJECT_CFG.Local.md in the Git common directory, which every linked worktree shares
#   3. the GH_TOKEN, then the GITHUB_TOKEN environment variable, the GitHub CLI's own order
#   4. the GitHub CLI, when it is signed in
# A file that exists without the key falls through to the next place. Exit with every place it
# checked when none holds a token. The Git credential helper is not a place: it can start an
# interactive sign-in when it holds nothing, and it holds what Git uses for clones and pushes, not a
# token set for GitHub tools.
load_github_token() {
  local skill_root="$1" from_gh_token="${GH_TOKEN:-}" from_github_token="${GITHUB_TOKEN:-}"
  local git_root="$skill_root" git_common_dir config cli_token
  local configs=("${skill_root}/PROJECT_CFG.Local.md")

  # MSYS2_ARG_CONV_EXCL protects API paths but also stops Git Bash translating /c/... for the
  # native Git executable. Convert only this filesystem argument when cygpath is available.
  if command -v cygpath >/dev/null 2>&1; then
    git_root=$(cygpath -m "$skill_root")
  fi
  git_common_dir=$(git -C "$git_root" rev-parse --path-format=absolute --git-common-dir 2>/dev/null || true)
  if [[ -n "$git_common_dir" ]]; then
    configs+=("${git_common_dir}/github/PROJECT_CFG.Local.md")
  fi

  # A file wins over the environment, so the exported value is set aside before the files load.
  unset GITHUB_TOKEN
  GITHUB_TOKEN_SOURCE=''
  for config in "${configs[@]}"; do
    [[ -f "$config" ]] || continue
    source_config "$config"
    if [[ -n "${GITHUB_TOKEN:-}" ]]; then
      GITHUB_TOKEN_SOURCE="$config"
      return
    fi
  done

  if [[ -n "$from_gh_token" ]]; then
    GITHUB_TOKEN="$from_gh_token"
    GITHUB_TOKEN_SOURCE='the GH_TOKEN environment variable'
  elif [[ -n "$from_github_token" ]]; then
    GITHUB_TOKEN="$from_github_token"
    GITHUB_TOKEN_SOURCE='the GITHUB_TOKEN environment variable'
  elif cli_token=$(github_cli_token) && [[ -n "$cli_token" ]]; then
    GITHUB_TOKEN="$cli_token"
    GITHUB_TOKEN_SOURCE='the GitHub CLI (gh auth token)'
  else
    echo "ERROR: no GitHub token was found. These places were checked, in order:" >&2
    printf '         %s\n' "${configs[@]}" 'the GH_TOKEN and GITHUB_TOKEN environment variables' \
      'the GitHub CLI (gh auth token)' >&2
    echo "       See .agents/skills/gh-pr-review/references/setup.md." >&2
    exit 1
  fi
}

# github_cli_token
# Print the token of the GitHub CLI for github.com. Fails when gh is missing or signed out.
github_cli_token() {
  command -v gh >/dev/null 2>&1 && gh auth token --hostname github.com 2>/dev/null
}

# require_max_positionals <action> <max> <argc>
# Reject positionals past the last slot an action reads. Each action binds fixed slots and ignores
# the rest, so a body that an unquoting shell split into words would post its first word only.
require_max_positionals() {
  local action="$1" max="$2" argc="$3"
  if (( argc > max )); then
    echo "ERROR: ${action} takes at most ${max} argument(s), but received ${argc}." >&2
    echo "       A multiline body must arrive as ONE argument. If it split into words, the calling" >&2
    echo "       shell did not quote it — see .agents/skills/gh-pr-review/references/writing.md, \"Multiline content\"." >&2
    exit 1
  fi
}
