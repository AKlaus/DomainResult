#!/usr/bin/env bash
# Cases for list_prs.sh and create_pull_request.sh.

# pull_requests_list_page <first> <last>
# Print a page of open pull requests numbered <first> to <last>.
pull_requests_list_page() {
  jq -n --argjson first "$1" --argjson last "$2" '[range($first; $last + 1) | {
    number: ., title: "T\(.)", user: {login: "alex"}, head: {ref: "h\(.)"}, base: {ref: "develop"},
    state: "open", merged_at: null, draft: false, created_at: "2026-08-01T00:00:00Z",
    requested_reviewers: [], requested_teams: []
  }]'
}

test_list_prs_paginates_until_a_short_page() {
  # GIVEN a first page of 100 pull requests and a second page of two
  LIST_PAGE_ONE=$(pull_requests_list_page 1 100)
  LIST_PAGE_TWO=$(pull_requests_list_page 101 102)
  export LIST_PAGE_ONE LIST_PAGE_TWO
  curl() {
    case "$*" in
      *'/repos/acme/widgets/pulls?state=all&per_page=100&page=1') printf '%s' "$LIST_PAGE_ONE" ;;
      *'/repos/acme/widgets/pulls?state=all&per_page=100&page=2') printf '%s' "$LIST_PAGE_TWO" ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # WHEN every pull request is listed
  local output
  output=$(bash "${SCRIPT_DIR}/list_prs.sh" fake-token acme/widgets all 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN both pages are joined in order, and no third page is requested
  jq -e 'length == 102 and .[0].id == 1 and .[101].id == 102 and .[101].source == "h102"' \
    <<<"$output" >/dev/null
}

test_list_prs_maps_and_filters_status() {
  curl() {
    case "$*" in
      *'/pulls?state=closed&per_page=100&page=1' | *'/pulls?state=all&per_page=100&page=1')
        printf '%s' '[
          {"number":1,"state":"closed","merged_at":"2026-08-02T00:00:00Z","user":{"login":"alex"},"head":{"ref":"one"},"base":{"ref":"develop"},"draft":false},
          {"number":2,"state":"closed","merged_at":null,"user":{"login":"blair"},"head":{"ref":"two"},"base":{"ref":"develop"},"draft":false},
          {"number":3,"state":"open","merged_at":null,"user":{"login":"casey"},"head":{"ref":"three"},"base":{"ref":"develop"},"draft":true,
           "requested_reviewers":[{"login":"dana"}],"requested_teams":[{"slug":"core"}]}
        ]'
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN one merged, one closed, and one open pull request
  # WHEN all of them are listed, then merged only, then closed only
  local all merged closed
  all=$(bash "${SCRIPT_DIR}/list_prs.sh" fake-token acme/widgets all 2>&1) &&
    merged=$(bash "${SCRIPT_DIR}/list_prs.sh" fake-token acme/widgets merged 2>&1) &&
    closed=$(bash "${SCRIPT_DIR}/list_prs.sh" fake-token acme/widgets closed 2>&1) || {
      printf '%s\n%s\n%s\n' "${all:-}" "${merged:-}" "${closed:-}" >&2
      return 1
    }

  # THEN merged_at decides merged, state decides closed and open
  jq -e 'map(.status) == ["merged", "closed", "open"]
    and .[2].reviewers == ["dana", "core"] and .[2].isDraft == true and .[0].author == "alex"' \
    <<<"$all" >/dev/null &&
    # AND a status filter keeps only its own pull requests
    jq -e 'map(.id) == [1]' <<<"$merged" >/dev/null &&
    jq -e 'map(.id) == [2]' <<<"$closed" >/dev/null
}

test_list_prs_rejects_unknown_status_before_http() {
  curl() { return 90; }
  export -f curl

  # GIVEN a status name that list does not accept
  # WHEN it is passed to list
  local output
  output=$(bash "${SCRIPT_DIR}/list_prs.sh" fake-token acme/widgets abandoned 2>&1) && return 1

  # THEN the valid statuses are named, rather than a curl failure
  assert_contains "$output" "invalid status 'abandoned'" &&
    assert_contains "$output" 'open, merged, closed, all'
}

