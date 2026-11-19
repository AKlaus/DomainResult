#!/usr/bin/env bash
# pr_scope.sh — Check a change against the PR budget and report its tangible changes.
# Usage: bash pr_scope.sh [BASE] [HEAD]
#
# BASE defaults to origin/master and HEAD to HEAD. The report covers the diff from the merge base
# of the two to HEAD, which is the diff a GitHub PR shows. It reads committed Git objects only:
# uncommitted edits do not count, and nothing contacts GitHub, so no token is needed.
# The output is Markdown for the PR description. It counts what changed, such as endpoints, and
# never prints line counts. The budget result goes to stderr: the author and the reviewer act on
# it, but the description does not show it. references/scope.md explains both.

set -euo pipefail

# Byte semantics for awk, sort, and tr. In a UTF-8 locale, macOS awk stops on a byte sequence it
# cannot decode, such as a diff line that cuts through a byte-order mark. The scripts match ASCII
# syntax only, so the C locale changes no result.
export LC_ALL=C

# The budget, in changed lines (added plus deleted). AGENTS.md states the same numbers: change both
# places together.
PRODUCTION_TARGET=500
PRODUCTION_CEILING=1000
REVIEWABLE_CEILING=2000

# Each detail list shows at most this many rows. This shortens a long list, but it does not bound
# the whole report: a row can hold several names.
MAX_ROWS=15

SCOPE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if (( $# > 2 )); then
  echo "ERROR: scope takes at most 2 arguments, but received $#." >&2
  echo "       Usage: bash .agents/skills/gh-pr-review/scripts/run.sh scope [base] [head]" >&2
  exit 1
fi
BASE="${1:-origin/master}"
HEAD_REV="${2:-HEAD}"

for rev in "$BASE" "$HEAD_REV"; do
  if ! git rev-parse --verify --quiet "${rev}^{commit}" >/dev/null; then
    echo "ERROR: '${rev}' is not a commit in this clone. Fetch it first, for example: git fetch origin" >&2
    exit 1
  fi
done
MERGE_BASE=$(git merge-base "$BASE" "$HEAD_REV") || {
  echo "ERROR: '${BASE}' and '${HEAD_REV}' share no commit. Fetch more history, for example: git fetch --unshallow origin" >&2
  exit 1
}

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

# ── Budget ───────────────────────────────────────────────────────────────────────────────────────
# -M counts a moved file by its edits only, as the GitHub diff shows a rename. classify.awk
# reads the new path out of the rename notation.
git -c core.quotepath=off diff --numstat -M "$MERGE_BASE" "$HEAD_REV" \
  | awk -f "${SCOPE_DIR}/classify.awk" >"${WORK}/classified"

# Comment lines and renames do not count. uncounted.awk needs whole files as context to follow
# block comments, and its paths must match the numstat paths, so the prefixes are fixed.
git -c core.quotepath=off diff -M -U1000000 --no-color --no-ext-diff --no-textconv \
  --src-prefix=a/ --dst-prefix=b/ "$MERGE_BASE" "$HEAD_REV" \
  | awk -f "${SCOPE_DIR}/uncounted.awk" >"${WORK}/uncounted"

lines() { # category → its changed lines, less the lines that do not count
  awk -F'\t' -v c="$1" '
    FILENAME == ARGV[1] { uncounted[$2] = $1; next }
    $1 == c { sum += $2 + $3 - uncounted[$4] }
    END { print sum + 0 }' "${WORK}/uncounted" "${WORK}/classified"
}

# Auxiliary files, docs, and binary files do not count.
PRODUCTION_LINES=$(lines production)
REVIEWABLE_LINES=$(( PRODUCTION_LINES + $(lines test) ))

# ── Metrics ──────────────────────────────────────────────────────────────────────────────────────
# One row per tangible change: kind, then added, changed, or removed. The table below counts them.
git -c core.quotepath=off diff -M --name-status "$MERGE_BASE" "$HEAD_REV" >"${WORK}/status"
: >"${WORK}/metrics"

# The top-level folders that hold changed production or test files.
AREAS=$(awk -F'\t' '
  ($1 == "production" || $1 == "test") && index($4, "/") > 0 { print substr($4, 1, index($4, "/") - 1) }' \
  "${WORK}/classified" | sort -u | awk '{ printf "%s`%s`", (NR > 1 ? ", " : ""), $0 }')

# ── Inventory ────────────────────────────────────────────────────────────────────────────────────
# Each row: category, status, first path, second path (the new path of a rename). A renamed file
# takes the category of its new path.
awk -F'\t' '
  FILENAME == ARGV[1] { category[$4] = $1; next }
  { path = ($3 != "") ? $3 : $2; print category[path] "\t" $0 }' \
  "${WORK}/classified" "${WORK}/status" >"${WORK}/inventory"

# The current file at one end of the diff: nothing when it does not exist there. Reads the loop's
# old and new paths.
content_at() { # base|head
  local revision path
  if [[ "$1" == base ]]; then revision="$MERGE_BASE"; path="$old"; else revision="$HEAD_REV"; path="$new"; fi
  [[ -n "$path" ]] || return 0
  git show "${revision}:${path}" 2>/dev/null || true
}

change_label() {
  case "$1" in
    A) echo added ;;
    D) echo removed ;;
    R*) echo renamed ;;
    *) echo modified ;;
  esac
}

