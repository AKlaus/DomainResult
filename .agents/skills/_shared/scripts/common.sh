#!/usr/bin/env bash
# common.sh — error and body-text helpers shared by the skill scripts.
#
# This file is *sourced* through a skill's lib.sh, never executed directly. It inherits the
# caller's shell options and defines only functions.

# fail_with_body <response> <message>
# Print an error and exit 1. From a JSON error object it prints `.message` and every entry of
# `.errors[]`: a REST error such as 404 puts the whole cause in `.message`, a 422 answers
# "Validation Failed" there and the cause, such as "line must be part of the diff", in `.errors`,
# and GraphQL reports only `.errors`. Any other body is printed raw. Piping jq straight to stderr
# would abort on a non-JSON body (HTML sign-in or proxy page) before `exit 1`, losing the detail
# and returning jq's status instead of 1.
fail_with_body() {
  local response="$1" message="$2" detail
  echo "ERROR: ${message}" >&2
  if [[ -n "$response" ]]; then
    detail=$(printf '%s' "$response" | jq -r '
      if type == "object" then
        . as $body
        | [.message, (.errors[]? | if type == "object" then (.message // tojson) else tostring end)]
        | map(select(. != null and . != ""))
        | if length > 0 then join("\n") else ($body | tojson) end
      else . end' 2>/dev/null) || detail="$response"
    printf '%s\n' "$detail" >&2
  fi
  exit 1
}

# require_json_field <response> <jq-filter> <message>
# Assert the response satisfies <jq-filter> (e.g. '.id', '.pullRequestId', 'has("vote")');
# otherwise fail via fail_with_body. Guards against an API error rendering as a null result
# and exiting 0, so a call that never took effect cannot read as success.
require_json_field() {
  local response="$1" filter="$2" message="$3"
  printf '%s' "$response" | jq -e "$filter" >/dev/null 2>&1 || fail_with_body "$response" "$message"
}

# warn_unexpanded_quoting <label> <text>
# Warn on stderr when <text> carries the fingerprints of quoting the caller's shell did not
# expand, and return 0 regardless: this reports, it never repairs. A body that genuinely quotes
# an escape sequence as its subject — a review comment about `\n`, say — is legitimate, so
# rewriting the text would corrupt the honest case to rescue the careless one.
#
# Both fingerprints come from ANSI-C quoting, `$'line one\nline two'`, reaching a shell that has
# no such syntax. PowerShell is the one that matters: it is the only shell some callers have, it
# passes `$'...'` through as literal characters, and the result posts successfully and reads
# plausibly. See .agents/skills/_shared/references/text.md for the per-shell forms that do work.
warn_unexpanded_quoting() {
  local label="$1" text="$2"

  # A leading dollar sign, with or without the apostrophe. PowerShell strips the apostrophes of
  # $'...' while building the native command line, so bash receives a bare `$` and the narrower
  # $'-only test would never fire for the caller most likely to make this mistake. Matching the
  # bare `$` costs a false positive on prose that opens with a price or a variable name; that is
  # the cheaper error for a warning that neither blocks nor rewrites.
  if [[ "$text" == \$\'* ]]; then
    printf "WARNING: %s starts with a literal \$' — ANSI-C quoting reached a shell that does not expand it.\n" "$label" >&2
    printf '         The text will post exactly as written, dollar sign and all. See .agents/skills/_shared/references/text.md.\n' >&2
  elif [[ "$text" == \$* ]]; then
    printf 'WARNING: %s starts with a literal $ — this is how unexpanded ANSI-C quoting arrives from PowerShell.\n' "$label" >&2
    printf '         Ignore this if the text genuinely opens with a dollar sign. See .agents/skills/_shared/references/text.md.\n' >&2
  fi

  # Two characters, backslash then n, rather than a newline. Checked separately: a body can carry
  # this without the $' prefix, and each fingerprint names a different half of the same mistake.
  if [[ "$text" == *'\n'* ]]; then
    printf 'WARNING: %s contains a literal backslash-n rather than a newline.\n' "$label" >&2
    printf '         The service renders it as visible text. Ignore this if the backslash belongs to the\n' >&2
    printf '         content itself — a path like C:\\nightly, an escape sequence you are quoting.\n' >&2
  fi
}

# names_unlinked_pr <text>
# Succeed when <text> names a pull request as "PR 12", "PR #12" or "pull request 12", the forms
# that GitHub does not link. A hand-built "[PR 12](url)" link matches too. A bare
# number needs two digits, so "PR 2 of the stack" stays silent; the # form always matches. Each
# skill's warn_unlinked_pr_reference prints the advice for where its text lands.
names_unlinked_pr() {
  local pattern='(^|[^[:alnum:]])(PR|[Pp]ull [Rr]equest) (#[0-9]+|[0-9]{2,})([^[:alnum:]]|$)'
  [[ "$1" =~ $pattern ]]
}
