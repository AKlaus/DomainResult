#!/usr/bin/env bash
# Library and dispatcher cases for the GitHub pull request scripts.
#
# Sourced by harness.sh, never run directly: it supplies SCRIPT_DIR, the assert helpers and
# run_test, and each case runs where it is sourced. Every case stubs curl itself, so this file
# depends on nothing another case file defines.

test_all_scripts_parse() {
  bash -n "${SCRIPT_DIR}"/*.sh
}

test_gh_init_and_path_helpers() {
  set -euo pipefail
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN an owner/repo pair
  gh_init fake-token AKlaus/DomainResult

  # THEN the REST root is repository-scoped, and refs and paths are normalised for GitHub
  [[ "$REPO_URL" == "https://api.github.com/repos/AKlaus/DomainResult" ]] &&
    [[ "$GH_OWNER" == AKlaus && "$GH_REPO" == DomainResult ]] &&
    [[ "$(gh_ref_to_branch refs/heads/bugfix/#912729)" == 'bugfix/#912729' ]] &&
    [[ "$(gh_ref_to_branch feature/x)" == 'feature/x' ]] &&
    [[ "$(gh_repo_path /src/Common/a.cs)" == 'src/Common/a.cs' ]] &&
    # AND an Azure DevOps style repository name is refused
    ! (gh_init fake-token DomainResult) 2>/dev/null
}

test_body_guards_cover_bodies_but_not_titles() {
  curl() {
    cat >/dev/null
    printf '%s' '{"number":42,"title":"PR 41 follow-up","body":"D"}'
  }
  export -f curl

  # GIVEN plain-text references sent to a PR description and a PR title
  local body_output title_output
  body_output=$(bash "${SCRIPT_DIR}/update_description.sh" \
    fake-token o/r 42 'Follows PR 41.' 2>&1)
  title_output=$(bash "${SCRIPT_DIR}/update_title.sh" \
    fake-token o/r 42 'PR 41 follow-up' 2>&1)

  # THEN the body is warned about the reference
  assert_contains "$body_output" 'the description names a pull request as plain text' &&
    assert_contains "$body_output" 'Write #<n>' &&
    # AND the title is updated without a warning, because a title carries no reference to link
    assert_contains "$title_output" '"id": 42' &&
    [[ "$title_output" != *'names a pull request'* ]]
}

test_run_rejects_extra_positionals() {
  # A body that reaches bash already split into words lands in the slots after the one the action
  # reads. Those slots were ignored, so the call posted the first word and exited 0. The dispatcher
  # must refuse the call instead, and must still accept every legitimate arity.
  local temp_root output
  temp_root=$(make_skill_fixture)
  trap "rm -rf '${temp_root%/skill}'" EXIT

  cp "${SCRIPT_DIR}/run.sh" "${temp_root}/scripts/run.sh"
  printf 'GITHUB_REPOSITORY=o/r\n' >"${temp_root}/PROJECT_CFG.md"
  # The token sits only in the Git common directory, so this case also covers the folder run.sh names.
  write_git_dir_private_config "$temp_root" github 'GITHUB_TOKEN=fake-token'
  printf '%s\n' '#!/usr/bin/env bash' 'IFS="|"; printf "%s\n" "$*"' \
    | tee "${temp_root}/scripts/update_description.sh" "${temp_root}/scripts/vote_pr.sh" \
      >"${temp_root}/scripts/post_comment.sh"

  # GIVEN a description that arrived as four words rather than one argument
  output=$(bash "${temp_root}/scripts/run.sh" describe 42 He said lease here. 2>&1) && {
    printf 'Expected a non-zero exit, got: %s\n' "$output" >&2
    return 1
  }

  # THEN the dispatcher names the arity and points at the quoting rules
  assert_contains "$output" 'takes at most 2' &&
    assert_contains "$output" '.agents/skills/_shared/references/text.md' &&
    # AND the correctly quoted call still reaches the worker intact
    [[ "$(bash "${temp_root}/scripts/run.sh" describe 42 'He said lease here.')" \
      == 'fake-token|o/r|42|He said lease here.' ]] &&
    # AND an action's optional trailing slots stay usable: comment takes five
    [[ "$(bash "${temp_root}/scripts/run.sh" comment 42 'Body.' abc123 src/a.cs 12)" \
      == 'fake-token|o/r|42|Body.|abc123|src/a.cs|12' ]] &&
    ! bash "${temp_root}/scripts/run.sh" comment 42 'Body.' abc123 src/a.cs 12 spurious >/dev/null 2>&1 &&
    # AND vote binds its event and body and refuses a fourth slot
    [[ "$(bash "${temp_root}/scripts/run.sh" vote 42 request-changes 'Body.')" \
      == 'fake-token|o/r|42|request-changes|Body.' ]] &&
    ! bash "${temp_root}/scripts/run.sh" vote 42 comment 'Body.' spurious >/dev/null 2>&1
}

run_test 'all scripts parse' test_all_scripts_parse
run_test 'gh_init and the path helpers normalise for GitHub' test_gh_init_and_path_helpers
run_test 'body guards cover bodies but not titles' test_body_guards_cover_bodies_but_not_titles
run_test 'run.sh rejects a body split into extra positionals' test_run_rejects_extra_positionals
