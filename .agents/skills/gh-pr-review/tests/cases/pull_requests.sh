#!/usr/bin/env bash
# Pull request actions — cases for the GitHub skill scripts.
#
# Sourced by harness.sh, never run directly: it supplies SCRIPT_DIR, the assert helpers and
# run_test, and each case runs where it is sourced. Every case stubs curl itself, so this file
# depends on nothing another case file defines.
#
# The stub sees the curl arguments in "$*", with the URL last. A request with a body sends it on
# stdin (--data-binary @-), so the stub reads stdin only then; a bare `cat` on a GET would block.
# GraphQL requests all POST to https://api.github.com/graphql, so a stub tells them apart by the
# operation name inside the query.

# pull_requests_section <output> <name>
# Print the JSON of one "=== NAME ===" section of review output.
pull_requests_section() {
  printf '%s\n' "$1" | awk -v header="=== $2 ===" '
    $0 == header { found = 1; next }
    /^=== / { found = 0 }
    found'
}

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

  # GIVEN an Azure DevOps status name that GitHub does not use
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

# The commit a reviewer read, which review comments are anchored to.
COMMENT_REVIEWED_SHA=0123456789abcdef0123456789abcdef01234567
export COMMENT_REVIEWED_SHA

test_comment_general_posts_an_issue_comment() {
  curl() {
    [[ "$*" == *'-X POST'*'/repos/acme/widgets/issues/12/comments' ]] || return 90
    jq -e '. == {body: "General note."}' >/dev/null || return 91
    printf '%s' '{"node_id":"IC_1","html_url":"https://github.com/acme/widgets/pull/12#issuecomment-1"}'
  }
  export -f curl

  # GIVEN a comment with no file
  local output
  output=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'General note.' 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN it posts on the conversation tab and reports its node id
  jq -e '.id == "IC_1" and .kind == "general"' <<<"$output" >/dev/null
}

test_comment_on_a_file_sends_file_subject() {
  curl() {
    [[ "$*" == *'-X POST'*'/repos/acme/widgets/pulls/12/comments' ]] || return 90
    jq -e --arg sha "$COMMENT_REVIEWED_SHA" \
      '. == {body: "File note.", commit_id: $sha, path: "src/a.cs", subject_type: "file"}' >/dev/null || return 91
    printf '%s' '{"node_id":"PRRC_1","path":"src/a.cs","line":null,"html_url":"u"}'
  }
  export -f curl

  # GIVEN a comment with the reviewed commit, a file, and no line
  local output
  output=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'File note.' "$COMMENT_REVIEWED_SHA" /src/a.cs 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN it posts a file-level review comment on the reviewed commit, without asking for the head
  jq -e '.id == "PRRC_1" and .kind == "review" and .file == "src/a.cs"' <<<"$output" >/dev/null
}

test_comment_on_a_line_sends_line_and_right_side() {
  curl() {
    [[ "$*" == *'-X POST'*'/repos/acme/widgets/pulls/12/comments' ]] || return 90
    jq -e --arg sha "$COMMENT_REVIEWED_SHA" \
      '. == {body: "Line note.", commit_id: $sha, path: "src/a.cs", line: 8, side: "RIGHT"}' >/dev/null || return 91
    printf '%s' '{"node_id":"PRRC_2","path":"src/a.cs","line":8,"html_url":"u"}'
  }
  export -f curl

  # GIVEN a comment with the reviewed commit, a file, and a line
  local output
  output=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'Line note.' "$COMMENT_REVIEWED_SHA" src/a.cs 8 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN it posts a line comment on the new side of the diff
  jq -e '.id == "PRRC_2" and .line == 8' <<<"$output" >/dev/null
}

test_comment_422_names_its_cause_and_way_out() {
  curl() {
    cat >/dev/null
    printf '{"message":"Validation Failed","errors":["%s"]}' "$COMMENT_422_CAUSE"
    return 22
  }
  export -f curl

  local cause hint other output status
  for cause in 'pull_request_review_thread.line must be part of the diff' \
    'user_id can only have one pending review per pull request'; do
    if [[ "$cause" == *diff* ]]; then
      hint='HINT: a line comment needs a line inside a diff hunk'
      other='pending review on PR'
    else
      hint='HINT: your account has a pending review on PR 12'
      other='inside a diff hunk'
    fi

    # GIVEN GitHub refuses the review comment with 422 for this cause
    output=$(COMMENT_422_CAUSE="$cause" bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 \
      'Line note.' "$COMMENT_REVIEWED_SHA" src/a.cs 900 2>&1)
    status=$?

    # THEN the run fails, shows GitHub's reason, and gives only the hint for that cause
    [[ $status -ne 0 ]] &&
      assert_contains "$output" "$cause" &&
      assert_contains "$output" "$hint" &&
      assert_not_contains "$output" "$other" || return 1
  done
}

test_comment_rejects_bad_anchor_arguments_before_http() {
  CURL_MARKER=$(mktemp -u)
  export CURL_MARKER
  curl() { : >"$CURL_MARKER"; return 90; }
  export -f curl

  # GIVEN a commit with no file, an empty commit with a file and a line, a line alone, a short
  # commit, and a line that is not a number
  local no_file no_commit line_only short_commit not_number
  no_file=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'Note.' "$COMMENT_REVIEWED_SHA" 2>&1) && return 1
  no_commit=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'Note.' '' src/a.cs 8 2>&1) && return 1
  line_only=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'Note.' '' '' 8 2>&1) && return 1
  short_commit=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'Note.' 0123456 src/a.cs 8 2>&1) && return 1
  not_number=$(bash "${SCRIPT_DIR}/post_comment.sh" fake-token acme/widgets 12 'Note.' "$COMMENT_REVIEWED_SHA" src/a.cs 8a 2>&1) && return 1

  # THEN each is refused with a reason, and curl never runs
  assert_contains "$no_file" 'a review comment needs COMMIT and FILE_PATH' &&
    assert_contains "$no_commit" 'a review comment needs COMMIT and FILE_PATH' &&
    assert_contains "$line_only" 'a review comment needs COMMIT and FILE_PATH' &&
    assert_contains "$short_commit" 'COMMIT must be the full 40-character SHA' &&
    assert_contains "$not_number" "LINE must be a positive line number, got '8a'" &&
    [[ ! -e "$CURL_MARKER" ]]
}

