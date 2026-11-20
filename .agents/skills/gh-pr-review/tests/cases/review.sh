#!/usr/bin/env bash
# Cases for what a review reads: review_pr.sh, fetch_file.sh, and show_issue.sh.

# pull_requests_section <output> <name>
# Print the JSON of one "=== NAME ===" section of review output.
pull_requests_section() {
  printf '%s\n' "$1" | awk -v header="=== $2 ===" '
    $0 == header { found = 1; next }
    /^=== / { found = 0 }
    found'
}

# A 100-file first page, so review must ask for a second page of files.
REVIEW_FILES_PAGE_ONE=$(jq -n '[range(1; 101) | {filename: "src/f\(.).cs", status: "modified"}]')
export REVIEW_FILES_PAGE_ONE

# review_stub_rest <url-args>
# Answer the REST calls of review_pr.sh for PR 7, open unless REVIEW_STATE says closed. base.sha
# (base111) is the target when the PR last synced; the target branch has moved on to tip777 since;
# and the merge base (merge000) differs from both. With REVIEW_MOVE set to head or target and
# REVIEW_READS naming a scratch file, every read after the first sees a push (head333) or a new
# target commit (tip888), as when the PR changes while review reads it.
review_stub_rest() {
  local head=head222 tip=tip777
  case "$*" in
    *'/repos/acme/widgets/pulls/7')
      if [[ -n "${REVIEW_READS:-}" ]]; then
        printf 'pull\n' >>"$REVIEW_READS"
        [[ "${REVIEW_MOVE:-}" == head && $(grep -c '^pull$' "$REVIEW_READS") -gt 1 ]] && head=head333
      fi
      printf '{"number":7,"title":"Seven","body":"Desc","user":{"login":"alex"},
        "head":{"ref":"feature/seven","sha":"%s"},"base":{"ref":"develop","sha":"base111"},
        "state":"%s","merged_at":null,"draft":false,"created_at":"2026-08-01T00:00:00Z",
        "requested_reviewers":[{"login":"dave"}],"requested_teams":[]}' "$head" "${REVIEW_STATE:-open}"
      ;;
    *'/repos/acme/widgets/compare/develop...head222?per_page=1')
      if [[ -n "${REVIEW_READS:-}" ]]; then
        printf 'compare\n' >>"$REVIEW_READS"
        [[ "${REVIEW_MOVE:-}" == target && $(grep -c '^compare$' "$REVIEW_READS") -gt 1 ]] && tip=tip888
      fi
      printf '{"merge_base_commit":{"sha":"merge000"},"base_commit":{"sha":"%s"}}' "$tip" ;;
    *'/repos/acme/widgets/compare/base111...head222?per_page=1')
      printf '%s' '{"merge_base_commit":{"sha":"merge111"},"base_commit":{"sha":"base111"}}' ;;
    *'/pulls/7/files?&per_page=100&page=1') printf '%s' "$REVIEW_FILES_PAGE_ONE" ;;
    *'/pulls/7/files?&per_page=100&page=2')
      printf '%s' '[{"filename":"src/new.cs","status":"renamed","previous_filename":"src/old.cs"}]'
      ;;
    *) return 90 ;;
  esac
}

export -f review_stub_rest

