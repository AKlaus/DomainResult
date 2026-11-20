#!/usr/bin/env bash
# Cases for complete_pr.sh: the merge, and the cleanup of the branch and the local clone.

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
  cd "$COMPLETE_TEMP_DIR" || exit 1
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
  COMPLETE_PULL=$(complete_pull_json open false false '"### Why\n\nA reason — with an em dash.\n\nCloses #88"')
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
          commit_message: "### Why\n\nA reason — with an em dash.\n\nCloses #88",
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
    output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) && return 1
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
      output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || exit 1
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
      output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || exit 1

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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || return 1

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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || return 1

  # THEN the local steps find no remote, so nothing fetches from that host
  jq -e '.localBranch == "kept: no remote points at acme/widgets"' \
    <<<"$(printf '%s\n' "$output" | grep -v '^WARNING:')" >/dev/null
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) && return 1

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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1) || {
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
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
  output=$(bash "${SCRIPT_DIR}/complete_pr.sh" acme/widgets 42 2>&1)
  status=$?

  # THEN the run polls twice, fails, says how to pick the request up, and reports no merge
  [[ $status -ne 0 ]] &&
    [[ "$(wc -l <"$COMPLETE_POLLS")" -eq 2 ]] &&
    assert_contains "$output" 'merge request u1 for pull request 42 is still pending after 2 seconds' &&
    assert_contains "$output" 'Run complete again' &&
    assert_not_contains "$output" '"merged": true'
}

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