# thread_stub_graphql
# Answer the GraphQL calls of reply and resolve, and log each operation to $THREAD_LOG. The node
# lookup answers PRRT_ ids as review threads; IC_issue as a comment on an issue; other IC_ ids as
# general comments on the PR with node id PR_12, whose Markdown body opens with a blank line and an
# earlier quote, then $IC_FIRST_LINE, and whose author is $IC_AUTHOR (empty for a deleted account);
# and PRRC_ ids as review comments. $MINIMIZED_STATE overrides the state minimizeComment reports.
thread_stub_graphql() {
  local body query id
  body=$(cat)
  query=$(jq -r '.query' <<<"$body")
  id=$(jq -r '.variables.id // .variables.thread' <<<"$body")
  case "$query" in
    *'node(id:'*)
      printf 'node\n' >>"$THREAD_LOG"
      case "$id" in
        PRRT_*) printf '%s' '{"data":{"node":{"__typename":"PullRequestReviewThread"}}}' ;;
        IC_issue) printf '%s' '{"data":{"node":{"__typename":"IssueComment","url":"u","body":"b","author":null,"pullRequest":null}}}' ;;
        IC_*)
          jq -n --arg author "${IC_AUTHOR-nigel}" --arg line "${IC_FIRST_LINE:-[Major] The call-out names \`@casey\` and \`#12\`.}" \
            '{data: {node: {__typename: "IssueComment",
              url: "https://github.com/acme/widgets/pull/12#issuecomment-5",
              body: ("\n> @blair [wrote](u):\n> Earlier point.\n\n" + $line + "\nSecond line.\n"),
              author: (if $author == "" then null else {login: $author} end),
              pullRequest: {id: "PR_12"}}}}' ;;
        *) printf '%s' '{"data":{"node":{"__typename":"PullRequestReviewComment"}}}' ;;
      esac
      ;;
    *addComment*)
      printf 'addComment\n' >>"$THREAD_LOG"
      jq -e '.variables.subject == "PR_12"' <<<"$body" >/dev/null || return 94
      jq '{data: {addComment: {commentEdge: {node: {id: "IC_9", body: .variables.body, url: "u"}}}}}' <<<"$body" ;;
    *addPullRequestReviewThreadReply*)
      printf 'reply\n' >>"$THREAD_LOG"
      jq -e '.variables == {thread: "PRRT_1", body: "Fixed in abc1234."}' <<<"$body" >/dev/null || return 91
      printf '%s' '{"data":{"addPullRequestReviewThreadReply":{"comment":{"id":"PRRC_9","body":"Fixed in abc1234.","url":"u"}}}}' ;;
    *' resolveReviewThread('*)
      printf 'resolveReviewThread\n' >>"$THREAD_LOG"
      printf '%s' '{"data":{"resolveReviewThread":{"thread":{"id":"PRRT_1","isResolved":true,"path":"src/a.cs"}}}}' ;;
    *unresolveReviewThread*)
      printf 'unresolveReviewThread\n' >>"$THREAD_LOG"
      printf '%s' '{"data":{"unresolveReviewThread":{"thread":{"id":"PRRT_1","isResolved":false,"path":"src/a.cs"}}}}' ;;
    *unminimizeComment*)
      printf 'unminimizeComment\n' >>"$THREAD_LOG"
      printf '%s' '{"data":{"unminimizeComment":{"unminimizedComment":{"isMinimized":false,"minimizedReason":null}}}}' ;;
    *minimizeComment*)
      [[ "$query" == *'classifier: RESOLVED'* ]] || return 92
      printf 'minimizeComment\n' >>"$THREAD_LOG"
      local state='{"isMinimized":true,"minimizedReason":"resolved"}'
      [[ -n "${MINIMIZED_STATE:-}" ]] && state="$MINIMIZED_STATE"
      printf '{"data":{"minimizeComment":{"minimizedComment":%s}}}' "$state" ;;
    *) return 93 ;;
  esac
}
export -f thread_stub_graphql

test_reply_posts_to_a_review_thread() {
  THREAD_LOG=$(mktemp)
  export THREAD_LOG
  curl() {
    [[ "$*" == *'-X POST'*'https://api.github.com/graphql' ]] || return 90
    thread_stub_graphql
  }
  export -f curl

  # GIVEN a review thread id and a reply
  local output
  output=$(bash "${SCRIPT_DIR}/reply_comment.sh" fake-token acme/widgets PRRT_1 'Fixed in abc1234.' 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the reply is nested in that thread, and the new comment is reported
  jq -e '. == {kind: "thread", inReplyTo: "PRRT_1", id: "PRRC_9", content: "Fixed in abc1234.", url: "u"}' \
    <<<"$output" >/dev/null &&
    [[ "$(<"$THREAD_LOG")" == $'node\nreply' ]]
}

test_reply_quotes_a_general_comment() {
  THREAD_LOG=$(mktemp)
  export THREAD_LOG
  curl() {
    [[ "$*" == *'-X POST'*'https://api.github.com/graphql' ]] || return 90
    thread_stub_graphql
  }
  export -f curl

  # GIVEN a general comment by nigel that opens with a quote of an earlier comment, then a line
  # whose code spans hold a mention and a PR reference
  local output long_line deleted code_cut
  output=$(bash "${SCRIPT_DIR}/reply_comment.sh" fake-token acme/widgets IC_5 'Fixed in abc1234.' 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the reply goes to the PR that holds the comment, mentions the author, links the original,
  # quotes its first own line as Markdown, so the code spans stay code, and then holds the reply
  jq -e '.kind == "general" and .inReplyTo == "IC_5" and .id == "IC_9"
    and .content == "> @nigel [wrote](https://github.com/acme/widgets/pull/12#issuecomment-5):\n> [Major] The call-out names `@casey` and `#12`.\n\nFixed in abc1234."' \
    <<<"$output" >/dev/null &&
    [[ "$(<"$THREAD_LOG")" == $'node\naddComment' ]] || return 1

  # AND a long first line is cut at 200 characters, and a deleted author gets a plain link
  long_line=$(printf 'x%.0s' $(seq 1 250))
  deleted=$(IC_AUTHOR='' IC_FIRST_LINE="$long_line" \
    bash "${SCRIPT_DIR}/reply_comment.sh" fake-token acme/widgets IC_5 'Done.' 2>&1) || return 1
  jq -e --arg cut "${long_line:0:200}…" \
    '.content == "> [Comment](https://github.com/acme/widgets/pull/12#issuecomment-5):\n> \($cut)\n\nDone."' \
    <<<"$deleted" >/dev/null || return 1

  # AND a cut inside a code span closes the span
  code_cut=$(IC_FIRST_LINE="$(printf 'y%.0s' $(seq 1 190))\`$(printf 'z%.0s' $(seq 1 40))\`" \
    bash "${SCRIPT_DIR}/reply_comment.sh" fake-token acme/widgets IC_5 'Done.' 2>&1) || return 1
  jq -e '.content | split("\n")[1] | endswith("…`")' <<<"$code_cut" >/dev/null
}

test_reply_and_resolve_refuse_other_targets() {
  THREAD_LOG=$(mktemp)
  export THREAD_LOG
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || return 90
    thread_stub_graphql
  }
  export -f curl

  # GIVEN a comment on an issue rather than a PR, and a review comment id
  local issue review
  issue=$(bash "${SCRIPT_DIR}/reply_comment.sh" fake-token acme/widgets IC_issue 'Reply.' 2>&1) && return 1
  review=$(bash "${SCRIPT_DIR}/resolve_thread.sh" fake-token acme/widgets PRRC_1 resolve 2>&1) && return 1

  # THEN each is refused with its reason, and only the node lookups ran
  assert_contains "$issue" 'is not a comment on a pull request' &&
    assert_contains "$review" 'is not a review thread (PRRT_…) or a general comment (IC_…)' &&
    [[ "$(<"$THREAD_LOG")" == $'node\nnode' ]]
}

test_reply_refuses_a_review_comment_id() {
  THREAD_LOG=$(mktemp)
  export THREAD_LOG
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || return 90
    thread_stub_graphql
  }
  export -f curl

  # GIVEN the id of a review comment rather than of its thread
  local output
  output=$(bash "${SCRIPT_DIR}/reply_comment.sh" fake-token acme/widgets PRRC_1 'Reply.' 2>&1) && return 1

  # THEN nothing is posted, and the message points at the thread
  assert_contains "$output" 'reply to its thread' &&
    [[ "$(<"$THREAD_LOG")" == node ]]
}

