#!/usr/bin/env bash
# Cases for retarget_pr.sh and stack_prs.sh.

test_retarget_changes_the_base_branch() {
  # GIVEN a pull request and a target given as a refs/heads/ ref
  curl() {
    local body
    case "$*" in
      *'-X PATCH'*'/repos/acme/widgets/pulls/42')
        body=$(cat)
        jq -e '. == {base: "feature/parent"}' <<<"$body" >/dev/null || return 91
        printf '%s' '{"number":42,"state":"open","head":{"ref":"feature/child"},"base":{"ref":"feature/parent"}}'
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # WHEN it is retargeted
  local output
  output=$(bash "${SCRIPT_DIR}/retarget_pr.sh" acme/widgets 42 refs/heads/feature/parent 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the PATCH carries the short branch name only, and the report names both branches
  jq -e '. == {id: 42, source: "feature/child", target: "feature/parent", status: "open"}' <<<"$output" >/dev/null
}

test_retarget_reports_the_refusal() {
  # GIVEN a target branch that GitHub rejects
  curl() {
    case "$*" in
      *'-X PATCH'*) cat >/dev/null; printf '%s' '{"message":"Validation Failed","errors":[{"message":"Proposed base branch '"'"'nope'"'"' was not found"}]}'; return 22 ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # WHEN the retarget runs
  local output status
  output=$(bash "${SCRIPT_DIR}/retarget_pr.sh" acme/widgets 42 nope 2>&1)
  status=$?

  # THEN the run fails with GitHub's reason
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'pull request 42 was not retargeted.' &&
    assert_contains "$output" "Proposed base branch 'nope' was not found"
}

# stack_json <number> <pr...> — a stack response whose pull requests are the given numbers, bottom first
stack_json() {
  local number="$1"; shift
  jq -n --argjson number "$number" --args '{
    id: 9, number: $number, node_id: "PRS_1", url: "u", base: {ref: "develop"}, open: true,
    pull_requests: [$ARGS.positional[] | tonumber | {number: ., state: "open", draft: false, merged_at: null, head: {ref: "h\(.)", sha: "s\(.)"}}]
  }' "$@"
}

test_stack_show_prints_the_stack_holding_a_pull_request() {
  # GIVEN a pull request in a stack of three
  curl() {
    case "$*" in
      *'/repos/acme/widgets/stacks?pull_request=42&per_page=100')
        [[ "$*" == *'X-GitHub-Api-Version: 2026-03-10'* ]] || return 92
        printf '[%s]' "$(stack_json 7 41 42 43 | jq -c '.pull_requests[0].merged_at = "2026-01-01T00:00:00Z" | .pull_requests[0].state = "closed"')"
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl stack_json

  # WHEN its stack is shown
  local output
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets show 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the stack is printed bottom first with each pull request's state, head branch and commit
  jq -e '. == {stack: 7, base: "develop", open: true, pullRequests: [
    {id: 41, status: "merged", isDraft: false, head: "h41", headCommit: "s41"},
    {id: 42, status: "open", isDraft: false, head: "h42", headCommit: "s42"},
    {id: 43, status: "open", isDraft: false, head: "h43", headCommit: "s43"}]}' <<<"$output" >/dev/null
}

test_stack_show_names_a_pull_request_outside_any_stack() {
  # GIVEN a pull request that no stack holds
  curl() {
    case "$*" in
      *'/stacks?pull_request=42&per_page=100') printf '[]' ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # WHEN its stack is shown
  local output status
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets show 42 2>&1)
  status=$?

  # THEN the run fails and says so
  [[ $status -ne 0 ]] && assert_contains "$output" 'pull request 42 is not in a stack.'
}

test_stack_create_sends_the_pull_requests_bottom_first() {
  # GIVEN three pull requests each targeting the one below
  curl() {
    local body
    case "$*" in
      *'-X POST'*'/repos/acme/widgets/stacks')
        [[ "$*" == *'X-GitHub-Api-Version: 2026-03-10'* ]] || return 92
        body=$(cat)
        jq -e '. == {pull_requests: [41, 42, 43]}' <<<"$body" >/dev/null || return 91
        stack_json 8 41 42 43
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl stack_json

  # WHEN a stack is created from them
  local output
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets create 41 42 43 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the numbers go as integers in the given order and the new stack is printed
  jq -e '.stack == 8 and ([.pullRequests[].id] == [41, 42, 43])' <<<"$output" >/dev/null || return 1

  # AND a single pull request cannot form a stack
  local refusal status
  refusal=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets create 41 2>&1)
  status=$?
  [[ $status -ne 0 ]] && assert_contains "$refusal" 'create takes at least two pull request numbers'
}

