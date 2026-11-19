#!/usr/bin/env bash
# Cases for post_comment.sh, reply_comment.sh, resolve_thread.sh, edit_comment.sh, and
# delete_comment.sh.

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