test_resolve_and_unresolve_a_review_thread() {
  THREAD_LOG=$(mktemp)
  export THREAD_LOG
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || return 90
    thread_stub_graphql
  }
  export -f curl

  # GIVEN one thread, resolved and then reopened
  local resolved unresolved
  resolved=$(bash "${SCRIPT_DIR}/resolve_thread.sh" fake-token acme/widgets PRRT_1 resolve 2>&1) &&
    unresolved=$(bash "${SCRIPT_DIR}/resolve_thread.sh" fake-token acme/widgets PRRT_1 unresolve 2>&1) || {
      printf '%s\n%s\n' "${resolved:-}" "${unresolved:-}" >&2
      return 1
    }

  # THEN each mode runs its own thread mutation and reports the new state
  jq -e '. == {kind: "thread", id: "PRRT_1", isResolved: true, file: "src/a.cs"}' <<<"$resolved" >/dev/null &&
    jq -e '. == {kind: "thread", id: "PRRT_1", isResolved: false, file: "src/a.cs"}' <<<"$unresolved" >/dev/null &&
    [[ "$(grep -v '^node$' "$THREAD_LOG")" == $'resolveReviewThread\nunresolveReviewThread' ]]
}

test_resolve_and_unresolve_a_general_comment() {
  THREAD_LOG=$(mktemp)
  export THREAD_LOG
  curl() {
    [[ "$*" == *'https://api.github.com/graphql' ]] || return 90
    thread_stub_graphql
  }
  export -f curl

  # GIVEN one general comment, resolved and then reopened
  local resolved unresolved not_taken
  resolved=$(bash "${SCRIPT_DIR}/resolve_thread.sh" fake-token acme/widgets IC_5 resolve 2>&1) &&
    unresolved=$(bash "${SCRIPT_DIR}/resolve_thread.sh" fake-token acme/widgets IC_5 unresolve 2>&1) || {
      printf '%s\n%s\n' "${resolved:-}" "${unresolved:-}" >&2
      return 1
    }

  # THEN resolve hides it with the reason RESOLVED, and unresolve shows it again
  jq -e '. == {kind: "general", id: "IC_5", isResolved: true}' <<<"$resolved" >/dev/null &&
    jq -e '. == {kind: "general", id: "IC_5", isResolved: false}' <<<"$unresolved" >/dev/null &&
    [[ "$(grep -v '^node$' "$THREAD_LOG")" == $'minimizeComment\nunminimizeComment' ]] || return 1

  # AND a hide that GitHub reports under another reason is a failure, not a resolve
  not_taken=$(MINIMIZED_STATE='{"isMinimized":true,"minimizedReason":"outdated"}' \
    bash "${SCRIPT_DIR}/resolve_thread.sh" fake-token acme/widgets IC_5 resolve 2>&1) && return 1
  assert_contains "$not_taken" 'does not report the resolved state'
}

# comment_stub_graphql
# Answer the node lookup and the comment mutations of edit and delete, and log each mutation name
# to $MUTATION_LOG. IC_ ids are issue comments, PRRC_ ids review comments; any other id is a user.
# An update echoes the new body, unless $STORED_BODY overrides it.
comment_stub_graphql() {
  local body query id
  body=$(cat)
  query=$(jq -r '.query' <<<"$body")
  id=$(jq -r '.variables.id' <<<"$body")
  case "$query" in
    *'node(id:'*)
      case "$id" in
        IC_*) printf '%s' '{"data":{"node":{"__typename":"IssueComment"}}}' ;;
        PRRC_*) printf '%s' '{"data":{"node":{"__typename":"PullRequestReviewComment"}}}' ;;
        *) printf '%s' '{"data":{"node":{"__typename":"User"}}}' ;;
      esac
      ;;
    *updateIssueComment*)
      printf 'updateIssueComment %s\n' "$id" >>"$MUTATION_LOG"
      jq -c --arg stored "${STORED_BODY:-}" \
        '{data: {updateIssueComment: {issueComment: {id: .variables.id, body: (if $stored == "" then .variables.body else $stored end)}}}}' \
        <<<"$body"
      ;;
    *updatePullRequestReviewComment*)
      printf 'updatePullRequestReviewComment %s\n' "$id" >>"$MUTATION_LOG"
      jq -c '{data: {updatePullRequestReviewComment: {pullRequestReviewComment: {id: .variables.id, body: .variables.body}}}}' \
        <<<"$body"
      ;;
    *deleteIssueComment* | *deletePullRequestReviewComment*)
      [[ "$query" =~ (delete[A-Za-z]+Comment) ]]
      printf '%s %s\n' "${BASH_REMATCH[1]}" "$id" >>"$MUTATION_LOG"
      printf '%s' '{"data":{"x":{"clientMutationId":null}}}'
      ;;
    *) return 91 ;;
  esac
}
export -f comment_stub_graphql

test_edit_dispatches_on_comment_type() {
  MUTATION_LOG=$(mktemp)
  export MUTATION_LOG
  trap 'rm -f "$MUTATION_LOG"' RETURN
  curl() { comment_stub_graphql; }
  export -f curl

  # GIVEN an issue comment and a review comment
  local issue review
  issue=$(bash "${SCRIPT_DIR}/edit_comment.sh" fake-token acme/widgets IC_1 'New text.' 2>&1) &&
    review=$(bash "${SCRIPT_DIR}/edit_comment.sh" fake-token acme/widgets PRRC_1 'Other text.' 2>&1) || {
      printf '%s\n%s\n' "${issue:-}" "${review:-}" >&2
      return 1
    }

  # THEN each is updated through the mutation for its type
  [[ "$(<"$MUTATION_LOG")" == $'updateIssueComment IC_1\nupdatePullRequestReviewComment PRRC_1' ]] &&
    jq -e '. == {id: "IC_1", content: "New text."}' <<<"$issue" >/dev/null &&
    jq -e '. == {id: "PRRC_1", content: "Other text."}' <<<"$review" >/dev/null
}

test_edit_fails_when_the_stored_text_differs() {
  MUTATION_LOG=$(mktemp)
  STORED_BODY='Old text.'
  export MUTATION_LOG STORED_BODY
  trap 'rm -f "$MUTATION_LOG"' RETURN
  curl() { comment_stub_graphql; }
  export -f curl

  # GIVEN an update whose response still holds the old text
  local output status
  output=$(bash "${SCRIPT_DIR}/edit_comment.sh" fake-token acme/widgets IC_1 'New text.' 2>&1)
  status=$?

  # THEN the run fails rather than reporting success
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'comment IC_1 does not hold the new text.'
}

