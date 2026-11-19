#!/usr/bin/env bash
# Cases for _shared/scripts/config.sh: where the private token file is read from.
#
# Each case sources config.sh in its own subshell and reads the variable the private file sets, so
# it holds for every skill that loads its token through load_private_config.

# Builds a Git repository in $1 holding one skill folder, $1/skill, with a committed PROJECT_CFG.md.
config_prepare_repo() {
  local repo_root="$1" git_root="$1"
  mkdir -p "${repo_root}/skill"
  printf 'ORG=my-org\n' >"${repo_root}/skill/PROJECT_CFG.md"
  if command -v cygpath >/dev/null 2>&1; then
    git_root=$(cygpath -m "$repo_root")
  fi
  command git -C "$git_root" init -q
  command git -C "$git_root" config core.autocrlf false
  command git -C "$git_root" add skill/PROJECT_CFG.md
  command git -C "$git_root" -c user.name='Test User' -c user.email='test@example.invalid' commit -qm fixture
}

test_private_config_uses_shared_git_config_fallback() {
  local temp_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"

  # GIVEN only the shared file under the Git common directory
  mkdir -p "${temp_root}/.git/azure-devops"
  printf 'ADO_PAT=fake-shared-pat\n' >"${temp_root}/.git/azure-devops/PROJECT_CFG.Local.md"

  # WHEN the skill loads its private config
  source "${SHARED_SCRIPTS_DIR}/config.sh"
  load_private_config "${temp_root}/skill" azure-devops

  # THEN the shared token is read
  [[ "$ADO_PAT" == fake-shared-pat ]]
}

test_private_config_prefers_skill_local_file() {
  local temp_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"

  # GIVEN both the shared file and a skill-local file
  mkdir -p "${temp_root}/.git/azure-devops"
  printf 'ADO_PAT=fake-shared-pat\n' >"${temp_root}/.git/azure-devops/PROJECT_CFG.Local.md"
  printf 'ADO_PAT=fake-local-pat\n' >"${temp_root}/skill/PROJECT_CFG.Local.md"

  # WHEN the skill loads its private config
  source "${SHARED_SCRIPTS_DIR}/config.sh"
  load_private_config "${temp_root}/skill" azure-devops

  # THEN the skill-local token wins
  [[ "$ADO_PAT" == fake-local-pat ]]
}

test_private_config_reads_shared_file_from_linked_worktree() {
  local temp_root linked_root git_main_root git_linked_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "${temp_root}/main"

  # GIVEN a linked worktree and the shared file in the main repository's Git directory
  linked_root="${temp_root}/linked"
  git_main_root="${temp_root}/main"
  git_linked_root="$linked_root"
  if command -v cygpath >/dev/null 2>&1; then
    git_main_root=$(cygpath -m "$git_main_root")
    git_linked_root=$(cygpath -m "$linked_root")
  fi
  command git -C "$git_main_root" worktree add -qb linked "$git_linked_root"
  mkdir -p "${temp_root}/main/.git/azure-devops"
  printf 'ADO_PAT=fake-shared-pat\n' >"${temp_root}/main/.git/azure-devops/PROJECT_CFG.Local.md"

  # WHEN the skill in the linked worktree loads its private config
  source "${SHARED_SCRIPTS_DIR}/config.sh"
  load_private_config "${linked_root}/skill" azure-devops

  # THEN the shared token is read
  [[ "$ADO_PAT" == fake-shared-pat ]]
}

test_private_config_names_both_paths_when_missing() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"

  # GIVEN neither private file
  source "${SHARED_SCRIPTS_DIR}/config.sh"

  # WHEN the skill loads its private config
  output=$(load_private_config "${temp_root}/skill" azure-devops 2>&1) && return 1

  # THEN the error names the skill-local path and the shared path
  assert_contains "$output" "${temp_root}/skill/PROJECT_CFG.Local.md" &&
    assert_contains "$output" 'azure-devops/PROJECT_CFG.Local.md'
}

run_test 'private config uses the shared Git config fallback' test_private_config_uses_shared_git_config_fallback
run_test 'private config prefers the skill-local file' test_private_config_prefers_skill_local_file
run_test 'private config reads the shared file from a linked worktree' test_private_config_reads_shared_file_from_linked_worktree
run_test 'private config names both paths when missing' test_private_config_names_both_paths_when_missing