test_review_reports_snapshot_files_threads_and_reviews() {
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || { review_stub_rest "$@"; return; }
    local body after
    body=$(cat)
    after=$(jq -r '.variables.after // "first"' <<<"$body")
    case "$body:$after" in
      *reviewThreads*:first)
        printf '%s' '{"data":{"repository":{"pullRequest":{"reviewThreads":{
          "pageInfo":{"hasNextPage":true,"endCursor":"T1"},
          "nodes":[
            {"id":"PRRT_a","isResolved":false,"isOutdated":false,"path":"src/f1.cs","line":4,"originalLine":4,"subjectType":"LINE",
             "comments":{"nodes":[{"id":"PRRC_a1","body":"Why?","createdAt":"t1","author":{"login":"blair","__typename":"User"},"originalCommit":{"oid":"head222"}}]}},
            {"id":"PRRT_bot","isResolved":false,"isOutdated":false,"path":"src/f2.cs","line":1,"originalLine":1,"subjectType":"LINE",
             "comments":{"nodes":[{"id":"PRRC_bot","body":"Scan","createdAt":"t1","author":{"login":"scanner","__typename":"Bot"},"originalCommit":{"oid":"head222"}}]}}
          ]}}}}}'
        ;;
      *reviewThreads*:T1)
        printf '%s' '{"data":{"repository":{"pullRequest":{"reviewThreads":{
          "pageInfo":{"hasNextPage":false,"endCursor":"T2"},
          "nodes":[
            {"id":"PRRT_b","isResolved":true,"isOutdated":true,"path":"src/new.cs","line":null,"originalLine":5,"subjectType":"FILE",
             "comments":{"nodes":[
               {"id":"PRRC_b0","body":"Noise","createdAt":"t1","author":{"login":"scanner","__typename":"Bot"},"originalCommit":{"oid":"old999"}},
               {"id":"PRRC_b1","body":"Done","createdAt":"t2","author":{"login":"casey","__typename":"User"},"originalCommit":{"oid":"old999"}}]}}
          ]}}}}}'
        ;;
      *'reviews(first'*:first)
        printf '%s' '{"data":{"repository":{"pullRequest":{"reviews":{
          "pageInfo":{"hasNextPage":false,"endCursor":"R1"},
          "nodes":[
            {"id":"PRR_1","body":"","state":"APPROVED","submittedAt":"2026-08-01T00:00:00Z","author":{"login":"blair","__typename":"User"}},
            {"id":"PRR_2","body":"Fix it","state":"CHANGES_REQUESTED","submittedAt":"2026-08-02T00:00:00Z","author":{"login":"blair","__typename":"User"}},
            {"id":"PRR_3","body":"Looks fine","state":"COMMENTED","submittedAt":"2026-08-02T00:00:00Z","author":{"login":"erin","__typename":"User"}},
            {"id":"PRR_4","body":"","state":"APPROVED","submittedAt":"2026-08-02T00:00:00Z","author":{"login":"casey","__typename":"User"}},
            {"id":"PRR_5","body":"Bot review","state":"COMMENTED","submittedAt":"2026-08-02T00:00:00Z","author":{"login":"scanner","__typename":"Bot"}}
          ]}}}}}'
        ;;
      *'comments(first'*:first)
        printf '%s' '{"data":{"repository":{"pullRequest":{"comments":{
          "pageInfo":{"hasNextPage":true,"endCursor":"C1"},
          "nodes":[
            {"id":"IC_1","body":"General","createdAt":"t1","isMinimized":false,"minimizedReason":null,"author":{"login":"blair","__typename":"User"}},
            {"id":"IC_bot","body":"Coverage","createdAt":"t1","author":{"login":"scanner","__typename":"Bot"}}
          ]}}}}}'
        ;;
      *'comments(first'*:C1)
        printf '%s' '{"data":{"repository":{"pullRequest":{"comments":{
          "pageInfo":{"hasNextPage":false,"endCursor":"C2"},
          "nodes":[{"id":"IC_2","body":"Later","createdAt":"t2","isMinimized":true,"minimizedReason":"resolved","author":{"login":"casey","__typename":"User"}},
                   {"id":"IC_3","body":"Old","createdAt":"t3","isMinimized":true,"minimizedReason":"outdated","author":{"login":"casey","__typename":"User"}}]}}}}}'
        ;;
      *) return 91 ;;
    esac
  }
  export -f curl

  # GIVEN PR 7 with a moved base branch, 101 files, and threads and comments over two pages each
  local output
  output=$(bash "${SCRIPT_DIR}/review_pr.sh" fake-token acme/widgets 7 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the snapshot compares with the target branch as it is now: the merge base, the head, and
  # the branch tip, not the stale base.sha
  pull_requests_section "$output" SNAPSHOT | jq -e '. == {baseCommit: "merge000",
    sourceCommit: "head222", targetCommit: "tip777"}' >/dev/null &&
    # AND every file page is read, and a rename keeps its original path
    pull_requests_section "$output" 'CHANGED FILES' | jq -e 'length == 101
      and .[100] == {changeType: "renamed", path: "src/new.cs", originalPath: "src/old.cs"}
      and .[0].originalPath == null' >/dev/null &&
    # AND threads from both pages remain, bot comments go, and a bot-only thread goes too
    pull_requests_section "$output" 'REVIEW THREADS' | jq -e 'map(.id) == ["PRRT_a", "PRRT_b"]
      and .[1].comments == [{id: "PRRC_b1", author: "casey", content: "Done", date: "t2", commit: "old999"}]
      and .[1].isResolved == true and .[1].isOutdated == true and .[1].line == 5 and .[1].subject == "file"
      and .[0].subject == "line"' >/dev/null &&
    # AND general comments from both pages remain, without the bot
    pull_requests_section "$output" 'GENERAL COMMENTS' | jq -e 'map(.id) == ["IC_1", "IC_2", "IC_3"]
      and map(.isResolved) == [false, true, false] and map(.hidden) == [null, "resolved", "outdated"]' >/dev/null &&
    # AND reviewers hold each user's latest deciding review, then the requested reviewers
    pull_requests_section "$output" 'PR METADATA' | jq -e '.id == 7 and .status == "open"
      and .reviewers == [
        {name: "blair", state: "CHANGES_REQUESTED"},
        {name: "casey", state: "APPROVED"},
        {name: "dave", state: "REQUESTED"}]' >/dev/null &&
    # AND reviews list only human reviews with a body
    pull_requests_section "$output" REVIEWS | jq -e 'map(.id) == ["PRR_2", "PRR_3"]' >/dev/null
}