test_delete_dispatches_on_comment_type() {
  MUTATION_LOG=$(mktemp)
  export MUTATION_LOG
  trap 'rm -f "$MUTATION_LOG"' RETURN
  curl() { comment_stub_graphql; }
  export -f curl

  # GIVEN an issue comment and a review comment
  local issue review
  issue=$(bash "${SCRIPT_DIR}/delete_comment.sh" fake-token acme/widgets IC_1 2>&1) &&
    review=$(bash "${SCRIPT_DIR}/delete_comment.sh" fake-token acme/widgets PRRC_1 2>&1) || {
      printf '%s\n%s\n' "${issue:-}" "${review:-}" >&2
      return 1
    }

  # THEN each is deleted through the mutation for its type
  [[ "$(<"$MUTATION_LOG")" == $'deleteIssueComment IC_1\ndeletePullRequestReviewComment PRRC_1' ]] &&
    jq -e '. == {deleted: true, id: "IC_1"}' <<<"$issue" >/dev/null &&
    jq -e '. == {deleted: true, id: "PRRC_1"}' <<<"$review" >/dev/null
}

test_edit_and_delete_reject_unknown_node_type() {
  MUTATION_LOG=$(mktemp)
  export MUTATION_LOG
  trap 'rm -f "$MUTATION_LOG"' RETURN
  curl() { comment_stub_graphql; }
  export -f curl

  # GIVEN an id that names a user rather than a comment
  local edited deleted
  edited=$(bash "${SCRIPT_DIR}/edit_comment.sh" fake-token acme/widgets MDQ_user 'Text.' 2>&1) && return 1
  deleted=$(bash "${SCRIPT_DIR}/delete_comment.sh" fake-token acme/widgets MDQ_user 2>&1) && return 1

  # THEN both refuse it by type, and no mutation runs
  assert_contains "$edited" "'MDQ_user' is not a pull request comment (found: User)" &&
    assert_contains "$deleted" "'MDQ_user' is not a pull request comment (found: User)" &&
    [[ ! -s "$MUTATION_LOG" ]]
}

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
  approve=$(bash "${SCRIPT_DIR}/vote_pr.sh" fake-token acme/widgets 42 approve 2>&1) &&
    changes=$(bash "${SCRIPT_DIR}/vote_pr.sh" fake-token acme/widgets 42 request-changes 'Fix the test.' 2>&1) &&
    comment=$(bash "${SCRIPT_DIR}/vote_pr.sh" fake-token acme/widgets 42 comment 'A question.' 2>&1) || {
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
  no_body=$(bash "${SCRIPT_DIR}/vote_pr.sh" fake-token acme/widgets 42 request-changes 2>&1) && return 1
  invalid=$(bash "${SCRIPT_DIR}/vote_pr.sh" fake-token acme/widgets 42 10 2>&1) && return 1

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
  output=$(bash "${SCRIPT_DIR}/vote_pr.sh" fake-token acme/widgets 42 approve 2>&1)
  status=$?

  # THEN the run fails with GitHub's reason
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'review was not submitted on PR 42.' &&
    assert_contains "$output" 'Can not approve your own pull request'
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
  output=$(bash "${SCRIPT_DIR}/publish_pr.sh" fake-token acme/widgets 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/publish_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN no mutation runs, and it still reports isDraft false
  [[ ! -e "$CURL_MARKER" ]] &&
    jq -e '.isDraft == false and .id == 42' <<<"$output" >/dev/null
}

# complete_pull_json <state> <draft> <merged> <body_json> [head_sha] [head_repository]
# Print a pull request as GET /pulls/42 returns it, with the given state, flags, and body. Its head
# is feature/x, in acme/widgets unless head_repository names a fork, and its target is develop.
complete_pull_json() {
  jq -n --arg state "$1" --argjson draft "$2" --argjson merged "$3" --argjson body "$4" \
    --arg sha "${5:-${COMPLETE_HEAD:-abc123}}" --arg head_repo "${6:-acme/widgets}" '{
    number: 42, title: "Admin: add the thing", state: $state, draft: $draft, merged: $merged,
    body: $body, html_url: "u",
    head: {sha: $sha, ref: "feature/x", repo: {full_name: $head_repo}},
    base: {ref: "develop", repo: {full_name: "acme/widgets", default_branch: "develop"}}
  }'
}

# complete_enter_temp_dir
# Enter a new temporary directory outside any Git clone, and remove it when the case ends.
complete_enter_temp_dir() {
  COMPLETE_TEMP_DIR=$(mktemp -d)
  trap 'rm -rf "$COMPLETE_TEMP_DIR"' EXIT
  cd "$COMPLETE_TEMP_DIR"
}

# complete_origin_fixture
# Enter a temporary directory, outside any clone, that holds origin.git: a bare repository that
# stands in for https://github.com/acme/widgets.git through url.insteadOf in a private global Git
# config. origin.git has develop at a base commit, and feature/x and refs/pull/42/head one commit
# ahead. Sets COMPLETE_HEAD to that commit, the head that the pull request merges.
complete_origin_fixture() {
  complete_enter_temp_dir
  export GIT_AUTHOR_NAME=t GIT_AUTHOR_EMAIL=t@example.com GIT_COMMITTER_NAME=t GIT_COMMITTER_EMAIL=t@example.com
  export GIT_CONFIG_GLOBAL="${COMPLETE_TEMP_DIR}/gitconfig" GIT_CONFIG_NOSYSTEM=1
  git config --global url."${COMPLETE_TEMP_DIR}/origin.git".insteadOf https://github.com/acme/widgets.git
  git init -q --bare -b develop origin.git
  git init -q -b develop seed
  (
    cd seed
    echo base >file && git add file && git commit -q -m base
    git push -q https://github.com/acme/widgets.git develop
    git switch -q -c feature/x
    echo feature >>file && git commit -q -am feature
    git push -q https://github.com/acme/widgets.git feature/x feature/x:refs/pull/42/head
  )
  COMPLETE_HEAD=$(git -C seed rev-parse feature/x)
  export COMPLETE_HEAD
}

# COMPLETE_OUTSIDE_A_CLONE — the branch outcomes of a merge run outside a Git clone.
COMPLETE_OUTSIDE_A_CLONE='remoteBranch: "deleted", localBranch: "kept: not run in a Git clone", targetBranch: "not updated: not run in a Git clone"'

# The asynchronous merge API is polled; the tests poll at once. Exported here so that every
# complete case, which runs in a subshell, sees it without each case setting it.
export GH_MERGE_POLL_INTERVAL=0

# complete_async_pending <uuid>
# Print the body GitHub returns when it accepts a merge request, or when one is already pending.
complete_async_pending() {
  printf '{"status":"pending","details":{"message":"Merge request pending","uuid":"%s","merge_method":"squash","merge_action":"direct_merge","expected_head_sha":"%s"}}' "$1" "${COMPLETE_HEAD:-abc123}"
}

# complete_async_merged <sha>
# Print the body GitHub returns once a merge request has merged, from the PUT or from a poll.
complete_async_merged() {
  printf '{"status":"merged","details":{"message":"Pull Request successfully merged","sha":"%s"}}' "$1"
}
export -f complete_async_pending complete_async_merged
export COMPLETE_OUTSIDE_A_CLONE

