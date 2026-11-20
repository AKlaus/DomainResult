#!/usr/bin/env bash
# Cases for scripts/config.sh: where load_github_token finds the token, and in which order.

# Builds a Git repository in $1 holding one skill folder, $1/skill, with a committed PROJECT_CFG.md.
config_prepare_repo() {
  local repo_root="$1" git_root="$1"
  mkdir -p "${repo_root}/skill"
  printf 'GITHUB_REPOSITORY=o/r\n' >"${repo_root}/skill/PROJECT_CFG.md"
  if command -v cygpath >/dev/null 2>&1; then
    git_root=$(cygpath -m "$repo_root")
  fi
  command git -C "$git_root" init -q
  command git -C "$git_root" config core.autocrlf false
  command git -C "$git_root" add skill/PROJECT_CFG.md
  command git -C "$git_root" -c user.name='Test User' -c user.email='test@example.invalid' commit -qm fixture
}

# Sources config.sh with the caller's token variables cleared, and a GitHub CLI that prints $1, or
# fails when $1 is empty. No case reads the real environment or the real CLI of the machine.
config_load() {
  CONFIG_CLI_TOKEN="${1:-}"
  unset GH_TOKEN GITHUB_TOKEN
  source "${SCRIPT_DIR}/config.sh"
  github_cli_token() { [[ -n "$CONFIG_CLI_TOKEN" ]] && printf '%s' "$CONFIG_CLI_TOKEN"; }
}

test_token_prefers_the_skill_local_file() {
  local temp_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"
  config_load cli-token

  # GIVEN a token in both files, in the environment, and in the GitHub CLI
  mkdir -p "${temp_root}/.git/github"
  printf 'GITHUB_TOKEN=shared-token\n' >"${temp_root}/.git/github/PROJECT_CFG.Local.md"
  printf 'GITHUB_TOKEN=local-token\n' >"${temp_root}/skill/PROJECT_CFG.Local.md"
  export GH_TOKEN=gh-env-token GITHUB_TOKEN=github-env-token

  # WHEN the skill loads its token
  load_github_token "${temp_root}/skill"

  # THEN the skill-local file wins
  [[ "$GITHUB_TOKEN" == local-token && "$GITHUB_TOKEN_SOURCE" == "${temp_root}/skill/PROJECT_CFG.Local.md" ]]
}

test_token_falls_through_a_file_without_the_key() {
  local temp_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"
  config_load

  # GIVEN a skill-local file without GITHUB_TOKEN, and the shared file under the Git common directory
  printf 'OTHER_KEY=value\n' >"${temp_root}/skill/PROJECT_CFG.Local.md"
  mkdir -p "${temp_root}/.git/github"
  printf 'GITHUB_TOKEN=shared-token\n' >"${temp_root}/.git/github/PROJECT_CFG.Local.md"
  export GITHUB_TOKEN=github-env-token

  # WHEN the skill loads its token
  load_github_token "${temp_root}/skill"

  # THEN the shared file supplies it, ahead of the environment
  [[ "$GITHUB_TOKEN" == shared-token && "$GITHUB_TOKEN_SOURCE" == */.git/github/PROJECT_CFG.Local.md ]]
}

test_token_reads_the_shared_file_from_a_linked_worktree() {
  local temp_root linked_root git_main_root git_linked_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "${temp_root}/main"
  config_load

  # GIVEN a linked worktree and the shared file in the main repository's Git directory
  linked_root="${temp_root}/linked"
  git_main_root="${temp_root}/main"
  git_linked_root="$linked_root"
  if command -v cygpath >/dev/null 2>&1; then
    git_main_root=$(cygpath -m "$git_main_root")
    git_linked_root=$(cygpath -m "$linked_root")
  fi
  command git -C "$git_main_root" worktree add -qb linked "$git_linked_root"
  mkdir -p "${temp_root}/main/.git/github"
  printf 'GITHUB_TOKEN=shared-token\n' >"${temp_root}/main/.git/github/PROJECT_CFG.Local.md"

  # WHEN the skill in the linked worktree loads its token
  load_github_token "${linked_root}/skill"

  # THEN the shared token is read
  [[ "$GITHUB_TOKEN" == shared-token ]]
}

# Arguments: the GH_TOKEN and GITHUB_TOKEN values to export, then the token and the source to expect.
test_token_falls_back_to_the_environment() {
  local temp_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"
  config_load cli-token

  # GIVEN no token file, and the given environment variables
  [[ -z "$1" ]] || export GH_TOKEN="$1"
  [[ -z "$2" ]] || export GITHUB_TOKEN="$2"

  # WHEN the skill loads its token
  load_github_token "${temp_root}/skill"

  # THEN the expected variable supplies it, ahead of the GitHub CLI
  [[ "$GITHUB_TOKEN" == "$3" && "$GITHUB_TOKEN_SOURCE" == "the $4 environment variable" ]]
}

test_token_falls_back_to_the_github_cli() {
  local temp_root
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"

  # GIVEN no token file, no token variable, and a signed-in GitHub CLI
  config_load cli-token

  # WHEN the skill loads its token
  load_github_token "${temp_root}/skill"

  # THEN the GitHub CLI supplies it
  [[ "$GITHUB_TOKEN" == cli-token && "$GITHUB_TOKEN_SOURCE" == 'the GitHub CLI (gh auth token)' ]]
}

test_token_names_every_place_when_missing() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  config_prepare_repo "$temp_root"

  # GIVEN no token anywhere
  config_load

  # WHEN the skill loads its token
  output=$(load_github_token "${temp_root}/skill" 2>&1) && return 1

  # THEN the error names each place it checked, in order
  assert_contains "$output" "${temp_root}/skill/PROJECT_CFG.Local.md" &&
    assert_contains "$output" '.git/github/PROJECT_CFG.Local.md' &&
    assert_contains "$output" 'the GH_TOKEN and GITHUB_TOKEN environment variables' &&
    assert_contains "$output" 'the GitHub CLI (gh auth token)'
}

run_test 'token: the skill-local file wins' test_token_prefers_the_skill_local_file
run_test 'token: a file without the key falls through to the next place' test_token_falls_through_a_file_without_the_key
run_test 'token: the shared file is read from a linked worktree' test_token_reads_the_shared_file_from_a_linked_worktree
run_test 'token: GH_TOKEN wins over GITHUB_TOKEN' test_token_falls_back_to_the_environment gh-env-token github-env-token gh-env-token GH_TOKEN
run_test 'token: GITHUB_TOKEN is used when GH_TOKEN is not set' test_token_falls_back_to_the_environment '' github-env-token github-env-token GITHUB_TOKEN
run_test 'token: the GitHub CLI is the last place' test_token_falls_back_to_the_github_cli
run_test 'token: the error names every place checked' test_token_names_every_place_when_missing