# The folder that names a service or an app: the part of the path before its source folder.
owner_of() {
  local path="$1" marker
  for marker in /Api/ /Controllers/ /src/; do
    if [[ "$path" == *"$marker"* ]]; then
      printf '%s' "${path%%"$marker"*}"
      return
    fi
  done
  printf '%s' "${path%/*}"
}

compare() { awk -f "${SCOPE_DIR}/compare.awk" "$1" "$2" | sort; }

# Records one tangible change for the table: kind, then a file status (A, D, R, M) or a compare.awk
# change (Added, Changed, Removed). A rename counts as a change.
metric() { # kind change
  local change
  case "$2" in
    A | added | Added) change=added ;;
    D | removed | Removed) change=removed ;;
    *) change=changed ;;
  esac
  printf '%s\t%s\n' "$1" "$change" >>"${WORK}/metrics"
}

# Lists each difference between deps.base and deps.head (name<TAB>version rows) as
# "- label: change `name` version", and counts it as a tangible change of the given kind.
record_dependencies() { # kind label
  compare "${WORK}/deps.base" "${WORK}/deps.head" | while IFS=$'\t' read -r dep_change dep_name dep_version; do
    printf -- '- %s: %s `%s`%s\n' "$2" "$(printf '%s' "$dep_change" | tr '[:upper:]' '[:lower:]')" \
      "$dep_name" "${dep_version:+ ${dep_version}}"
    metric "$1" "$dep_change"
  done >>"${WORK}/dependencies"
}

for section_file in controllers endpoints endpoints.changed dependencies configuration workflows; do
  : >"${WORK}/${section_file}"
done

