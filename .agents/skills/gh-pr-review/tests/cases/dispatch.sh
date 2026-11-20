#!/usr/bin/env bash
# Cases for the run.sh dispatcher: what it forwards to each worker, and what it refuses.

# pull_requests_run_fixture
# Print the root of a skill fixture with run.sh, the shared config, and a private token. Every
# worker the dispatch cases call is a stub that prints its arguments joined by |.
pull_requests_run_fixture() {
  local temp_root worker
  temp_root=$(make_skill_fixture)
  cp "${SCRIPT_DIR}/run.sh" "${temp_root}/scripts/run.sh"
  printf 'GITHUB_REPOSITORY=acme/widgets\n' >"${temp_root}/PROJECT_CFG.md"
  printf 'GITHUB_TOKEN=fake-token\n' >"${temp_root}/PROJECT_CFG.Local.md"
  for worker in fetch_file vote_pr resolve_thread list_prs complete_pr retarget_pr stack_prs; do
    printf '%s\n' '#!/usr/bin/env bash' 'IFS="|"; printf "%s\n" "$*"' >"${temp_root}/scripts/${worker}.sh"
  done
  printf '%s' "$temp_root"
}

test_run_dispatches_fetch_vote_resolve_complete_retarget_and_stack() {
  local temp_root
  temp_root=$(pull_requests_run_fixture)
  trap "rm -rf '${temp_root%/skill}'" EXIT

  # GIVEN run.sh with stub workers
  # THEN fetch forwards the path and the ref, # included
  [[ "$(bash "${temp_root}/scripts/run.sh" fetch /src/a.cs 'bugfix/#9')" \
    == 'fake-token|acme/widgets|/src/a.cs|bugfix/#9' ]] &&
    # AND vote forwards three slots, the body empty when omitted
    [[ "$(bash "${temp_root}/scripts/run.sh" vote 42 approve)" \
      == 'fake-token|acme/widgets|42|approve|' ]] &&
    [[ "$(bash "${temp_root}/scripts/run.sh" vote 42 comment 'A question.')" \
      == 'fake-token|acme/widgets|42|comment|A question.' ]] &&
    # AND resolve and unresolve forward their own mode
    [[ "$(bash "${temp_root}/scripts/run.sh" resolve PRRT_1)" \
      == 'fake-token|acme/widgets|PRRT_1|resolve' ]] &&
    [[ "$(bash "${temp_root}/scripts/run.sh" unresolve PRRT_1)" \
      == 'fake-token|acme/widgets|PRRT_1|unresolve' ]] &&
    # AND complete forwards the pull request number only
    [[ "$(bash "${temp_root}/scripts/run.sh" complete 42)" \
      == 'fake-token|acme/widgets|42' ]] &&
    ! bash "${temp_root}/scripts/run.sh" complete 42 extra 2>/dev/null &&
    # AND retarget forwards the pull request number and the target branch
    [[ "$(bash "${temp_root}/scripts/run.sh" retarget 42 feature/parent)" \
      == 'fake-token|acme/widgets|42|feature/parent' ]] &&
    ! bash "${temp_root}/scripts/run.sh" retarget 42 2>/dev/null &&
    # AND stack forwards the subcommand and every pull request number
    [[ "$(bash "${temp_root}/scripts/run.sh" stack create 41 42 43 44 45 46 47 48 49 50 51 52 53)" \
      == 'fake-token|acme/widgets|create|41|42|43|44|45|46|47|48|49|50|51|52|53' ]] &&
    ! bash "${temp_root}/scripts/run.sh" stack 2>/dev/null
}

test_run_names_a_token_that_no_file_holds() {
  local temp_root output
  temp_root=$(pull_requests_run_fixture)
  trap "rm -rf '${temp_root%/skill}'" EXIT

  # GIVEN a private config without GITHUB_TOKEN, and an exported GITHUB_TOKEN
  printf 'OTHER_KEY=value\n' >"${temp_root}/PROJECT_CFG.Local.md"

  # WHEN a GitHub action runs
  output=$(env -u GH_TOKEN GITHUB_TOKEN=exported-token bash "${temp_root}/scripts/run.sh" list 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the exported token reaches the worker, and run.sh names where it came from
  assert_contains "$output" 'exported-token|acme/widgets|open' &&
    assert_contains "$output" 'Note: the GitHub token comes from the GITHUB_TOKEN environment variable.' &&
    # AND a token from the file wins, with no note
    printf 'GITHUB_TOKEN=file-token\n' >"${temp_root}/PROJECT_CFG.Local.md" &&
    output=$(env -u GH_TOKEN GITHUB_TOKEN=exported-token bash "${temp_root}/scripts/run.sh" list 2>&1) &&
    assert_contains "$output" 'file-token|acme/widgets|open' &&
    assert_not_contains "$output" 'Note:'
}

test_run_rejects_extra_positionals() {
  # A body that reaches bash split into words lands in the slots after the one the action reads.
  # The dispatcher refuses such a call, and still accepts every legitimate arity.
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
    assert_contains "$output" '.agents/skills/gh-pr-review/references/writing.md, "Multiline content"' &&
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

run_test 'run.sh dispatches fetch, vote, resolve, complete, retarget, and stack' test_run_dispatches_fetch_vote_resolve_complete_retarget_and_stack
run_test 'run.sh names a token that no file holds' test_run_names_a_token_that_no_file_holds
run_test 'run.sh rejects a body split into extra positionals' test_run_rejects_extra_positionals
