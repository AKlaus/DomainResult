#!/usr/bin/env bash
# complete_pr.sh — Squash-merge a GitHub pull request, with its description as the commit message
# Usage: bash complete_pr.sh <OWNER/REPO> <PR>
#
# The commit subject is "<PR title> (#<PR>)", the form GitHub gives a squash commit by default. The
# commit message is the PR description, copied as it stands when the PR is read. The merge names
# the head commit that was read, so a push after the read makes GitHub cancel the merge.
#
# The merge goes through GitHub's asynchronous merge API: the request is accepted at once and
# runs in the background, and the script polls its result until it is merged or failed. This is
# the only merge API that accepts a pull request in a stack. A stack merges from the bottom, and a
# request for a pull request higher up would merge every open pull request below it as well, so
# the script refuses a pull request whose target branch is not the stack's base.
#
# GH_MERGE_POLL_INTERVAL (seconds between polls, default 5) and GH_MERGE_POLL_TIMEOUT (seconds
# before the script gives up, default 600) exist for the tests; a run that gives up names the way
# to pick the pending request up again.
#
# After the merge, the script deletes the PR's branch on GitHub and in the local clone, and
# fast-forwards the local target branch. Each of these steps reports its outcome and never fails
# the run: the merge has happened, so a failed exit would invite a retry of a merged PR.

set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

REPOSITORY="${1:?Missing GITHUB_REPOSITORY}"
PR="${2:?Missing PR}"

gh_require_pr_number "$PR"
gh_init "$REPOSITORY"
# The fields this script reads from the pull request are the same in both API versions.
GH_API_VERSION="$GH_STACK_API_VERSION"

PULL=$(gh_get "${REPO_URL}/pulls/${PR}" "pull request ${PR} was not found.")

# GitHub refuses these cases too, but with a generic 400. Each check here names the way out.
if printf '%s' "$PULL" | jq -e '.merged == true' >/dev/null; then
  echo "ERROR: pull request ${PR} is already merged." >&2
  exit 1
fi
if printf '%s' "$PULL" | jq -e '.state != "open"' >/dev/null; then
  echo "ERROR: pull request ${PR} is closed. Reopen it before you complete it." >&2
  exit 1
fi
if printf '%s' "$PULL" | jq -e '.draft == true' >/dev/null; then
  echo "ERROR: pull request ${PR} is a draft. Run publish first." >&2
  exit 1
fi
# The squash commit copies the description, so an empty one would leave a commit without a body.
if printf '%s' "$PULL" | jq -e '(.body // "") | test("\\S") | not' >/dev/null; then
  echo "ERROR: pull request ${PR} has no description, and the squash commit copies it. Run describe first." >&2
  exit 1
fi
# A stacked pull request merges together with every open pull request below it. `stack.base` is
# the branch the whole stack targets, so a pull request whose own target differs sits above an
# open one. Only the bottom of a stack, or a pull request outside any stack, merges alone.
if printf '%s' "$PULL" | jq -e '.stack and .stack.base.ref != .base.ref' >/dev/null; then
  echo "ERROR: pull request ${PR} is stacked on $(printf '%s' "$PULL" | jq -r '.base.ref'), the branch of an open pull request. Complete that pull request first." >&2
  exit 1
fi

BODY=$(printf '%s' "$PULL" | jq --arg pr "$PR" '{
  merge_method: "squash",
  merge_action: "direct_merge",
  commit_title: "\(.title) (#\($pr))",
  commit_message: .body,
  sha: .head.sha
}')

# merge_error <response>
# Print the part of a merge response that fail_with_body reads: a result carries its message in
# `details`, while an ordinary API error carries it at the top level, as does a non-JSON body.
merge_error() {
  printf '%s' "$1" | jq -c '.details // .' 2>/dev/null || printf '%s' "$1"
}

# 202: accepted, with the UUID to poll. 200: already merged, or already queued. 409: another
# request for this pull request is pending, and the body carries that request's UUID, so the
# script polls it rather than failing. 400: the pull request is not ready, such as a closed one.
# Branch protection and repository rules run only when the merge runs, so a rule failure arrives
# as a failed result while polling, not as a refusal here.
RESPONSE=$(gh_api PUT "${REPO_URL}/pulls/${PR}/merge-async" "$BODY") \
  || printf '%s' "$RESPONSE" | jq -e '.status == "pending" and .details.uuid' >/dev/null \
  || fail_with_body "$(merge_error "$RESPONSE")" "pull request ${PR} was not merged."