while IFS=$'\t' read -r category status first second; do
  case "$status" in
    A) old=''; new="$first" ;;
    D) old="$first"; new='' ;;
    R*) old="$first"; new="$second" ;;
    *) old="$first"; new="$first" ;;
  esac
  path="${new:-$old}"
  name="${path##*/}"
  change=$(change_label "$status")
  # Endpoints: note each controller class touched, as its folder and its file name before the first
  # dot. A partial class spreads over XController.cs and XController.Query.cs, so the whole class
  # is compared below, not the one file. A *ControllerBase.cs file holds shared plumbing.
  if [[ "$category" == production && "$name" == *Controller*.cs && "$name" != *ControllerBase.cs ]]; then
    for controller_path in "$old" "$new"; do
      [[ -n "$controller_path" ]] || continue
      controller_file="${controller_path##*/}"
      printf '%s\t%s\n' "${controller_path%/*}" "${controller_file%%.*}" >>"${WORK}/controllers"
    done
    if [[ -n "$old" && -n "$new" ]]; then
      # -w: spacing within a line is not a change. --no-ext-diff and --no-color keep the hunk
      # headers that the user's diff settings could replace. A pure deletion has no head lines, so
      # it is recorded by its base lines.
      git diff --no-ext-diff --no-color -U0 -w -M "$MERGE_BASE" "$HEAD_REV" -- "$old" "$new" \
        | awk -v old_file="$old" -v new_file="$new" '
          function side(sign) {
            match($0, sign "[0-9]+(,[0-9]+)?")
            split(substr($0, RSTART + 1, RLENGTH - 1), part, ",")
            first = part[1]
            count = (2 in part) ? part[2] : 1
          }
          /^@@/ {
            side("\\+")
            if (count > 0) { print new_file "\t" first "\t" first + count - 1 "\thead"; next }
            side("-")
            print old_file "\t" first "\t" first + count - 1 "\tbase"
          }' >>"${WORK}/endpoints.changed"
    fi
  fi

  # Projects and dependencies: what each project builds on, which is the architecture.
  if [[ "$category" == production ]]; then
    case "$name" in
      *.csproj)
        if [[ "$change" != modified ]]; then
          printf -- '- %s project `%s`\n' "$change" "${path%/*}" >>"${WORK}/dependencies"
        fi
        ;;
    esac
    case "$name" in
      Directory.Packages.props)
        content_at base | awk -v kind=version -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.base"
        content_at head | awk -v kind=version -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.head"
        record_dependencies 'Package dependencies' NuGet
        ;;
      *.csproj)
        content_at base | awk -v kind=project -v dir="${old%/*}" -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.base"
        content_at head | awk -v kind=project -v dir="${new%/*}" -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.head"
        record_dependencies 'Project references' "\`${path%/*}\` project reference"
        content_at base | awk -v kind=package -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.base"
        content_at head | awk -v kind=package -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.head"
        record_dependencies 'Package dependencies' "\`${path%/*}\` package reference"
        ;;
      Directory.Build.props)
        # Every project below the file inherits these references, such as MinVer and SourceLink.
        content_at base | awk -v kind=package -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.base"
        content_at head | awk -v kind=package -f "${SCOPE_DIR}/msbuild.awk" >"${WORK}/deps.head"
        record_dependencies 'Package dependencies' "\`${path}\` package reference"
        ;;
    esac
  fi

  # Configuration: keys added or removed. Values are never printed, as they can hold secrets.
  if [[ "$category" == production && "$name" =~ ^appsettings.*\.json$ ]]; then
    parsed=true
    for side in base head; do
      content_at "$side" | jq -r 'paths(scalars) | map(tostring) | join(":") | . + "\t"' \
        >"${WORK}/keys.${side}" 2>/dev/null || parsed=false
    done
    if [[ "$parsed" == false ]]; then
      printf -- '- %s `%s` (not plain JSON; keys not compared)\n' "$change" "$path" >>"${WORK}/configuration"
    else
      compare "${WORK}/keys.base" "${WORK}/keys.head" | awk -F'\t' -v file="$path" -v metrics="${WORK}/metrics" '
            {
              print "Configuration keys\t" tolower($1) >>metrics
              count[$1]++
              if (count[$1] <= 8) keys[$1] = keys[$1] (keys[$1] == "" ? "" : ", ") "`" $2 "`"
            }
            END {
              for (c in keys) {
                more = count[c] > 8 ? sprintf(" and %d more", count[c] - 8) : ""
                printf "- `%s`: %s %s%s\n", file, tolower(c), keys[c], more
              }
            }' | sort >>"${WORK}/configuration"
    fi
  fi

  # CI workflows.
  if [[ "$category" == production && "$path" =~ ^\.github/workflows/.*\.ya?ml$ ]]; then
    printf -- '- %s `%s`\n' "$change" "$path" >>"${WORK}/workflows"
    metric 'CI workflows' "$status"
  fi
done <"${WORK}/inventory"

