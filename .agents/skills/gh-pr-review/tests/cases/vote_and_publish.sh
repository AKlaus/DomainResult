#!/usr/bin/env bash
# Cases for vote_pr.sh and publish_pr.sh.

test_vote_maps_to_review_events() {
  curl() {
    local body
    [[ "$*" == *'-X POST'*'/repos/acme/widgets/pulls/42/reviews' ]] || return 90
    body=$(cat)
    jq -e '. == {event: "APPROVE"}
      or . == {event: "REQUEST_CHANGES", body: "Fix the test."}
      or . == {event: "COMMENT", body: "A question."}' <<<"$body" >/dev/null || return 91
    jq -c '{node_id: "PRR_1", user: {login: "blair"}, html_url: "u",
      state: ({APPROVE: "APPROVED", REQUEST_CHANGES: "CHANGES_REQUESTED", COMMENT: "COMMENTED"}[.event])}' <<<"$body"
  }
  export -f curl

  # GIVEN the three votes, the last two with a body
  local approve changes comment
  approve=$(bash "${SCRIPT_DIR}/vote_pr.sh" acme/widgets 42 approve 2>&1) &&
    changes=$(bash "${SCRIPT_DIR}/vote_pr.sh" acme/widgets 42 request-changes 'Fix the test.' 2>&1) &&
    comment=$(bash "${SCRIPT_DIR}/vote_pr.sh" acme/widgets 42 comment 'A question.' 2>&1) || {
      printf '%s\n%s\n%s\n' "${approve:-}" "${changes:-}" "${comment:-}" >&2
      return 1
    }

  # THEN each submits its review event, an approval with no body, and reports the new state
  jq -e '. == {id: "PRR_1", reviewer: "blair", state: "APPROVED", url: "u"}' <<<"$approve" >/dev/null &&
    jq -e '.state == "CHANGES_REQUESTED"' <<<"$changes" >/dev/null &&
    jq -e '.state == "COMMENTED"' <<<"$comment" >/dev/null
}

test_vote_rejects_bad_arguments_before_http() {
  CURL_MARKER=$(mktemp -u)
  export CURL_MARKER
  curl() { : >"$CURL_MARKER"; return 90; }
  export -f curl

  # GIVEN request-changes with no body, and a vote that does not exist
  local no_body invalid
  no_body=$(bash "${SCRIPT_DIR}/vote_pr.sh" acme/widgets 42 request-changes 2>&1) && return 1
  invalid=$(bash "${SCRIPT_DIR}/vote_pr.sh" acme/widgets 42 10 2>&1) && return 1

  # THEN both are refused with a reason, and curl never runs
  assert_contains "$no_body" 'request-changes needs a body' &&
    assert_contains "$invalid" "invalid vote '10'" &&
    [[ ! -e "$CURL_MARKER" ]]
}

test_vote_reports_own_pr_refusal() {
  curl() {
    cat >/dev/null
    printf '%s' '{"message":"Unprocessable Entity","errors":["Review Can not approve your own pull request"]}'
    return 22
  }
  export -f curl

  # GIVEN the author approves their own pull request
  local output status
  output=$(bash "${SCRIPT_DIR}/vote_pr.sh" acme/widgets 42 approve 2>&1)
  status=$?

  # THEN the run fails with GitHub's reason
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'review was not submitted on PR 42.' &&
    assert_contains "$output" 'Can not approve your own pull request'
}

test_publish_marks_a_draft_ready() {
  curl() {
    local body
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42')
        printf '%s' '{"number":42,"node_id":"PR_kw42","title":"T","draft":true,"state":"open","html_url":"u"}'
        ;;
      *'-X POST'*'https://api.github.com/graphql')
        body=$(cat)
        jq -e '(.query | contains("markPullRequestReadyForReview")) and .variables == {id: "PR_kw42"}' \
          <<<"$body" >/dev/null || return 91
        printf '%s' '{"data":{"markPullRequestReadyForReview":{"pullRequest":{"isDraft":false}}}}'
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN a draft pull request
  local output
  output=$(bash "${SCRIPT_DIR}/publish_pr.sh" acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN it is marked ready by its node id, and reported out of draft
  jq -e '. == {id: 42, title: "T", isDraft: false, status: "open", url: "u"}' <<<"$output" >/dev/null
}

test_publish_skips_graphql_for_a_ready_pr() {
  CURL_MARKER=$(mktemp -u)
  export CURL_MARKER
  curl() {
    case "$*" in
      *'/repos/acme/widgets/pulls/42')
        printf '%s' '{"number":42,"node_id":"PR_kw42","title":"T","draft":false,"state":"open","html_url":"u"}'
        ;;
      *) : >"$CURL_MARKER"; return 90 ;;
    esac
  }
  export -f curl

  # GIVEN a pull request already out of draft
  local output
  output=$(bash "${SCRIPT_DIR}/publish_pr.sh" acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN no mutation runs, and it still reports isDraft false
  [[ ! -e "$CURL_MARKER" ]] &&
    jq -e '.isDraft == false and .id == 42' <<<"$output" >/dev/null
}

run_test 'vote maps to review events' test_vote_maps_to_review_events
run_test 'vote rejects bad arguments before HTTP' test_vote_rejects_bad_arguments_before_http
run_test 'vote reports the own-PR refusal' test_vote_reports_own_pr_refusal
run_test 'publish marks a draft ready' test_publish_marks_a_draft_ready
run_test 'publish skips GraphQL for a ready PR' test_publish_skips_graphql_for_a_ready_pr