test_complete_squash_merges_with_the_description() {
  complete_origin_fixture
  COMPLETE_PULL=$(complete_pull_json open false false '"### Why\n\nA reason — with an em dash.\n\nRelated: AB#123456"')
  export COMPLETE_PULL
  curl() {
    local body
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
      *'-X PUT'*'/repos/acme/widgets/pulls/42/merge-async')
        [[ "$*" == *'X-GitHub-Api-Version: 2026-03-10'* ]] || return 92
        body=$(cat)
        jq -e --arg sha "$COMPLETE_HEAD" '. == {
          merge_method: "squash",
          merge_action: "direct_merge",
          commit_title: "Admin: add the thing (#42)",
          commit_message: "### Why\n\nA reason — with an em dash.\n\nRelated: AB#123456",
          sha: $sha
        }' <<<"$body" >/dev/null || return 91
        complete_async_pending u1
        ;;
      *'-X GET'*'/repos/acme/widgets/pulls/42/merge-async/u1') complete_async_merged m1 ;;
      *'-X GET'*'/repos/acme/widgets/pulls?state=open&per_page=100&base=feature%2Fx') printf '[]' ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN an open pull request with a multiline description
  # WHEN it is completed outside a Git clone
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN it is squash-merged at the head it read, with the title and (#42) as the subject and the
  # description, byte for byte, as the commit message
  # AND the branch on GitHub is deleted, while the local steps report why they did not run
  jq -e ". == {id: 42, merged: true, method: \"squash\", commit: \"m1\", subject: \"Admin: add the thing (#42)\", url: \"u\", ${COMPLETE_OUTSIDE_A_CLONE}}" \
    <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:\|^Waiting for merge request')" >/dev/null &&
    ! git -C origin.git show-ref --verify --quiet refs/heads/feature/x
}

test_complete_refuses_before_merge() {
  CURL_MARKER=$(mktemp -u)
  export CURL_MARKER
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
      *) : >"$CURL_MARKER"; return 90 ;;
    esac
  }
  export -f curl

  # GIVEN pull requests that are merged, closed, draft, or without a description
  local pull expected output
  local -a cases=(
    "closed|true|false|\"d\"|is already merged."
    "closed|false|false|\"d\"|is closed."
    "open|false|true|\"d\"|is a draft. Run publish first."
    "open|false|false|null|has no description"
    "open|false|false|\" \\n \"|has no description"
  )
  for pull in "${cases[@]}"; do
    IFS='|' read -r state merged draft body expected <<<"$pull"
    COMPLETE_PULL=$(complete_pull_json "$state" "$draft" "$merged" "$body")
    export COMPLETE_PULL

    # WHEN each is completed
    # THEN the run fails with the way out, and no merge request is sent
    output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) && return 1
    assert_contains "$output" "$expected" || return 1
  done
  [[ ! -e "$CURL_MARKER" ]]
}

test_complete_reports_a_merge_with_a_long_description() {
  # The pull request is served from a file: an exported variable this large breaks every child process.
  complete_origin_fixture
  COMPLETE_PULL_FILE="${COMPLETE_TEMP_DIR}/pull.json"
  export COMPLETE_PULL_FILE
  complete_pull_json open false false '"d"' | jq '.body = ([range(0; 45000) | "—"] | join(""))' >"$COMPLETE_PULL_FILE"
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42') cat "$COMPLETE_PULL_FILE" ;;
      *'-X PUT'*'/repos/acme/widgets/pulls/42/merge-async') cat >/dev/null; complete_async_merged m1 ;;
      *'-X GET'*'/repos/acme/widgets/pulls?state=open&per_page=100&base=feature%2Fx') printf '[]' ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN an open pull request whose description is longer than one command-line argument allows
  # WHEN it is completed
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the merge is reported, not failed after GitHub merged
  jq -e ". == {id: 42, merged: true, method: \"squash\", commit: \"m1\", subject: \"Admin: add the thing (#42)\", url: \"u\", ${COMPLETE_OUTSIDE_A_CLONE}}" \
    <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:')" >/dev/null
}

test_complete_reports_each_remote_branch_outcome() {
  # GIVEN a branch that GitHub already deleted, a branch that moved after the merge, a deletion that
  # GitHub refuses, a fork, a base of stacked pull requests, and a failed lookup of those requests
  local row case head_repo expected
  local -a rows=(
    "gone|acme/widgets|already deleted"
    "moved|acme/widgets|kept: it moved after the merge"
    "refused|acme/widgets|kept: the deletion failed:"
    "fork|someone/widgets|kept: the branch is in another repository"
    "stacked|acme/widgets|kept: open pull requests target it: #3687, #3700"
    "unlisted|acme/widgets|kept: the open pull requests could not be listed"
  )
  for row in "${rows[@]}"; do
    IFS='|' read -r case head_repo expected <<<"$row"
    (
      complete_origin_fixture
      COMPLETE_PULL=$(complete_pull_json open false false '"d"' "$COMPLETE_HEAD" "$head_repo")
      COMPLETE_CASE="$case"
      export COMPLETE_PULL COMPLETE_CASE
      curl() {
        case "$*" in
          *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
          *'-X PUT'*'/repos/acme/widgets/pulls/42/merge-async') cat >/dev/null; complete_async_merged m1 ;;
          *'-X GET'*'/repos/acme/widgets/pulls?state=open&per_page=100&base=feature%2Fx')
            case "$COMPLETE_CASE" in
              stacked) printf '%s' '[{"number":3687},{"number":3700}]' ;;
              unlisted) printf '%s' '{"message":"Server Error"}'; return 22 ;;
              *) printf '[]' ;;
            esac
            ;;
          *) return 90 ;;
        esac
      }
      export -f curl
      local moved=""
      case "$case" in
        gone) git -C origin.git update-ref -d refs/heads/feature/x ;;
        moved)
          (cd seed && echo more >>file && git commit -q -am more && git push -q https://github.com/acme/widgets.git feature/x)
          moved=$(git -C seed rev-parse feature/x)
          ;;
        refused) printf '#!/bin/sh\nexit 1\n' >origin.git/hooks/pre-receive && chmod +x origin.git/hooks/pre-receive ;;
      esac

      # WHEN each is completed
      # THEN the run succeeds and reports the outcome; a kept branch also warns on stderr
      local output
      output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || exit 1
      jq -e --arg expected "$expected" '.merged and (.remoteBranch | startswith($expected))' \
        <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:')" >/dev/null || exit 1
      if [[ "$expected" == kept:* ]]; then
        assert_contains "$output" "WARNING: remote branch: ${expected}" || exit 1
        # AND a kept branch still points where it did, so a later push keeps its commits
        [[ "$(git -C origin.git rev-parse refs/heads/feature/x)" == "${moved:-$COMPLETE_HEAD}" ]] || exit 1
      fi
    ) || return 1
  done
}

# complete_clone_fixture
# Clone the origin of complete_origin_fixture as work, and leave the shell in it on feature/x.
# origin/develop holds the squash commit of feature/x, which the local develop lacks. Sets
# COMPLETE_SQUASH to the squash commit, and exports COMPLETE_PULL for COMPLETE_HEAD.
complete_clone_fixture() {
  complete_origin_fixture
  git clone -q https://github.com/acme/widgets.git work
  cd work
  git switch -q feature/x
  git switch -q develop
  git merge -q --squash feature/x >/dev/null && git commit -q -m 'Admin: add the thing (#42)'
  COMPLETE_SQUASH=$(git rev-parse HEAD)
  git push -q origin develop
  git reset -q --hard HEAD~1
  git switch -q feature/x
  COMPLETE_PULL=$(complete_pull_json open false false '"d"' "$COMPLETE_HEAD")
  export COMPLETE_PULL COMPLETE_SQUASH
}