require_json_field "$RESPONSE" '.status and .details' "pull request ${PR} was not merged."
# A pending request that another client queued may merge another way; polling it would report a
# squash this run did not make, and sending another request cannot replace it.
if printf '%s' "$RESPONSE" | jq -e '.status == "pending"
     and (.details.merge_method != "squash" or .details.merge_action != "direct_merge")' >/dev/null; then
  echo "ERROR: merge request $(printf '%s' "$RESPONSE" | jq -r '.details.uuid') for pull request ${PR} is already pending with method $(printf '%s' "$RESPONSE" | jq -r '.details.merge_method'); GitHub may still merge it that way. Wait for it to finish before you run complete again." >&2
  exit 1
fi

# wait_for_merge <uuid>
# Poll the result of the asynchronous merge request until it leaves the pending state, and print
# the final response. A run that gives up fails and leaves the request pending: GitHub keeps its
# result for 24 hours, and a rerun of complete finds the pending request through the 409 above.
# If GitHub merged it in the meantime, the rerun stops at the merged check above and runs no
# cleanup, so the message says that the branch is then the caller's to delete.
wait_for_merge() {
  local uuid="$1" waited=0 interval="${GH_MERGE_POLL_INTERVAL:-5}" limit="${GH_MERGE_POLL_TIMEOUT:-600}" result
  echo "Waiting for merge request ${uuid} to merge pull request ${PR}." >&2
  while :; do
    sleep "$interval"
    # A zero interval, as the tests use, still counts against the limit, so a pending request
    # cannot loop forever.
    waited=$((waited + (interval > 0 ? interval : 1)))
    result=$(gh_get "${REPO_URL}/pulls/${PR}/merge-async/${uuid}" \
      "the result of merge request ${uuid} could not be read; GitHub may still merge pull request ${PR}. Run complete again to pick the request up.") || exit 1
    if ! printf '%s' "$result" | jq -e '.status == "pending"' >/dev/null; then
      printf '%s' "$result"
      return
    fi
    if (( waited >= limit )); then
      echo "ERROR: merge request ${uuid} for pull request ${PR} is still pending after ${limit} seconds; GitHub may still merge it. Run complete again: a pending request is picked up, and an already merged pull request is reported, with its branch left for you to delete." >&2
      exit 1
    fi
  done
}

# wait_for_merge runs in a subshell, so its exit reaches this line as a failed assignment.
if printf '%s' "$RESPONSE" | jq -e '.status == "pending"' >/dev/null; then
  RESPONSE=$(wait_for_merge "$(printf '%s' "$RESPONSE" | jq -r '.details.uuid')") || exit 1
fi
case "$(printf '%s' "$RESPONSE" | jq -r '.status')" in
  merged) ;;
  enqueued)
    echo "ERROR: pull request ${PR} was added to the merge queue instead of merged. Check the queue; complete reports the merge once it has happened." >&2
    exit 1
    ;;
  *) fail_with_body "$(printf '%s' "$RESPONSE" | jq '.details')" "pull request ${PR} was not merged." ;;
esac
MERGE_SHA=$(printf '%s' "$RESPONSE" | jq -r '.details.sha')

# The PR JSON holds the description, so it reaches jq on stdin. As an argument, a long description
# exceeds the argument limit, and the report would fail after GitHub has merged.
SUBJECT=$(printf '%s' "$BODY" | jq -r '.commit_title')
URL=$(printf '%s' "$PULL" | jq -r '.html_url')
HEAD_REF=$(printf '%s' "$PULL" | jq -r '.head.ref')
HEAD_SHA=$(printf '%s' "$PULL" | jq -r '.head.sha')
TARGET=$(printf '%s' "$PULL" | jq -r '.base.ref')
SAME_REPO=$(printf '%s' "$PULL" | jq -r '(.head.repo.full_name // "") == .base.repo.full_name')
DEFAULT_BRANCH=$(printf '%s' "$PULL" | jq -r '.base.repo.default_branch // ""')