test_review_compares_a_closed_pr_with_its_recorded_base() {
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || { review_stub_rest "$@"; return; }
    local body connection
    body=$(cat)
    connection=$(grep -oE 'reviewThreads|reviews|comments' <<<"$body" | head -n1)
    jq -n --arg c "$connection" \
      '{data: {repository: {pullRequest: {($c): {pageInfo: {hasNextPage: false, endCursor: null}, nodes: []}}}}}'
  }
  export -f curl

  # GIVEN PR 7 closed, so its target branch has moved on since base.sha was recorded
  local output
  output=$(REVIEW_STATE=closed bash "${SCRIPT_DIR}/review_pr.sh" fake-token acme/widgets 7 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the snapshot compares with base.sha, the target as it was when the PR closed
  pull_requests_section "$output" SNAPSHOT | jq -e '. == {baseCommit: "merge111",
    sourceCommit: "head222", targetCommit: "base111"}' >/dev/null
}

test_review_fails_when_the_pr_moves_while_it_reads() {
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || { review_stub_rest "$@"; return; }
    local body connection
    body=$(cat)
    connection=$(grep -oE 'reviewThreads|reviews|comments' <<<"$body" | head -n1)
    jq -n --arg c "$connection" \
      '{data: {repository: {pullRequest: {($c): {pageInfo: {hasNextPage: false, endCursor: null}, nodes: []}}}}}'
  }
  export -f curl

  local move output status
  for move in head target; do
    # GIVEN a push to the PR, or a new commit on its target branch, while review reads it
    REVIEW_READS=$(mktemp)
    output=$(REVIEW_MOVE="$move" REVIEW_READS="$REVIEW_READS" \
      bash "${SCRIPT_DIR}/review_pr.sh" fake-token acme/widgets 7 2>&1)
    status=$?
    rm -f "$REVIEW_READS"

    # THEN review fails and prints no snapshot that pairs one revision with another's files
    [[ $status -ne 0 ]] &&
      assert_contains "$output" 'PR 7 changed while review was reading it' &&
      assert_not_contains "$output" '=== SNAPSHOT ===' || {
        printf 'No failure for a moved %s\n' "$move" >&2
        return 1
      }
  done
}

test_review_fails_on_graphql_errors_with_http_200() {
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || { review_stub_rest "$@"; return; }
    cat >/dev/null
    printf '%s' '{"data":null,"errors":[{"type":"FORBIDDEN","message":"Resource not accessible by personal access token"}]}'
  }
  export -f curl

  # GIVEN GraphQL answers HTTP 200 with an errors array
  local output status
  output=$(bash "${SCRIPT_DIR}/review_pr.sh" fake-token acme/widgets 7 2>&1)
  status=$?

  # THEN the run fails with the GraphQL message, and prints no partial review
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'Pull request threads were not listed.' &&
    assert_contains "$output" 'Resource not accessible by personal access token' &&
    assert_not_contains "$output" '=== PR METADATA ==='
}

test_fetch_encodes_path_and_ref() {
  curl() {
    [[ "$*" == *'Accept: application/vnd.github.raw+json'* ]] || return 90
    [[ "$*" == *'/repos/acme/widgets/contents/docs/My%20File%231.md?ref=bugfix%2F%23912729' ]] || return 91
    printf '%s' 'file-content'
  }
  export -f curl

  # GIVEN an Azure-style leading slash, a path with a space and #, and a full ref with #
  local output
  output=$(bash "${SCRIPT_DIR}/fetch_file.sh" fake-token acme/widgets \
    '/docs/My File#1.md' 'refs/heads/bugfix/#912729' 2>&1) || {
      printf '%s\n' "$output" >&2
      return 1
    }

  # THEN the path keeps its slashes, each segment and the ref are encoded, and raw content returns
  [[ "$output" == 'file-content' ]]
}

test_fetch_preserves_exact_file_bytes() {
  curl() {
    printf 'line1\n\nnaïve — end'
  }
  export -f curl

  local expected_file output_file error_file
  expected_file=$(mktemp)
  output_file=$(mktemp)
  error_file=$(mktemp)
  trap 'rm -f "$expected_file" "$output_file" "$error_file"' RETURN

  # GIVEN a file with a blank line, non-ASCII text, and no trailing newline
  printf 'line1\n\nnaïve — end' >"$expected_file"
  bash "${SCRIPT_DIR}/fetch_file.sh" fake-token acme/widgets src/main.cs 0123456789abcdef \
    >"$output_file" 2>"$error_file" || {
      cat "$error_file" >&2
      return 1
    }

  # THEN the bytes arrive unchanged, with nothing on stderr
  cmp -s "$expected_file" "$output_file" && [[ ! -s "$error_file" ]]
}

