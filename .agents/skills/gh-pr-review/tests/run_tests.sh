#!/usr/bin/env bash
# Regression suite for the gh-pr-review scripts: the counters, the assert helpers, the fixture
# helpers and run_test, then every case file in tests/cases/. The API cases replace curl with an
# exported Bash function; the scope cases build a throwaway Git repository. No case reads a real
# token, contacts a remote service, or mutates any remote state.

set -uo pipefail

SKILL_TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# The number of workers that post body text, which test_every_prose_script_calls_its_guard must
# find. It guards the derivation in that test against selecting nothing.
BODY_SCRIPT_FLOOR=5

# Worker scripts normally inherit this from run.sh. Set it here because the tests invoke
# workers directly; otherwise Git Bash rewrites POSIX-looking jq and curl arguments.
export MSYS2_ARG_CONV_EXCL='*'

SCRIPT_DIR="$(cd "${SKILL_TEST_DIR}/../scripts" && pwd)"
CASE_DIRS=("${SKILL_TEST_DIR}/cases")

PASSED=0
FAILED=0

# Every exit path but the tally check below is a failure: a case file that calls `exit` at the top
# level takes this whole shell with it, skipping the printf and the counts, and `exit 0` would then
# read as a clean run of a suite that stopped halfway.
TALLY_REACHED=false
trap 'if [[ "$TALLY_REACHED" != true ]]; then
        printf "ERROR: the suite exited before it reported a tally.\n" >&2
        exit 1
      fi' EXIT

assert_contains() {
  local actual="$1" expected="$2"
  [[ "$actual" == *"$expected"* ]] || {
    printf 'Expected output to contain: %s\nActual output:\n%s\n' "$expected" "$actual" >&2
    return 1
  }
}

assert_not_contains() {
  local actual="$1" unexpected="$2"
  [[ "$actual" != *"$unexpected"* ]] || {
    printf 'Expected output not to contain: %s\nActual output:\n%s\n' "$unexpected" "$actual" >&2
    return 1
  }
}

# make_skill_fixture
# Print the root of a throwaway skill folder, <tmp>/skill, whose scripts/ folder holds config.sh, so
# a copied run.sh finds it beside itself as in the repository. The caller removes the parent of the
# printed path.
make_skill_fixture() {
  local base
  base=$(mktemp -d)
  mkdir -p "${base}/skill/scripts"
  cp "${SCRIPT_DIR}/config.sh" "${base}/skill/scripts/config.sh"
  printf '%s' "${base}/skill"
}

# write_git_dir_private_config <skill_root> <git_common_subdir> <line>
# Make the parent of a make_skill_fixture folder a Git repository and write <line> to
# <git-dir>/<git_common_subdir>/PROJECT_CFG.Local.md. A copied run.sh then finds its token only
# through the subfolder it names, so a dispatcher case fails when run.sh names the wrong one.
write_git_dir_private_config() {
  local repo_root="${1%/skill}" git_root="${1%/skill}"
  if command -v cygpath >/dev/null 2>&1; then
    git_root=$(cygpath -m "$repo_root")
  fi
  command git -C "$git_root" init -q
  mkdir -p "${repo_root}/.git/$2"
  printf '%s\n' "$3" >"${repo_root}/.git/$2/PROJECT_CFG.Local.md"
}

run_test() {
  local name="$1"
  shift

  # Deliberately not `if ("$@")`, nor `("$@") || status=$?`: bash suppresses errexit inside a
  # condition context, and the suppression reaches into the subshell. A test that relies on
  # `set -e` between bare assertions would then only ever fail on its last command.
  local status
  ("$@")
  status=$?

  if (( status == 0 )); then
    printf 'ok - %s\n' "$name"
    ((PASSED += 1))
  else
    printf 'not ok - %s\n' "$name" >&2
    ((FAILED += 1))
  fi
}

# Cases live in the cases/*.sh of each CASE_DIRS entry, one file per area, each ending with the run_test calls for the cases
# above it. Every case stubs curl itself, and run_test runs each case body in a subshell, so a stub
# or a shell option cannot reach the next case and the glob order of the files carries no meaning.
# Top-level code in a case file is the exception: it runs in this process, so a fixture exported
# there is visible to every file sourced after it. Keep those names prefixed by their area.
# Adding an area is a new file and nothing else.
# Counted from the files rather than pinned to a number here: a floor would have to be raised with
# every case added, and the one nobody raises is the one that stops guarding. This catches a case
# list that stopped short of its own end. A whole file removed from cases/ still reads as a clean
# run; that one is a diff to review, not a number to check.
EXPECTED=0
for case_dir in "${CASE_DIRS[@]}"; do
  for case_file in "$case_dir"/*.sh; do
    EXPECTED=$(( EXPECTED + $(grep -c '^run_test ' "$case_file") ))
  done
done

for case_dir in "${CASE_DIRS[@]}"; do
  for case_file in "$case_dir"/*.sh; do
    # Parsed before it is sourced. A syntax error mid-file stops the source where it stands, so the
    # run_test calls below it never run and the suite reports a clean pass of the cases that did load.
    bash -n "$case_file" || exit 1
    source "$case_file"
  done
done

printf '\n%d passed, %d failed\n' "$PASSED" "$FAILED"
TALLY_REACHED=true

# A cases/ directory that moved or emptied would otherwise report a clean run of nothing and exit 0.
if (( PASSED + FAILED == 0 )); then
  printf 'ERROR: no cases were sourced from %s.\n' "${CASE_DIRS[*]}" >&2
  exit 1
fi

if (( PASSED + FAILED != EXPECTED )); then
  printf 'ERROR: %d cases ran, %d are declared in %s; a case list did not run as written.\n' \
    "$((PASSED + FAILED))" "$EXPECTED" "${CASE_DIRS[*]}" >&2
  exit 1
fi

((FAILED == 0))
