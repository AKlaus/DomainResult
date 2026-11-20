#!/usr/bin/env bash
# Cases for lib.sh: the API helpers and the body-text guards.

test_all_scripts_parse() {
  bash -n "${SCRIPT_DIR}"/*.sh "${SCRIPT_DIR}"/scope/*.sh
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
    # AND a repository name without its owner is refused
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

test_gh_git_offers_the_token_to_github_only() {
  # GIVEN gh_git with a token, and no other Git configuration or prompt
  source "${SCRIPT_DIR}/lib.sh"
  gh_init fake-token acme/widgets
  export GIT_CONFIG_GLOBAL=/dev/null GIT_CONFIG_NOSYSTEM=1 GIT_TERMINAL_PROMPT=0 GIT_ASKPASS='' SSH_ASKPASS=''

  # WHEN Git asks it for credentials for github.com over HTTPS, a look-alike host, and plain HTTP
  local github lookalike http
  github=$(printf 'protocol=https\nhost=github.com\n\n' | gh_git credential fill 2>&1)
  lookalike=$(printf 'protocol=https\nhost=notgithub.com\n\n' | gh_git credential fill 2>&1)
  http=$(printf 'protocol=http\nhost=github.com\n\n' | gh_git credential fill 2>&1)

  # THEN only github.com over HTTPS receives the token
  assert_contains "$github" 'password=fake-token' &&
    assert_not_contains "$lookalike" 'fake-token' &&
    assert_not_contains "$http" 'fake-token'
}

run_test 'all scripts parse' test_all_scripts_parse
run_test 'gh_init and the path helpers normalise for GitHub' test_gh_init_and_path_helpers
run_test 'body guards cover bodies but not titles' test_body_guards_cover_bodies_but_not_titles

test_fail_with_body_prints_message_and_errors() {
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN a GitHub 422 body and a GraphQL error body
  local rest graphql
  rest=$( (fail_with_body '{"message":"Validation Failed","errors":[{"resource":"PullRequestReviewComment","code":"custom","message":"pull_request_review_thread.line must be part of the diff"},"plain entry"]}' 'review comment was not posted.') 2>&1) && return 1
  graphql=$( (fail_with_body '{"data":null,"errors":[{"message":"Could not resolve to a node with the global id of '"'"'123'"'"'"}]}' 'reply was not posted.') 2>&1) && return 1

  # THEN each names the summary and every cause, and a body without both keys is printed whole,
  # not as the empty list of causes
  assert_contains "$rest" 'ERROR: review comment was not posted.' &&
    assert_contains "$rest" 'Validation Failed' &&
    assert_contains "$rest" 'line must be part of the diff' &&
    assert_contains "$rest" 'plain entry' &&
    assert_contains "$graphql" 'Could not resolve to a node' &&
    assert_contains "$( (fail_with_body '<html>Sign in</html>' 'GET request failed.') 2>&1)" '<html>Sign in</html>' &&
    assert_contains "$( (fail_with_body '{"status":"failed","details":{"message":"Required check is failing."}}' 'merge failed.') 2>&1)" '"Required check is failing."'
}

test_quoting_guard_flags_unexpanded_ansi_c() {
  set -euo pipefail
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN a body whose ANSI-C quoting reached a shell that does not expand it
  local output
  output=$(warn_unexpanded_quoting "the comment" "\$'[Minor] First line.\\n\\n_-- AI Assistant_" 2>&1)

  # THEN both fingerprints are named, and the guard stays silent on stdout
  assert_contains "$output" "starts with a literal \$'" &&
    assert_contains "$output" 'contains a literal backslash-n' &&
    [[ -z "$(warn_unexpanded_quoting "the comment" "\$'x\\n" 2>/dev/null)" ]]
}

test_quoting_guard_passes_clean_and_returns_zero() {
  set -euo pipefail
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN a body with real newlines and the punctuation a review comment carries
  local output
  output=$(warn_unexpanded_quoting "the comment" "$(printf 'The author'\''s lease — a test.\n\n_-- AI Assistant_')" 2>&1)

  # THEN nothing is reported
  [[ -z "$output" ]] || {
    printf 'Expected no warning, got:\n%s\n' "$output" >&2
    return 1
  }

  # AND the guard reports rather than repairs, so it never fails its caller
  warn_unexpanded_quoting "the comment" "\$'still returns zero\\n" 2>/dev/null
}

test_quoting_guard_flags_bare_leading_dollar() {
  set -euo pipefail
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN a title as PowerShell delivers it: the apostrophes of $'...' stripped on the way to bash,
  # leaving a bare $ and, on a single-line title, no backslash-n to fall back on
  local output
  output=$(warn_unexpanded_quoting "the title" '$DevOps: fix the lease' 2>&1)

  # THEN the leading dollar sign alone is enough to report it
  assert_contains "$output" 'starts with a literal $' &&
    assert_contains "$output" '.agents/skills/gh-pr-review/references/writing.md'
}

test_pr_reference_match_flags_unlinked_forms() {
  set -euo pipefail
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN each form that names a pull request without a link
  local text
  for text in 'Fixed by PR 42.' 'PR #7 holds the fix.' 'See pull request 1234' \
    'Builds on [PR 42](https://github.com/o/r/pull/42).'; do
    # THEN the shared match reports it
    names_unlinked_pr "$text" || {
      printf 'No match for: %s\n' "$text" >&2
      return 1
    }
  done
}

test_pr_reference_match_passes_linked_and_unrelated_text() {
  set -euo pipefail
  source "${SCRIPT_DIR}/lib.sh"

  # GIVEN the GitHub form, a printed link, a hash that starts with digits, and a small ordinal
  local text
  for text in 'Stacked on #41, for #93.' \
    'Fixed in [DomainResult\#42](https://github.com/AKlaus/DomainResult/pull/42).' \
    'On merged PR 6794b75e75 the rows are gone.' \
    'This is PR 2 of the stack.'; do
    # THEN the shared match stays silent
    ! names_unlinked_pr "$text" || {
      printf 'Unexpected match for: %s\n' "$text" >&2
      return 1
    }
  done
}

test_every_prose_script_calls_its_guard() {
  set -euo pipefail

  # GIVEN the workers that carry free text into the API, identified by the jq --arg names that
  # hold prose. Deriving the list means a worker added later is covered without editing this test.
  local body_args='--arg (body|content) '
  local script name missing=()
  for script in "${SCRIPT_DIR}"/*.sh; do
    name=$(basename "$script")
    [[ "$name" == lib.sh ]] && continue

    # THEN a worker that posts a body runs it past every body guard
    if grep -qE -- "$body_args" "$script" && ! grep -q 'warn_body_text ' "$script"; then
      missing+=("${name}: no warn_body_text for its body")
    fi

    # AND a worker that posts a title runs it past the quoting guard
    if grep -q -- '--arg title ' "$script" && ! grep -q 'warn_unexpanded_quoting "the title"' "$script"; then
      missing+=("${name}: no warn_unexpanded_quoting for its title")
    fi

    # AND no body is sent to the quoting guard alone, which would skip the other body guards
    if grep 'warn_unexpanded_quoting "' "$script" | grep -qv '"the title"'; then
      missing+=("${name}: warn_unexpanded_quoting on text that is not a title")
    fi
  done

  [[ ${#missing[@]} -eq 0 ]] || {
    printf '%s\n' "${missing[@]}" >&2
    return 1
  }

  # AND the derivation itself must still select something, or the check passes vacuously
  [[ $(grep -lE -- "$body_args" "${SCRIPT_DIR}"/*.sh | grep -cv 'lib\.sh$') -ge "${BODY_SCRIPT_FLOOR:?}" ]]
}

run_test 'fail_with_body prints the message and every error entry' test_fail_with_body_prints_message_and_errors
run_test 'quoting guard flags unexpanded ANSI-C quoting' test_quoting_guard_flags_unexpanded_ansi_c
run_test 'quoting guard passes clean text and never fails' test_quoting_guard_passes_clean_and_returns_zero
run_test 'quoting guard flags a bare leading dollar sign' test_quoting_guard_flags_bare_leading_dollar
run_test 'PR reference match flags each unlinked form' test_pr_reference_match_flags_unlinked_forms
run_test 'PR reference match passes linked and unrelated text' test_pr_reference_match_passes_linked_and_unrelated_text
run_test 'every prose-carrying script calls its guard' test_every_prose_script_calls_its_guard
run_test 'gh_git offers the token to github.com over HTTPS only' test_gh_git_offers_the_token_to_github_only
