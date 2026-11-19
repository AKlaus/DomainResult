#!/usr/bin/env bash
# Cases for the shared scripts in _shared/scripts, run by every skill suite.
#
# Sourced by harness.sh, never run directly: it supplies SCRIPT_DIR (the calling skill's scripts),
# SHARED_SCRIPTS_DIR, BODY_SCRIPT_FLOOR, the assert helpers and run_test. Every case stubs curl
# itself, so this file depends on nothing another case file defines.

test_fail_with_body_prints_message_and_errors() {
  source "${SHARED_SCRIPTS_DIR}/common.sh"

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
  source "${SHARED_SCRIPTS_DIR}/common.sh"

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
  source "${SHARED_SCRIPTS_DIR}/common.sh"

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
  source "${SHARED_SCRIPTS_DIR}/common.sh"

  # GIVEN a title as PowerShell delivers it: the apostrophes of $'...' stripped on the way to bash,
  # leaving a bare $ and, on a single-line title, no backslash-n to fall back on
  local output
  output=$(warn_unexpanded_quoting "the title" '$DevOps: fix the lease' 2>&1)

  # THEN the leading dollar sign alone is enough to report it
  assert_contains "$output" 'starts with a literal $' &&
    assert_contains "$output" '.agents/skills/_shared/references/text.md'
}

test_pr_reference_match_flags_unlinked_forms() {
  set -euo pipefail
  source "${SHARED_SCRIPTS_DIR}/common.sh"

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
  source "${SHARED_SCRIPTS_DIR}/common.sh"

  # GIVEN the GitHub form, a printed link, a legacy Azure DevOps reference, a hash that starts with
  # digits, and a small ordinal
  local text
  for text in 'Stacked on #41, for AB#964805.' \
    'Fixed in [aa-bida-acl\#42](https://github.com/anglo-american-group/aa-bida-acl/pull/42).' \
    'Stacked on !198265, for #964805.' 'On merged PR 6794b75e75 the rows are gone.' \
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
  # hold prose rather than a field name: a *Field arg names a work item field, it never holds
  # the body. Deriving the list means a worker added later is covered without editing this test.
  local body_args='--arg (body|content|description|desc|criteria|notes|repro|details|text) '
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