# The endpoints of every touched controller class at one revision, each row led by its service.
# A partial file without its own [Route] or policy takes them from the class's main file,
# XController.cs.
endpoints_at() { # revision
  local revision="$1" dir class main inherited file
  sort -u "${WORK}/controllers" | while IFS=$'\t' read -r dir class; do
    main="${dir}/${class}.cs"
    inherited=$( { git show "${revision}:${main}" 2>/dev/null || true; } | awk -v mode=class -f "${SCOPE_DIR}/endpoints.awk")
    git -c core.quotepath=off ls-tree --name-only "$revision" -- "${dir}/" | while IFS= read -r file; do
      case "${file##*/}" in
        "${class}.cs")
          git show "${revision}:${file}" | awk -v file="$file" -f "${SCOPE_DIR}/endpoints.awk"
          ;;
        "${class}".*.cs)
          git show "${revision}:${file}" | awk -v file="$file" -v inherited_route="${inherited%%$'\t'*}" \
            -v inherited_policy="${inherited#*$'\t'}" -f "${SCOPE_DIR}/endpoints.awk"
          ;;
      esac
    done | awk -v service="$(owner_of "${dir}/")" '{ print service "\t" $0 }'
  done
}

if [[ -s "${WORK}/controllers" ]]; then
  endpoints_at "$MERGE_BASE" >"${WORK}/endpoints.base"
  endpoints_at "$HEAD_REV" >"${WORK}/endpoints.head"
  awk -f "${SCOPE_DIR}/endpoint_changes.awk" \
    "${WORK}/endpoints.changed" "${WORK}/endpoints.base" "${WORK}/endpoints.head" \
    | sort | while IFS=$'\t' read -r endpoint_change service endpoint policy; do
        printf '| `%s` | %s | `%s` | %s |\n' "$service" "$endpoint_change" "$endpoint" "$policy"
        # A policy change is a change to the endpoint.
        metric 'API endpoints' "$endpoint_change"
      done >"${WORK}/endpoints"
fi

# Prints the metrics as one table row per kind, in a fixed order. A zero prints as an empty cell.
METRIC_ORDER='Project references|Package dependencies|API endpoints|Configuration keys|CI workflows'
print_metrics() {
  awk -F'\t' -v order="$METRIC_ORDER" '
    function cell(n) { return n > 0 ? n : "" }
    { count[$1, $2]++; present[$1] = 1 }
    END {
      n = split(order, kind, "|")
      for (i = 1; i <= n; i++) {
        if (!(kind[i] in present)) continue
        if (!header++) print "| Change | Added | Changed | Removed |\n|---|---|---|---|"
        printf "| %s | %s | %s | %s |\n", kind[i], cell(count[kind[i], "added"]),
          cell(count[kind[i], "changed"]), cell(count[kind[i], "removed"])
      }
    }' "${WORK}/metrics"
}

print_budget() {
  if (( PRODUCTION_LINES > PRODUCTION_CEILING || REVIEWABLE_LINES > REVIEWABLE_CEILING )); then
    echo "Budget: over the ceiling of ${PRODUCTION_CEILING} production lines, or ${REVIEWABLE_CEILING} production and test lines. Split the PR, or name its exemption in the description."
  elif (( PRODUCTION_LINES > PRODUCTION_TARGET )); then
    echo "Budget: over the ${PRODUCTION_TARGET}-line production target. State in the description why the change cannot be split."
  else
    echo "Budget: within the ${PRODUCTION_TARGET}-line production target."
  fi
}

# Prints a section's rows, cut at MAX_ROWS with a count of what was left out.
section() { # title file [table header lines...]
  local title="$1" file="$2"
  shift 2
  [[ -s "$file" ]] || return 0
  echo
  echo "**${title}**"
  echo
  if (( $# > 0 )); then
    printf '%s\n' "$@"
  fi
  awk -v max="$MAX_ROWS" -v table="$#" '
    NR <= max { print }
    END {
      if (NR > max) {
        if (table > 0) print ""
        printf "- and %d more\n", NR - max
      }
    }' "$file"
}

{
  echo "### Changes at a glance"
  echo
  if [[ -s "${WORK}/metrics" ]]; then
    print_metrics
  else
    echo "No tangible change that \`scope\` detects."
  fi
  if [[ -n "$AREAS" ]]; then
    echo
    echo "**Areas**: ${AREAS}"
  fi
  section 'API endpoints' "${WORK}/endpoints" '| Service | Change | Endpoint | Policy |' '|---|---|---|---|'
  section 'Projects and dependencies' "${WORK}/dependencies"
  section 'Configuration' "${WORK}/configuration"
  section 'CI workflows' "${WORK}/workflows"
}
print_budget >&2