test_list_prs_emits_no_partial_output_when_a_page_fails() {
  LIST_PAGE_ONE=$(pull_requests_list_page 1 100)
  export LIST_PAGE_ONE
  curl() {
    case "$*" in
      *'state=open&per_page=100&page=1') printf '%s' "$LIST_PAGE_ONE" ;;
      *'state=open&per_page=100&page=2')
        printf '%s' '{"message":"Second page failed"}'
        return 22
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  local output_file error_file status
  output_file=$(mktemp)
  error_file=$(mktemp)
  trap 'rm -f "$output_file" "$error_file"' RETURN

  # GIVEN a full first page and a second page that fails
  # WHEN open pull requests are listed
  bash "${SCRIPT_DIR}/list_prs.sh" fake-token acme/widgets >"$output_file" 2>"$error_file"
  status=$?

  # THEN the run fails, prints no list, and names the cause
  [[ $status -ne 0 ]] &&
    [[ ! -s "$output_file" ]] &&
    assert_contains "$(<"$error_file")" 'Second page failed'
}

test_create_preserves_unicode_and_ready_state() {
  curl() {
    local request_body
    [[ "$*" == *'-X POST'*'/repos/acme/widgets/pulls' ]] || return 90
    request_body=$(cat)
    jq -e \
      --arg description '--leading — curly ‘quote’' \
      '.head == "feature/example"
        and .base == "develop"
        and .title == "Test PR"
        and .body == $description
        and .draft == false' \
      <<<"$request_body" >/dev/null || return 91
    printf '%s' '{"number":42,"title":"Test PR","head":{"ref":"feature/example"},"base":{"ref":"develop"},"state":"open","draft":false,"html_url":"https://github.com/acme/widgets/pull/42"}'
  }
  export -f curl

  # GIVEN full refs, --ready, and a dash-prefixed Unicode description after --
  local output
  output=$(bash "${SCRIPT_DIR}/create_pull_request.sh" fake-token acme/widgets \
    refs/heads/feature/example refs/heads/develop "Test PR" --ready -- '--leading — curly ‘quote’' 2>&1) || {
      printf '%s\n' "$output" >&2
      return 1
    }

  # THEN the request carries short branch names, the exact text, and draft false
  jq -e '.id == 42 and .isDraft == false and .source == "feature/example"
    and .url == "https://github.com/acme/widgets/pull/42"' <<<"$output" >/dev/null
}

test_create_reports_api_error() {
  curl() {
    cat >/dev/null
    printf '%s' '{"message":"Validation Failed","errors":[{"resource":"PullRequest","code":"custom","message":"A pull request already exists for acme:feature/example."}]}'
    return 22
  }
  export -f curl

  # GIVEN GitHub answers 422 with a detail in .errors
  local output status
  output=$(bash "${SCRIPT_DIR}/create_pull_request.sh" \
    fake-token acme/widgets feature/example develop "Test PR" 2>&1)
  status=$?

  # THEN the run fails and shows both the message and the detail
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'ERROR: pull request was not created.' &&
    assert_contains "$output" 'Validation Failed' &&
    assert_contains "$output" 'A pull request already exists for acme:feature/example.'
}

run_test 'list PRs paginates until a short page' test_list_prs_paginates_until_a_short_page
run_test 'list PRs maps merged, closed, and open, and filters by status' test_list_prs_maps_and_filters_status
run_test 'list PRs rejects an unknown status before HTTP' test_list_prs_rejects_unknown_status_before_http
run_test 'list PRs emits no partial output when a page fails' test_list_prs_emits_no_partial_output_when_a_page_fails
run_test 'create preserves Unicode, dash-prefixed descriptions, and ready state' test_create_preserves_unicode_and_ready_state
run_test 'create reports API errors with their detail' test_create_reports_api_error