# delete_remote_branch
# Delete the head branch on GitHub and print the outcome. A branch that GitHub already deleted,
# such as by the repository's auto-delete setting, counts as deleted. A branch that open PRs
# target stays: such stacked PRs may be closed, not retargeted, when their base is deleted.
#
# The deletion is a push with a lease on the merged head, so GitHub deletes the branch only while
# it still points there. A push after the merge keeps the branch and its unmerged commits. The
# push runs from an empty scratch repository against the canonical URL, so it needs no clone.
delete_remote_branch() {
  local dependants scratch error url="https://github.com/${GH_OWNER}/${GH_REPO}.git"
  if [[ "$SAME_REPO" != true ]]; then
    echo "kept: the branch is in another repository"
    return
  fi
  if [[ "$HEAD_REF" == "$TARGET" || "$HEAD_REF" == "$DEFAULT_BRANCH" ]]; then
    echo "kept: it is the target or the default branch"
    return
  fi
  dependants=$(gh_api GET "${REPO_URL}/pulls?state=open&per_page=100&base=$(jq -rn --arg s "$HEAD_REF" '$s | @uri')") \
    || { echo "kept: the open pull requests could not be listed"; return; }
  if printf '%s' "$dependants" | jq -e 'length > 0' >/dev/null 2>&1; then
    echo "kept: open pull requests target it: $(printf '%s' "$dependants" | jq -r 'map("#\(.number)") | join(", ")')"
    return
  fi
  scratch=$(mktemp -d)
  git init -q --bare "$scratch"
  if error=$(gh_git --git-dir="$scratch" push --quiet \
      --force-with-lease="refs/heads/${HEAD_REF}:${HEAD_SHA}" "$url" ":refs/heads/${HEAD_REF}" 2>&1); then
    echo "deleted"
  # Git rejects the lease as "stale info" both when the branch moved and when it is gone already.
  elif [[ "$error" == *"stale info"* ]]; then
    if [[ -z "$(gh_git --git-dir="$scratch" ls-remote "$url" "refs/heads/${HEAD_REF}" 2>/dev/null)" ]] \
      && gh_git --git-dir="$scratch" ls-remote --exit-code "$url" HEAD >/dev/null 2>&1; then
      echo "already deleted"
    else
      echo "kept: it moved after the merge"
    fi
  else
    echo "kept: the deletion failed: $(printf '%s' "$error" | grep -v '^To ' | head -n 1)"
  fi
  rm -rf "$scratch"
}

# git_remote_for_repository
# Print the name of the local remote whose URL points at the configured repository on github.com,
# over HTTPS or SSH. The whole URL must match, so a look-alike host such as notgithub.com does not.
# The URL is read as configured: `git remote get-url` applies url.insteadOf rewrites.
git_remote_for_repository() {
  local name url
  local pattern="^(https://([^/@]+@)?github\.com/|ssh://git@github\.com/|git@github\.com:)${GH_OWNER//./\\.}/${GH_REPO//./\\.}(\.git)?/?$"
  shopt -s nocasematch
  while read -r name; do
    url=$(git config --get "remote.${name}.url" 2>/dev/null) || continue
    if [[ "$url" =~ $pattern ]]; then
      shopt -u nocasematch
      printf '%s' "$name"
      return 0
    fi
  done < <(git remote)
  shopt -u nocasematch
  return 1
}