test_stack_list_prints_every_stack() {
  # GIVEN two stacks, the first with a merged bottom
  curl() {
    case "$*" in
      *'/repos/acme/widgets/stacks?&per_page=100&page=1')
        [[ "$*" == *'X-GitHub-Api-Version: 2026-03-10'* ]] || return 92
        printf '[%s,%s]' \
          "$(stack_json 7 41 42 | jq -c '.pull_requests[0].merged_at = "2026-01-01T00:00:00Z" | .pull_requests[0].state = "closed"')" \
          "$(stack_json 8 51 52)"
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl stack_json

  # WHEN the stacks are listed
  local output
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets list 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN both stacks are printed in the shape show uses, with the merged pull request named as merged
  jq -e 'length == 2 and .[0].stack == 7 and ([.[0].pullRequests[].status] == ["merged", "open"])
    and ([.[1].pullRequests[].head] == ["h51", "h52"]) and ([.[1].pullRequests[].headCommit] == ["s51", "s52"])
    and all(.[].pullRequests[]; has("isDraft"))' <<<"$output" >/dev/null
}

test_stack_show_fails_on_a_non_array_response() {
  # GIVEN an API that answers show with a redirect page instead of an array
  curl() {
    case "$*" in
      *'/stacks?pull_request=42&per_page=100') printf '{"message":"Moved Permanently"}' ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # WHEN the stack is shown
  local output status
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets show 42 2>&1)
  status=$?

  # THEN the run fails instead of printing a jq error and exiting 0
  [[ $status -ne 0 ]] && assert_contains "$output" 'the stack of pull request 42 was not read.'
}

test_stack_rejects_bad_arguments_before_http() {
  # GIVEN an API that must not be called
  curl() { return 90; }
  export -f curl

  # WHEN a call carries a bad number or a stray argument
  local bad_pr bad_stack stray
  bad_pr=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets create 41 x 2>&1) && return 1
  bad_stack=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets unstack x 2>&1) && return 1
  stray=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets show 41 42 2>&1) && return 1

  # THEN each fails with its own message
  assert_contains "$bad_pr" "'x' is not a pull request number." &&
    assert_contains "$bad_stack" "STACK must be a positive integer, got 'x'." &&
    assert_contains "$stray" 'show takes one pull request number.'
}

test_stack_add_appends_above_the_top_and_reports_the_refusal() {
  # GIVEN a stack and a pull request that targets its top, and one that does not
  curl() {
    local body
    case "$*" in
      *'-X POST'*'/repos/acme/widgets/stacks/8/add')
        body=$(cat)
        if jq -e '. == {pull_requests: [44]}' <<<"$body" >/dev/null; then
          stack_json 8 41 42 43 44
        else
          printf '%s' '{"message":"Validation Failed","errors":[{"message":"Pull request 45 does not target the top of the stack"}]}'
          return 22
        fi
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl stack_json

  # WHEN each is added
  local output refusal status
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets add 8 44 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }
  refusal=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets add 8 45 2>&1)
  status=$?

  # THEN the first joins the top and the second fails with GitHub's reason
  jq -e '[.pullRequests[].id] == [41, 42, 43, 44]' <<<"$output" >/dev/null &&
    [[ $status -ne 0 ]] &&
    assert_contains "$refusal" 'were not added to stack 8.' &&
    assert_contains "$refusal" 'Pull request 45 does not target the top of the stack'
}

test_stack_unstack_reports_dissolution_or_the_remaining_stack() {
  # GIVEN one stack that dissolves fully and one whose merged bottom stays
  curl() {
    case "$*" in
      *'-X POST'*'/repos/acme/widgets/stacks/8/unstack') ;;
      *'-X POST'*'/repos/acme/widgets/stacks/9/unstack')
        stack_json 9 41 | jq -c '.open = false | .pull_requests[0].merged_at = "2026-01-01T00:00:00Z" | .pull_requests[0].state = "closed"' ;;
      *) return 90 ;;
    esac
  }
  export -f curl stack_json

  # WHEN each is unstacked
  local dissolved kept
  dissolved=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets unstack 8 2>/dev/null) || return 1
  kept=$(bash "${SCRIPT_DIR}/stack_prs.sh" acme/widgets unstack 9 2>&1) || return 1

  # THEN the 204 is reported as dissolved and the 200 names what stayed and why
  jq -e '. == {stack: 8, dissolved: true}' <<<"$dissolved" >/dev/null &&
    assert_contains "$kept" 'stack 9 still holds #41 (merged); merged and queued pull requests are not unstacked.' &&
    assert_contains "$kept" '"status": "merged"'
}

run_test 'retarget changes the base branch' test_retarget_changes_the_base_branch
run_test 'retarget reports the refusal' test_retarget_reports_the_refusal
run_test 'stack show prints the stack holding a pull request' test_stack_show_prints_the_stack_holding_a_pull_request
run_test 'stack show names a pull request outside any stack' test_stack_show_names_a_pull_request_outside_any_stack
run_test 'stack create sends the pull requests bottom first' test_stack_create_sends_the_pull_requests_bottom_first
run_test 'stack list prints every stack' test_stack_list_prints_every_stack
run_test 'stack show fails on a non-array response' test_stack_show_fails_on_a_non_array_response
run_test 'stack rejects bad arguments before HTTP' test_stack_rejects_bad_arguments_before_http
run_test 'stack add appends above the top and reports the refusal' test_stack_add_appends_above_the_top_and_reports_the_refusal
run_test 'stack unstack reports dissolution or the remaining stack' test_stack_unstack_reports_dissolution_or_the_remaining_stack
