#!/usr/bin/env bash
# config.sh — configuration loading and the argument guard shared by every skill's run.sh.
#
# This file is *sourced* by a skill's run.sh, never executed directly. It inherits the caller's
# shell options and defines only functions.

# source_config <path>
# Config values are assigned literally instead of being sourced. A work item tree path carries
# spaces and backslashes (DA-G3\PI 26.4\2026.4 Sprint 1); sourcing that eats the backslashes and
# runs the rest of the line as a command, and the corruption is silent. Surrounding quotes are
# stripped, so a value quoted for the old parser still reads the same.
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
    # Both ends: a leading space survived into a tree path, failed the project-prefix test in
    # create_work_item.sh and was sent as {project}\ {project}\{area}. ADO then rejected an unknown
    # classification node and failed the whole patch document, so nothing was filed at all.
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

# load_shared_config <skill_root>
# Load the committed PROJECT_CFG.md of a skill, or exit with the setup pointer.
load_shared_config() {
  local config_path="$1/PROJECT_CFG.md"
  if [[ -f "$config_path" ]]; then
    source_config "$config_path"
  else
    echo "ERROR: PROJECT_CFG.md not found at ${config_path}" >&2
    echo "       Create PROJECT_CFG.md — see references/setup.md" >&2
    exit 1
  fi
}

# load_private_config <skill_root> <git_common_subdir>
# Load the private token file: the skill-local PROJECT_CFG.Local.md first, then
# <git-common-dir>/<git_common_subdir>/PROJECT_CFG.Local.md. The Git common directory is shared by
# every linked worktree and cannot be staged from a worktree.
load_private_config() {
  local skill_root="$1" subdir="$2"
  local local_config="${skill_root}/PROJECT_CFG.Local.md"
  local git_root="$skill_root" git_common_dir shared_config

  if [[ -f "$local_config" ]]; then
    source_config "$local_config"
    return
  fi

  # MSYS2_ARG_CONV_EXCL protects API paths but also stops Git Bash translating /c/... for the
  # native Git executable. Convert only this filesystem argument when cygpath is available.
  if command -v cygpath >/dev/null 2>&1; then
    git_root=$(cygpath -m "$skill_root")
  fi
  git_common_dir=$(git -C "$git_root" rev-parse --path-format=absolute --git-common-dir 2>/dev/null || true)
  shared_config="${git_common_dir:+${git_common_dir}/${subdir}/PROJECT_CFG.Local.md}"
  if [[ -n "$shared_config" && -f "$shared_config" ]]; then
    source_config "$shared_config"
    return
  fi

  echo "ERROR: PROJECT_CFG.Local.md was not found at ${local_config}." >&2
  if [[ -n "$shared_config" ]]; then
    echo "       Shared config was not found at ${shared_config}." >&2
  else
    echo "       The shared Git directory could not be resolved." >&2
  fi
  echo "       Create a private config — see references/setup.md." >&2
  exit 1
}

# require_max_positionals <action> <max> <argc>
# Reject positionals past the last slot an action reads. Each action binds fixed slots and ignores
# whatever follows, so a body that reaches bash already split into words — which is what an
# unquoted native command line produces, Windows PowerShell 5.1 being the case that prompted this —
# posted its first word and exited 0. Nothing in the response marked the text as truncated.
require_max_positionals() {
  local action="$1" max="$2" argc="$3"
  if (( argc > max )); then
    echo "ERROR: ${action} takes at most ${max} argument(s), but received ${argc}." >&2
    echo "       A multiline body must arrive as ONE argument. If it split into words, the calling" >&2
    echo "       shell did not quote it — see .agents/skills/_shared/references/text.md for the form your shell needs." >&2
    exit 1
  fi
}