# complete_clone_curl
# Stub the merge and the lookup of stacked pull requests for complete_clone_fixture.
complete_clone_curl() {
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
      *'-X PUT'*'/repos/acme/widgets/pulls/42/merge-async') cat >/dev/null; complete_async_merged "$COMPLETE_SQUASH" ;;
      *'-X GET'*'/repos/acme/widgets/pulls?state=open&per_page=100&base=feature%2Fx') printf '[]' ;;
      *) return 90 ;;
    esac
  }
  export -f curl
}

test_complete_cleans_up_the_local_clone() {
  # GIVEN a clone on the merged feature branch, with develop behind origin/develop
  complete_clone_fixture
  complete_clone_curl

  # WHEN the pull request is completed in the clone
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the clone is on develop at the squash commit, and feature/x is gone locally and as a
  # remote-tracking branch
  [[ "$(git symbolic-ref --short HEAD)" == develop ]] &&
    [[ "$(git rev-parse develop)" == "$COMPLETE_SQUASH" ]] &&
    ! git show-ref --verify --quiet refs/heads/feature/x &&
    ! git show-ref --verify --quiet refs/remotes/origin/feature/x &&
    jq -e --arg target "develop at $(git rev-parse --short "$COMPLETE_SQUASH")" \
      '.localBranch == "deleted" and .targetBranch == $target and .remoteBranch == "deleted"' \
      <<<"$output" >/dev/null
}

test_complete_keeps_a_local_branch_with_unmerged_commits() {
  # GIVEN a clone on the merged feature branch, with one more local commit
  complete_clone_fixture
  complete_clone_curl
  echo local >>file && git commit -q -am local

  # WHEN the pull request is completed in the clone
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN feature/x stays, still checked out, and a warning names the reason
  # AND develop moves to the squash commit without a switch
  [[ "$(git symbolic-ref --short HEAD)" == feature/x ]] &&
    [[ "$(git rev-parse develop)" == "$COMPLETE_SQUASH" ]] &&
    assert_contains "$output" 'WARNING: local branch: kept: it holds commits that the PR did not merge'
}

test_complete_keeps_the_branch_when_the_switch_fails() {
  # GIVEN a clone on the merged feature branch, with an uncommitted change that develop would overwrite
  complete_clone_fixture
  complete_clone_curl
  echo change >>file

  # WHEN the pull request is completed in the clone
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN feature/x stays checked out with the change, and the warning gives Git's reason
  # AND develop moves to the squash commit without a switch
  [[ "$(git symbolic-ref --short HEAD)" == feature/x ]] &&
    [[ "$(tail -n 1 file)" == change ]] &&
    [[ "$(git rev-parse develop)" == "$COMPLETE_SQUASH" ]] &&
    assert_contains "$output" 'WARNING: local branch: kept: the switch to develop failed: error: Your local changes'
}

test_complete_keeps_a_local_branch_that_github_keeps() {
  # GIVEN a clone, and a release pull request from develop, the default branch, into master
  complete_clone_fixture
  complete_clone_curl
  git push -q origin develop:master
  COMPLETE_PULL=$(complete_pull_json open false false '"d"' "$(git rev-parse develop)" |
    jq '.head.ref = "develop" | .base.ref = "master"')
  export COMPLETE_PULL

  # WHEN the pull request is completed in the clone
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN GitHub and the clone both keep develop
  git show-ref --verify --quiet refs/heads/develop &&
    jq -e '.remoteBranch == "kept: it is the target or the default branch" and .localBranch == "kept: the branch on GitHub was kept"' \
      <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:')" >/dev/null
}

test_complete_never_forces_the_target_branch() {
  # GIVEN a clone whose develop has a local commit, with HEAD detached or on feature/x
  local row checkout expected
  local -a rows=(
    "detached|not updated: develop has local commits, or another worktree has it checked out"
    "feature/x|not updated: develop has local commits, or local changes block the fast-forward"
  )
  for row in "${rows[@]}"; do
    IFS='|' read -r checkout expected <<<"$row"
    (
      complete_clone_fixture
      complete_clone_curl
      git switch -q develop && echo mine >other && git add other && git commit -q -m mine
      if [[ "$checkout" == detached ]]; then git switch -q --detach feature/x; else git switch -q feature/x; fi
      local before output
      before=$(git rev-parse develop)

      # WHEN the pull request is completed in the clone
      output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || exit 1

      # THEN develop keeps its local commit, and a warning names the reason
      [[ "$(git rev-parse develop)" == "$before" ]] &&
        assert_contains "$output" "WARNING: target branch: ${expected}"
    ) || return 1
  done
}

test_complete_skips_the_local_steps_inside_a_bare_repository() {
  # GIVEN a run inside a bare repository, where Git reports that no work tree exists
  complete_clone_fixture
  complete_clone_curl
  cd "${COMPLETE_TEMP_DIR}/origin.git"

  # WHEN the pull request is completed there
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || return 1

  # THEN the local steps report that they did not run in a clone
  jq -e '.localBranch == "kept: not run in a Git clone"' \
    <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:')" >/dev/null
}

test_complete_ignores_a_look_alike_remote() {
  # GIVEN a clone whose remote is on notgithub.com, not github.com
  complete_clone_fixture
  complete_clone_curl
  git remote set-url origin https://notgithub.com/acme/widgets.git

  # WHEN the pull request is completed in the clone
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || return 1

  # THEN the local steps find no remote, so nothing fetches from that host
  jq -e '.localBranch == "kept: no remote points at acme/widgets"' \
    <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:')" >/dev/null
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

test_complete_reports_the_merge_refusal() {
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
      *'-X PUT'*)
        cat >/dev/null
        printf '%s' '{"status":"failed","details":{"message":"Head branch was modified. Review and try the merge again."}}'
        return 22
        ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # GIVEN a pull request whose head moves before the merge
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run fails with GitHub's reason
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'pull request 42 was not merged.' &&
    assert_contains "$output" 'Head branch was modified'
}

# complete_async_curl <put_body> [put_status]
# Stub the pull request read and a merge request whose PUT answers <put_body>, failing with curl's
# HTTP-error status when <put_status> is given. Polls of request u1 answer the body in
# COMPLETE_POLL_RESULT; every poll is counted in the file COMPLETE_POLLS, and every PUT in COMPLETE_PUTS.
complete_async_curl() {
  COMPLETE_PUT_BODY="$1" COMPLETE_PUT_STATUS="${2:-0}"
  COMPLETE_POLLS=$(mktemp) COMPLETE_PUTS=$(mktemp)
  export COMPLETE_PUT_BODY COMPLETE_PUT_STATUS COMPLETE_POLLS COMPLETE_PUTS COMPLETE_POLL_RESULT
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42/merge-async/u1') echo poll >>"$COMPLETE_POLLS"; printf '%s' "$COMPLETE_POLL_RESULT" ;;
      *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
      *'-X PUT'*'/repos/acme/widgets/pulls/42/merge-async') echo put >>"$COMPLETE_PUTS"; cat >/dev/null; printf '%s' "$COMPLETE_PUT_BODY"; return "$COMPLETE_PUT_STATUS" ;;
      *'-X GET'*'/repos/acme/widgets/pulls?state=open&per_page=100&base=feature%2Fx') printf '[]' ;;
      *) return 90 ;;
    esac
  }
  export -f curl
}