# clean_up_local_clone <remote outcome>
# Delete the local head branch and fast-forward the local target branch, in the clone the script
# runs in. Sets LOCAL_BRANCH_OUTCOME and TARGET_BRANCH_OUTCOME to the outcomes.
clean_up_local_clone() {
  local remote current error
  # Inside a .git directory or a bare repository, the command prints false and still exits 0.
  if [[ "$(git rev-parse --is-inside-work-tree 2>/dev/null)" != true ]]; then
    LOCAL_BRANCH_OUTCOME="kept: not run in a Git clone"
    TARGET_BRANCH_OUTCOME="not updated: not run in a Git clone"
    return
  fi
  if ! remote=$(git_remote_for_repository); then
    LOCAL_BRANCH_OUTCOME="kept: no remote points at ${GH_OWNER}/${GH_REPO}"
    TARGET_BRANCH_OUTCOME="not updated: no remote points at ${GH_OWNER}/${GH_REPO}"
    return
  fi
  # The PR ref outlives the branch, so the merged head is known locally even after the deletion.
  if ! gh_git fetch --quiet "$remote" "+refs/heads/${TARGET}:refs/remotes/${remote}/${TARGET}" "refs/pull/${PR}/head" 2>/dev/null; then
    LOCAL_BRANCH_OUTCOME="kept: the fetch from ${remote} failed"
    TARGET_BRANCH_OUTCOME="not updated: the fetch from ${remote} failed"
    return
  fi
  if [[ "$1" == deleted || "$1" == "already deleted" ]]; then
    git update-ref -d "refs/remotes/${remote}/${HEAD_REF}" 2>/dev/null || true
  fi

  current=$(git symbolic-ref --quiet --short HEAD 2>/dev/null || true)
  # The remote outcome decides for both copies, so a branch that GitHub keeps, such as the
  # default branch or a base of stacked PRs, stays in the clone too.
  if [[ "$1" != deleted && "$1" != "already deleted" ]]; then
    LOCAL_BRANCH_OUTCOME="kept: the branch on GitHub was kept"
  elif ! git show-ref --verify --quiet "refs/heads/${HEAD_REF}"; then
    LOCAL_BRANCH_OUTCOME="absent"
  # A squash merge leaves the branch looking unmerged, so `branch -d` would refuse it. The branch
  # is deleted only when the merged head contains it, so no local commit is lost.
  elif ! git merge-base --is-ancestor "refs/heads/${HEAD_REF}" "$HEAD_SHA" 2>/dev/null; then
    LOCAL_BRANCH_OUTCOME="kept: it holds commits that the PR did not merge"
  else
    if [[ "$current" == "$HEAD_REF" ]]; then
      if error=$(git switch --quiet "$TARGET" 2>&1); then
        current="$TARGET"
      else
        LOCAL_BRANCH_OUTCOME="kept: the switch to ${TARGET} failed: $(printf '%s' "$error" | head -n 1)"
      fi
    fi
    if [[ "$current" != "$HEAD_REF" ]]; then
      if git branch --quiet -D "$HEAD_REF" 2>/dev/null; then
        LOCAL_BRANCH_OUTCOME="deleted"
      else
        LOCAL_BRANCH_OUTCOME="kept: Git refused the deletion; it may be checked out in another worktree"
      fi
    fi
  fi

  # Fast-forward only: a target branch with local commits is left for the developer.
  if [[ "$current" == "$TARGET" ]]; then
    if git merge --quiet --ff-only "refs/remotes/${remote}/${TARGET}" >/dev/null 2>&1; then
      TARGET_BRANCH_OUTCOME="${TARGET} at $(git rev-parse --short HEAD)"
    else
      TARGET_BRANCH_OUTCOME="not updated: ${TARGET} has local commits, or local changes block the fast-forward"
    fi
  elif git show-ref --verify --quiet "refs/heads/${TARGET}"; then
    # A fetch from the clone itself refuses a non-fast-forward and a branch checked out elsewhere.
    if git fetch --quiet . "refs/remotes/${remote}/${TARGET}:refs/heads/${TARGET}" 2>/dev/null; then
      TARGET_BRANCH_OUTCOME="${TARGET} at $(git rev-parse --short "refs/heads/${TARGET}")"
    else
      TARGET_BRANCH_OUTCOME="not updated: ${TARGET} has local commits, or another worktree has it checked out"
    fi
  else
    TARGET_BRANCH_OUTCOME="absent"
  fi
}

REMOTE_BRANCH_OUTCOME=$(delete_remote_branch)
clean_up_local_clone "$REMOTE_BRANCH_OUTCOME"
for outcome in "remote branch: ${REMOTE_BRANCH_OUTCOME}" "local branch: ${LOCAL_BRANCH_OUTCOME}" "target branch: ${TARGET_BRANCH_OUTCOME}"; do
  case "$outcome" in
    *": kept: "* | *": not updated: "*) printf 'WARNING: %s\n' "$outcome" >&2 ;;
  esac
done

jq -n --argjson id "$PR" --arg commit "$MERGE_SHA" --arg subject "$SUBJECT" --arg url "$URL" \
  --arg remote "$REMOTE_BRANCH_OUTCOME" --arg local "$LOCAL_BRANCH_OUTCOME" --arg target "$TARGET_BRANCH_OUTCOME" \
  '{id: $id, merged: true, method: "squash", commit: $commit, subject: $subject, url: $url,
    remoteBranch: $remote, localBranch: $local, targetBranch: $target}'