test_fetch_propagates_http_failure() {
  curl() {
    printf '%s' '{"message":"Not Found"}'
    return 22
  }
  export -f curl

  # GIVEN a path that GitHub does not hold at the ref
  # THEN fetch fails rather than printing the error body as file content
  ! bash "${SCRIPT_DIR}/fetch_file.sh" fake-token acme/widgets src/missing.cs develop >/dev/null 2>&1
}

test_issue_shows_the_issue_and_its_comments() {
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/issues/88')
        printf '%s' '{"number":88,"title":"Add 410 Gone","state":"open","state_reason":null,"user":{"login":"alex"},
          "labels":[{"name":"enhancement"}],"html_url":"u","body":"Return 410 for a deleted record."}'
        ;;
      *'-X GET'*'/repos/acme/widgets/issues/88/comments?sort=created&per_page=100&page=1')
        printf '%s' '[{"user":{"login":"blair"},"created_at":"2026-11-01T00:00:00Z","body":"Also for IResult."}]'
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN an issue with one comment
  # WHEN the issue is shown
  local output
  output=$(bash "${SCRIPT_DIR}/show_issue.sh" fake-token acme/widgets 88 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the issue and its comment are reported
  jq -e '.id == 88 and .title == "Add 410 Gone" and .labels == ["enhancement"]
      and .body == "Return 410 for a deleted record."
      and .comments == [{author: "blair", created: "2026-11-01T00:00:00Z", body: "Also for IResult."}]' \
    <<<"$output" >/dev/null
}

# A comment list over 1 MiB exceeds the argument limit of macOS, and 128 KiB the one of Linux.
test_issue_reads_a_long_comment_list() {
  # The page sits in a file: as an exported variable, it would exceed the limit of every exec.
  ISSUE_PAGE_FILE=$(mktemp)
  trap "rm -f '$ISSUE_PAGE_FILE'" EXIT
  jq -nc '[range(1; 101) | {user: {login: "u\(.)"}, created_at: "2026-11-01T00:00:00Z",
    body: ("x" * 11000)}]' >"$ISSUE_PAGE_FILE"
  export ISSUE_PAGE_FILE
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/issues/88')
        printf '%s' '{"number":88,"title":"T","state":"open","user":{"login":"alex"},"html_url":"u","body":"B"}'
        ;;
      *'/issues/88/comments?sort=created&per_page=100&page=1') cat "$ISSUE_PAGE_FILE" ;;
      *'/issues/88/comments?sort=created&per_page=100&page=2') printf '%s' '[]' ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN an issue with 100 comments of 11,000 characters each
  # WHEN the issue is shown
  local output
  output=$(bash "${SCRIPT_DIR}/show_issue.sh" fake-token acme/widgets 88 2>&1) || {
    printf '%s\n' "${output:0:500}" >&2
    return 1
  }

  # THEN every comment is reported
  jq -e '.comments | length == 100 and .[99].author == "u100"' <<<"$output" >/dev/null
}

test_issue_refuses_a_pull_request_number() {
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/issues/89')
        printf '%s' '{"number":89,"title":"T","pull_request":{"url":"p"}}'
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN a number that belongs to a pull request
  # WHEN it is shown as an issue
  local output
  if output=$(bash "${SCRIPT_DIR}/show_issue.sh" fake-token acme/widgets 89 2>&1); then
    return 1
  fi

  # THEN the script fails and points to review
  assert_contains "$output" '#89 is a pull request' &&
    assert_contains "$output" 'run.sh review 89'
}

run_test 'review compares an open PR with its target branch, and reports the merge base, files, threads, comments, and reviewers' test_review_reports_snapshot_files_threads_and_reviews
run_test 'review compares a closed PR with its recorded base' test_review_compares_a_closed_pr_with_its_recorded_base
run_test 'review fails when the PR moves while it reads' test_review_fails_when_the_pr_moves_while_it_reads
run_test 'review fails on GraphQL errors with HTTP 200' test_review_fails_on_graphql_errors_with_http_200
run_test 'fetch encodes the path and the ref' test_fetch_encodes_path_and_ref
run_test 'fetch preserves exact file bytes' test_fetch_preserves_exact_file_bytes
run_test 'fetch propagates HTTP failures' test_fetch_propagates_http_failure
run_test 'issue shows the issue and its comments' test_issue_shows_the_issue_and_its_comments
run_test 'issue reads a comment list over the argument limit' test_issue_reads_a_long_comment_list
run_test 'issue refuses a pull request number' test_issue_refuses_a_pull_request_number