test_complete_refuses_a_pull_request_above_the_bottom_of_its_stack() {
  # GIVEN a pull request that targets feature/parent, inside a stack whose base is develop
  COMPLETE_PULL=$(complete_pull_json open false false '"d"' |
    jq '.base.ref = "feature/parent" | .stack = {base: {ref: "develop", sha: "b"}, size: 3, position: 2, id: 7, number: 1}')
  export COMPLETE_PULL
  complete_async_curl "$(complete_async_merged m1)"

  # WHEN it is completed
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) && return 1

  # THEN the run refuses it, names the pull request to complete first, and sends no merge request
  assert_contains "$output" 'is stacked on feature/parent' &&
    assert_contains "$output" 'Complete that pull request first' &&
    [[ ! -s "$COMPLETE_PUTS" ]] && [[ ! -s "$COMPLETE_POLLS" ]]
}

test_complete_merges_the_bottom_of_a_stack() {
  # GIVEN the bottom pull request of a stack: it targets develop, the stack's base
  complete_origin_fixture
  COMPLETE_PULL=$(complete_pull_json open false false '"d"' |
    jq '.stack = {base: {ref: "develop", sha: "b"}, size: 3, position: 1, id: 7, number: 1}')
  export COMPLETE_PULL
  COMPLETE_POLL_RESULT=$(complete_async_merged m1)
  complete_async_curl "$(complete_async_pending u1)"

  # WHEN it is completed
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the merge request is sent, polled once, and reported as merged
  jq -e '.merged and .commit == "m1"' <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:\|^Waiting for merge request')" >/dev/null &&
    [[ "$(wc -l <"$COMPLETE_POLLS")" -eq 1 ]]
}

test_complete_picks_up_a_pending_merge_request() {
  # GIVEN a pull request whose merge request is already pending, so the PUT answers 409 with its UUID
  complete_origin_fixture
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  COMPLETE_POLL_RESULT=$(complete_async_merged m1)
  complete_async_curl "$(complete_async_pending u1)" 22

  # WHEN it is completed again
  local output
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the pending request is polled to its merged result rather than reported as a failure
  jq -e '.merged and .commit == "m1"' <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:\|^Waiting for merge request')" >/dev/null &&
    assert_contains "$output" 'Waiting for merge request u1'
}

test_complete_reports_a_failed_merge_result() {
  # GIVEN a merge request that GitHub accepts and then fails, such as on a repository rule
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  COMPLETE_POLL_RESULT='{"status":"failed","details":{"message":"Required status check \"build\" is failing."}}'
  complete_async_curl "$(complete_async_pending u1)"

  # WHEN it is completed
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run fails with GitHub's reason, and no branch is deleted
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'pull request 42 was not merged.' &&
    assert_contains "$output" 'Required status check "build" is failing.' &&
    assert_not_contains "$output" '"merged": true'
}

test_complete_reports_an_enqueued_merge_result() {
  # GIVEN a merge request whose result is enqueued: the pull request went to a merge queue
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  COMPLETE_POLL_RESULT='{"status":"enqueued","details":{}}'
  complete_async_curl "$(complete_async_pending u1)"

  # WHEN it is completed
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run fails, names the merge queue, and reports no merge
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'was added to the merge queue instead of merged' &&
    assert_not_contains "$output" '"merged": true'
}

test_complete_keeps_the_pending_hint_when_a_poll_fails() {
  # GIVEN a merge request whose poll answers a gateway error rather than a result
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  complete_async_curl "$(complete_async_pending u1)"
  curl() {
    case "$*" in
      *'-X GET'*'/repos/acme/widgets/pulls/42/merge-async/u1') printf '%s' '<html>502 Bad Gateway</html>'; return 22 ;;
      *'-X GET'*'/repos/acme/widgets/pulls/42') printf '%s' "$COMPLETE_PULL" ;;
      *'-X PUT'*'/repos/acme/widgets/pulls/42/merge-async') cat >/dev/null; printf '%s' "$COMPLETE_PUT_BODY" ;;
      *) return 90 ;;
    esac
  }
  export -f curl

  # WHEN it is completed
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run fails with the pending hint, not as a failed merge, and reports no merge
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'the result of merge request u1 could not be read; GitHub may still merge pull request 42. Run complete again' &&
    assert_not_contains "$output" 'was not merged.' &&
    assert_not_contains "$output" '"merged": true'
}

test_complete_refuses_a_pending_request_with_another_method() {
  # GIVEN a pull request whose pending merge request another client queued as a merge commit
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  COMPLETE_POLL_RESULT=$(complete_async_merged m1)
  complete_async_curl "$(complete_async_pending u1 | jq -c '.details.merge_method = "merge"')" 22

  # WHEN it is completed
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run refuses to adopt that request, names it and its method, and never polls it
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'merge request u1 for pull request 42 is already pending with method merge' &&
    assert_contains "$output" 'Wait for it to finish' &&
    assert_not_contains "$output" '"merged": true' &&
    [[ ! -s "$COMPLETE_POLLS" ]]
}

test_complete_reports_a_top_level_merge_error() {
  # GIVEN a merge request that GitHub refuses with an ordinary error body, as for a 403 or 422
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  complete_async_curl '{"message":"Resource not accessible by integration"}' 22

  # WHEN it is completed
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run fails with GitHub's message
  [[ $status -ne 0 ]] &&
    assert_contains "$output" 'pull request 42 was not merged.' &&
    assert_contains "$output" 'Resource not accessible by integration'
}

test_complete_gives_up_on_a_merge_that_stays_pending() {
  # GIVEN a merge request that is still pending when the polling time is up
  COMPLETE_PULL=$(complete_pull_json open false false '"d"')
  export COMPLETE_PULL
  COMPLETE_POLL_RESULT=$(complete_async_pending u1)
  complete_async_curl "$(complete_async_pending u1)"
  export GH_MERGE_POLL_TIMEOUT=2

  # WHEN it is completed, with the zero interval of the tests counting as a second per poll
  local output status
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" fake-token acme/widgets 42 2>&1)
  status=$?

  # THEN the run polls twice, fails, says how to pick the request up, and reports no merge
  [[ $status -ne 0 ]] &&
    [[ "$(wc -l <"$COMPLETE_POLLS")" -eq 2 ]] &&
    assert_contains "$output" 'merge request u1 for pull request 42 is still pending after 2 seconds' &&
    assert_contains "$output" 'Run complete again' &&
    assert_not_contains "$output" '"merged": true'
}

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
  output=$(bash "${SCRIPT_DIR}/retarget_pr.sh" fake-token acme/widgets 42 refs/heads/feature/parent 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/retarget_pr.sh" fake-token acme/widgets 42 nope 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets show 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets show 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets create 41 42 43 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # THEN the numbers go as integers in the given order and the new stack is printed
  jq -e '.stack == 8 and ([.pullRequests[].id] == [41, 42, 43])' <<<"$output" >/dev/null || return 1

  # AND a single pull request cannot form a stack
  local refusal status
  refusal=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets create 41 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets list 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets show 42 2>&1)
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
  bad_pr=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets create 41 x 2>&1) && return 1
  bad_stack=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets unstack x 2>&1) && return 1
  stray=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets show 41 42 2>&1) && return 1

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
  output=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets add 8 44 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }
  refusal=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets add 8 45 2>&1)
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
  dissolved=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets unstack 8 2>/dev/null) || return 1
  kept=$(bash "${SCRIPT_DIR}/stack_prs.sh" fake-token acme/widgets unstack 9 2>&1) || return 1

  # THEN the 204 is reported as dissolved and the 200 names what stayed and why
  jq -e '. == {stack: 8, dissolved: true}' <<<"$dissolved" >/dev/null &&
    assert_contains "$kept" 'stack 9 still holds #41 (merged); merged and queued pull requests are not unstacked.' &&
    assert_contains "$kept" '"status": "merged"'
}

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

test_run_ignores_an_exported_github_token() {
  local temp_root output
  temp_root=$(pull_requests_run_fixture)
  trap "rm -rf '${temp_root%/skill}'" EXIT

  # GIVEN a private config without GITHUB_TOKEN, and a token that another tool exported
  printf 'ADO_PAT=fake-pat\n' >"${temp_root}/PROJECT_CFG.Local.md"

  # WHEN a GitHub action runs
  output=$(GITHUB_TOKEN=exported-token bash "${temp_root}/scripts/run.sh" list 2>&1) && return 1

  # THEN it fails and names the missing key, rather than using the exported token
  assert_contains "$output" 'GITHUB_TOKEN is not set in PROJECT_CFG.Local.md' &&
    assert_not_contains "$output" 'exported-token'
}

run_test 'list PRs paginates until a short page' test_list_prs_paginates_until_a_short_page
run_test 'list PRs maps merged, closed, and open, and filters by status' test_list_prs_maps_and_filters_status
run_test 'list PRs rejects an unknown status before HTTP' test_list_prs_rejects_unknown_status_before_http
run_test 'list PRs emits no partial output when a page fails' test_list_prs_emits_no_partial_output_when_a_page_fails
run_test 'create preserves Unicode, dash-prefixed descriptions, and ready state' test_create_preserves_unicode_and_ready_state
run_test 'create reports API errors with their detail' test_create_reports_api_error
run_test 'review compares an open PR with its target branch, and reports the merge base, files, threads, comments, and reviewers' test_review_reports_snapshot_files_threads_and_reviews
run_test 'review compares a closed PR with its recorded base' test_review_compares_a_closed_pr_with_its_recorded_base
run_test 'review fails when the PR moves while it reads' test_review_fails_when_the_pr_moves_while_it_reads
run_test 'review fails on GraphQL errors with HTTP 200' test_review_fails_on_graphql_errors_with_http_200
run_test 'fetch encodes the path and the ref' test_fetch_encodes_path_and_ref
run_test 'fetch preserves exact file bytes' test_fetch_preserves_exact_file_bytes
run_test 'fetch propagates HTTP failures' test_fetch_propagates_http_failure
run_test 'comment without a file posts an issue comment' test_comment_general_posts_an_issue_comment
run_test 'comment on a file sends subject_type file' test_comment_on_a_file_sends_file_subject
run_test 'comment on a line sends the line and the right side' test_comment_on_a_line_sends_line_and_right_side
run_test 'comment 422 names its cause and the way out' test_comment_422_names_its_cause_and_way_out
run_test 'comment rejects bad anchor arguments before HTTP' test_comment_rejects_bad_anchor_arguments_before_http
run_test 'reply posts to a review thread' test_reply_posts_to_a_review_thread
run_test 'reply quotes a general comment on its own PR' test_reply_quotes_a_general_comment
run_test 'reply and resolve refuse other targets' test_reply_and_resolve_refuse_other_targets
run_test 'reply refuses a review comment id' test_reply_refuses_a_review_comment_id
run_test 'resolve and unresolve a review thread' test_resolve_and_unresolve_a_review_thread
run_test 'resolve and unresolve a general comment' test_resolve_and_unresolve_a_general_comment
run_test 'edit dispatches on the comment type' test_edit_dispatches_on_comment_type
run_test 'edit fails when the stored text differs' test_edit_fails_when_the_stored_text_differs
run_test 'delete dispatches on the comment type' test_delete_dispatches_on_comment_type
run_test 'edit and delete reject an unknown node type' test_edit_and_delete_reject_unknown_node_type
run_test 'vote maps to review events' test_vote_maps_to_review_events
run_test 'vote rejects bad arguments before HTTP' test_vote_rejects_bad_arguments_before_http
run_test 'vote reports the own-PR refusal' test_vote_reports_own_pr_refusal
run_test 'issue shows the issue and its comments' test_issue_shows_the_issue_and_its_comments
run_test 'issue refuses a pull request number' test_issue_refuses_a_pull_request_number
run_test 'publish marks a draft ready' test_publish_marks_a_draft_ready
run_test 'publish skips GraphQL for a ready PR' test_publish_skips_graphql_for_a_ready_pr
run_test 'complete squash-merges with the description as the commit message' test_complete_squash_merges_with_the_description
run_test 'complete refuses a merged, closed, draft, or undescribed PR before merging' test_complete_refuses_before_merge
run_test 'complete reports a merge with a long description' test_complete_reports_a_merge_with_a_long_description
run_test 'complete reports each remote branch outcome, and keeps a moved branch and a base of open PRs' test_complete_reports_each_remote_branch_outcome
run_test 'complete deletes the local branch and fast-forwards develop' test_complete_cleans_up_the_local_clone
run_test 'complete keeps a local branch with unmerged commits' test_complete_keeps_a_local_branch_with_unmerged_commits
run_test 'complete keeps the branch when the switch to develop fails' test_complete_keeps_the_branch_when_the_switch_fails
run_test 'complete keeps a local branch that GitHub keeps' test_complete_keeps_a_local_branch_that_github_keeps
run_test 'complete never forces the target branch' test_complete_never_forces_the_target_branch
run_test 'complete skips the local steps inside a bare repository' test_complete_skips_the_local_steps_inside_a_bare_repository
run_test 'complete ignores a look-alike remote' test_complete_ignores_a_look_alike_remote
run_test 'gh_git offers the token to github.com over HTTPS only' test_gh_git_offers_the_token_to_github_only
run_test 'complete reports the merge refusal' test_complete_reports_the_merge_refusal
run_test 'complete refuses a pull request above the bottom of its stack' test_complete_refuses_a_pull_request_above_the_bottom_of_its_stack
run_test 'complete merges the bottom of a stack through the asynchronous merge API' test_complete_merges_the_bottom_of_a_stack
run_test 'complete picks up a pending merge request' test_complete_picks_up_a_pending_merge_request
run_test 'complete reports a failed merge result' test_complete_reports_a_failed_merge_result
run_test 'complete gives up on a merge that stays pending' test_complete_gives_up_on_a_merge_that_stays_pending
run_test 'complete reports an enqueued merge result' test_complete_reports_an_enqueued_merge_result
run_test 'complete keeps the pending hint when a poll fails' test_complete_keeps_the_pending_hint_when_a_poll_fails
run_test 'complete refuses a pending request with another method' test_complete_refuses_a_pending_request_with_another_method
run_test 'complete reports a top-level merge error' test_complete_reports_a_top_level_merge_error
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
run_test 'run.sh dispatches fetch, vote, resolve, complete, retarget, and stack' test_run_dispatches_fetch_vote_resolve_complete_retarget_and_stack
run_test 'run.sh ignores an exported GITHUB_TOKEN' test_run_ignores_an_exported_github_token
