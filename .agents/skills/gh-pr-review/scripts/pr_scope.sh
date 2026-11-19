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

# The budget, in changed lines (added plus deleted). references/scope.md gives the reasons, and
# AGENTS.md quotes the numbers: change all three places together.
PRODUCTION_TARGET=500
PRODUCTION_CEILING=1000
REVIEWABLE_CEILING=2000

# Each detail list shows at most this many rows. This shortens a long list, but it does not bound
# the whole report: a row can hold several names.
MAX_ROWS=15

SCOPE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/scope" && pwd)"

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

# Generated code, auxiliary files and experiments, docs, and binary files do not count.
PRODUCTION_LINES=$(lines production)
REVIEWABLE_LINES=$(( PRODUCTION_LINES + $(lines test) ))

# ── Metrics ──────────────────────────────────────────────────────────────────────────────────────
# One row per tangible change: kind, then added, changed, or removed. The table below counts them.
git -c core.quotepath=off diff -M --name-status "$MERGE_BASE" "$HEAD_REV" >"${WORK}/status"
: >"${WORK}/metrics"

# Projects: a folder that holds a *.csproj, a package.json, or a pyproject.toml, at either end. The
# lists below name a service or an app by its project. The table does not count projects: Areas
# names the parts of the repository that a PR touches more plainly.
{
  git -c core.quotepath=off ls-tree -r --name-only "$MERGE_BASE"
  git -c core.quotepath=off ls-tree -r --name-only "$HEAD_REV"
} | awk '/\// && (/\.csproj$/ || /\/package\.json$/ || /\/pyproject\.toml$/) { sub(/\/[^\/]*$/, ""); print }' \
  | sort -u >"${WORK}/projects"

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
  for marker in /Api/ /Controllers/ /src/ /Migrations/; do
    if [[ "$path" == *"$marker"* ]]; then
      printf '%s' "${path%%"$marker"*}"
      return
    fi
  done
  printf '%s' "${path%/*}"
}

# The project that holds a path: its nearest folder in the projects list. A front-end file can sit
# outside src/, such as a preview harness, so the project names its app better than owner_of does.
project_of() {
  local dir="$1"
  while [[ "$dir" == */* ]]; do
    dir="${dir%/*}"
    if grep -Fxq -- "$dir" "${WORK}/projects"; then
      printf '%s' "$dir"
      return
    fi
  done
  owner_of "$1"
}

# Whether a project is a front-end app or package: it holds a package.json at either end.
is_front_end() { # project
  git cat-file -e "${HEAD_REV}:${1}/package.json" 2>/dev/null ||
    git cat-file -e "${MERGE_BASE}:${1}/package.json" 2>/dev/null
}

# Whether a component exists at one revision. A folder, given with its trailing slash, exists when
# it holds a .tsx file; a .tsx file at the app's root exists when it is there.
component_exists() { # revision component-path
  if [[ "$2" == */ ]]; then
    git -c core.quotepath=off ls-tree --name-only "$1" -- "$2" | awk '/\.tsx$/ { found = 1 } END { exit !found }'
  else
    git cat-file -e "${1}:${2}" 2>/dev/null
  fi
}

# A generated file in a test folder is a fixture, not a contract. classify.awk files every *.gen.*
# as generated, so it classifies the same path without the .gen part.
is_test_fixture() { # path
  [[ "$(printf '0\t0\t%s.%s\n' "${1%.gen.*}" "${1##*.}" | awk -f "${SCOPE_DIR}/classify.awk" | cut -f1)" == test ]]
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

# Records each table, view, function, procedure, and type that the loop's SQL script changes, as
# service, kind, name, and change (added, replaced, changed, data, or removed). A created or
# replaced view, function, or procedure is added only if no SQL file of the service named it at the
# base; otherwise it is replaced. A script that names nothing that database.awk reads is listed by
# its file name, so the reviewer still sees it.
record_database_objects() {
  local service kind object statement object_change
  service=$(project_of "$path")
  content_at head | awk -f "${SCOPE_DIR}/database.awk" >"${WORK}/sql.head"
  : >"${WORK}/sql.base"
  if [[ "$path" == */CustomFunctions/* ]]; then
    content_at base | awk -f "${SCOPE_DIR}/database.awk" >"${WORK}/sql.base"
  fi
  if [[ ! -s "${WORK}/sql.head" && ! -s "${WORK}/sql.base" ]]; then
    if [[ -n "$new" ]]; then
      printf -- '- `%s`: %s `%s`, which names no database object that `scope` reads\n' \
        "$service" "$change" "$name" >>"${WORK}/database.unread"
      metric Database changed
    fi
    return 0
  fi
  while IFS=$'\t' read -r kind object statement; do
    case "$statement" in
      created | replaced)
        object_change=added
        if [[ "$kind" =~ ^(view|function|procedure)$ ]] && git grep -q -i -w -F -e "${object##*.}" "$MERGE_BASE" -- \
            "${service}/*.sql" "${service}/*.pgsql"; then
          object_change=replaced
        fi
        ;;
      altered) object_change=changed ;;
      data) object_change=data ;;
      *) object_change=removed ;;
    esac
    printf '%s\t%s\t%s\t%s\n' "$service" "$kind" "$object" "$object_change" >>"${WORK}/database.rows"
  done <"${WORK}/sql.head"
  # A routine or view that leaves a CustomFunctions/ script no longer deploys.
  awk -F'\t' -v service="$service" '
    FILENAME == ARGV[1] { head[$1 "\t" $2] = 1; next }
    $1 ~ /^(view|function|procedure)$/ && !(($1 "\t" $2) in head) && !seen[$1 "\t" $2]++ {
      print service "\t" $1 "\t" $2 "\tremoved"
    }' \
    "${WORK}/sql.head" "${WORK}/sql.base" >>"${WORK}/database.rows"
}

for section_file in controllers endpoints endpoints.changed contracts routes components database.rows database.unread dependencies configuration infrastructure; do
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
  # A contract or component name is internal: neither an API caller nor a user sees it. A moved
  # file without edits is no tangible change, and a moved file with edits is a modified one.
  case "$status" in
    R100) content_change='' ;;
    R*) content_change=modified ;;
    *) content_change="$change" ;;
  esac

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

  # Contracts that other services compile against: SDK clients and DTO assemblies, and the
  # generated front-end clients. One row per file; folded per folder below.
  if [[ -n "$content_change" && "$category" == production && "$name" == *.cs && "$path" =~ /SDK\.(Client|DTOs)/ ]]; then
    project="${path%%/SDK.${BASH_REMATCH[1]}/*}/SDK.${BASH_REMATCH[1]}"
    printf '%s\t%s\t`%s`\n' "$project" "$content_change" "${name%.*}" >>"${WORK}/contracts"
    metric 'API contracts and clients' "$content_change"
  elif [[ -n "$content_change" && "$category" == generated && "$name" =~ \.gen\.[cm]?[jt]sx?$ ]] && ! is_test_fixture "$path"; then
    printf '%s\t%s\t`%s`\n' "${path%/*}" "$content_change (generated)" "${name%%.gen.*}" >>"${WORK}/contracts"
    metric 'API contracts and clients' "$content_change"
  fi

  # UI routes: path literals in the files that declare routes. App.tsx, AppRoutes.tsx,
  # AppRouteElements.tsx, and the *Module.tsx route tables match; breadcrumb configs do not.
  if [[ "$category" == production && "$name" =~ ^(App[A-Za-z]*|[A-Za-z]*[Mm]odules?|[A-Za-z]*[Rr]out(es?|er))\.tsx?$ ]]; then
    content_at base | awk -f "${SCOPE_DIR}/routes.awk" >"${WORK}/routes.base"
    content_at head | awk -f "${SCOPE_DIR}/routes.awk" >"${WORK}/routes.head"
    compare "${WORK}/routes.base" "${WORK}/routes.head" \
      | while IFS=$'\t' read -r route_change route _; do
          printf '| `%s` | %s | `%s` |\n' "$(project_of "$path")" "$route_change" "$route"
          metric 'UI routes' "$route_change"
        done >>"${WORK}/routes"
  fi

  # UI components: the front-end source files of each component folder, named from the app. A
  # file at the app's root is a component of its own only if it is a .tsx file. The rows are
  # folded per component below, where its size is the sum of its files' changed lines.
  if [[ -n "$content_change" && "$category" == production && "$name" =~ \.(tsx|ts|scss|css)$ ]]; then
    app=$(project_of "$path")
    component="${path#"$app"/}"
    component="${component#src/}"
    if [[ "$component" == */* ]]; then
      component="${component%/*}/"
    elif [[ "$name" != *.tsx ]]; then
      component=''
    fi
    if [[ -n "$component" ]] && is_front_end "$app"; then
      printf '%s\t%s\t%s\t%s\n' "$app" "$component" "$path" "$status" >>"${WORK}/components"
    fi
  fi

  # Database: the objects that a deployed script changes. A Migrations/ script runs once. A
  # CustomFunctions/ script redeploys its routines with every release, as in downhole-data-service,
  # so a routine that leaves it is removed. Other SQL, such as a query beside its service or a
  # devops/utils/sql script run by hand, deploys nothing.
  if [[ "$category" == production && ( "$name" == *.sql || "$name" == *.pgsql ) &&
        ( "$path" == */Migrations/* || "$path" == */CustomFunctions/* ) ]]; then
    record_database_objects
  fi

  # Projects and dependencies: what each project builds on, which is the architecture.
  if [[ "$category" == production ]]; then
    case "$name" in
      *.csproj | package.json | pyproject.toml)
        if [[ "$change" != modified ]]; then
          printf -- '- %s project `%s`\n' "$change" "${path%/*}" >>"${WORK}/dependencies"
        fi
        ;;
    esac
    # requirements.txt often holds only "-r requirements/prod.txt"; the pins live in that folder.
    dependency_file="$name"
    if [[ "$path" == */requirements/*.txt ]]; then
      dependency_file=requirements.txt
    fi
    case "$dependency_file" in
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
      package.json)
        for side in base head; do
          content_at "$side" | jq -r '
            ("dependencies", "devDependencies", "peerDependencies") as $kind
            | (.[$kind] // {}) | to_entries[] | "\(.key) (\($kind))\t\(.value)"' \
            >"${WORK}/deps.${side}" 2>/dev/null || : >"${WORK}/deps.${side}"
        done
        record_dependencies 'Package dependencies' "npm \`${path%/*}\`"
        ;;
      requirements*.txt)
        for side in base head; do
          content_at "$side" | awk '
            { sub(/#.*/, ""); gsub(/[ \t\r]/, "") }
            /^-/ || $0 == "" { next }
            { name = $0; sub(/[=<>~!;\[].*/, "", name); spec = substr($0, length(name) + 1); print tolower(name) "\t" spec }' \
            >"${WORK}/deps.${side}"
        done
        record_dependencies 'Package dependencies' "Python \`$path\`"
        ;;
      pyproject.toml)
        if [[ "$change" == modified ]]; then
          printf -- '- modified `%s` (dependencies not compared)\n' "$path" >>"${WORK}/dependencies"
        fi
        ;;
    esac
  fi

  # Configuration: keys added or removed. Values are never printed, as they can hold secrets.
  if [[ "$category" == production && ( "$name" =~ ^appsettings.*\.json$ || "$path" =~ /public/env[^/]*\.json$ ) ]]; then
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

  # Infrastructure and pipelines.
  if [[ "$category" == production && ( "$path" == devops/* || "$path" == */devops/* || "$name" =~ \.(bicep|tf)$ || "$name" == Dockerfile* ||
        "$name" == docker-compose* || "$name" =~ ^azure-pipelines.*\.ya?ml$ || "$path" =~ /pipelines?/.*\.ya?ml$ ||
        "$path" =~ ^\.github/workflows/.*\.ya?ml$ ) ]]; then
    printf -- '- %s `%s`\n' "$change" "$path" >>"${WORK}/infrastructure"
    metric 'Infrastructure files' "$status"
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

# Folds one row per contract file into one row per folder and change: "- `folder`: change names".
awk -F'\t' '
  {
    key = $1 SUBSEP $2
    if (!(key in count)) order[++n] = key
    count[key]++
    if (count[key] <= 8) names[key] = names[key] (names[key] == "" ? "" : ", ") $3
  }
  END {
    for (i = 1; i <= n; i++) {
      split(order[i], part, SUBSEP)
      more = count[order[i]] > 8 ? sprintf(" and %d more", count[order[i]] - 8) : ""
      printf "- `%s`: %s %s%s\n", part[1], part[2], names[order[i]], more
    }
  }' "${WORK}/contracts" >"${WORK}/contracts.folded"

# One row per app: how many components changed, and one size for all of them together, from the
# changed lines of their files. The size is approximate on purpose: small under 50 lines, average
# under 200, and big from 200. Whether a component is added, modified, or removed depends on whether
# it exists at each end, not on which of its files changed: a new helper in an existing component
# modifies it. A folder without a .tsx file at either end holds helpers, not components. A file
# moved into a component existed at the base under its old path, so the component is modified.
awk -F'\t' '
  FILENAME == ARGV[1] { lines[$4] = $2 + $3; next }
  {
    key = $1 SUBSEP $2
    if (!(key in total)) {
      order[++n] = key
      identity[key] = $3
      if ($2 ~ /\/$/) sub(/[^\/]*$/, "", identity[key])
    }
    total[key] += lines[$3]
    if ($4 ~ /^R/) moved[key] = 1
  }
  END {
    for (i = 1; i <= n; i++) {
      key = order[i]
      split(key, part, SUBSEP)
      print part[1] "\t" total[key] "\t" ((key in moved) ? 1 : 0) "\t" identity[key]
    }
  }' "${WORK}/classified" "${WORK}/components" \
  | while IFS=$'\t' read -r app component_lines moved component_path; do
      at_base=false
      at_head=false
      component_exists "$MERGE_BASE" "$component_path" && at_base=true
      component_exists "$HEAD_REV" "$component_path" && at_head=true
      if [[ "$moved" == 1 && "$at_head" == true ]]; then
        at_base=true
      fi
      case "$at_base:$at_head" in
        true:true) component_change=modified ;;
        false:true) component_change=added ;;
        true:false) component_change=removed ;;
        *) continue ;;
      esac
      metric 'UI components' "$component_change"
      printf '%s\t%s\n' "$app" "$component_lines"
    done \
  | LC_ALL=C sort -t$'\t' -k1,1 \
  | awk -F'\t' '
      { if (!($1 in count)) order[++n] = $1; count[$1]++; total[$1] += $2 }
      END {
        for (i = 1; i <= n; i++) {
          app = order[i]
          size = total[app] >= 200 ? "big" : (total[app] >= 50 ? "average" : "small")
          printf "| `%s` | %d | %s |\n", app, count[app], size
        }
      }' >"${WORK}/components.table"

# One row per database object: an object that the PR adds and alters is added, one that it adds or
# replaces and also removes is changed, as a migration that drops and re-creates a routine does, and
# one whose rows it only writes has changed data. An altered table that the PR drops is removed.
# Each object, whatever its kind, counts once in the Database row of the table above.
awk -F'\t' -v metrics="${WORK}/metrics" '
  {
    key = $1 SUBSEP $2 SUBSEP $3
    if (!(key in changes)) order[++n] = key
    changes[key] = changes[key] " " $4
  }
  END {
    for (i = 1; i <= n; i++) {
      c = changes[order[i]]
      split(order[i], part, SUBSEP)
      if (c ~ /added|replaced/ && c ~ /removed/) change = "changed"
      else if (c ~ /added/) change = "added"
      else if (c ~ /removed/) change = "removed"
      else if (c ~ /changed|replaced/) change = "changed"
      else change = "changed data in"
      print "Database\t" (change ~ /^changed/ ? "changed" : change) >>metrics
      printf "%s\t%s\t%s\t%s\n", part[1], change, part[2], part[3]
    }
  }' "${WORK}/database.rows" | sort -t$'\t' -k1,1 -k2,2 -k3,3 -k4,4 >"${WORK}/database.items"

# One line per service and change, with the names grouped by kind:
# "- `svc`: added tables `A`, `B`; function `f`". Each kind lists at most 8 names.
awk -F'\t' '
  function flush_kind() {
    if (kind == "") return
    text = text (text == "" ? "" : "; ") kind (count > 1 ? "s" : "") " " names \
      (count > 8 ? sprintf(" and %d more", count - 8) : "")
    kind = ""
    names = ""
    count = 0
  }
  function flush_group() {
    flush_kind()
    if (service != "") printf "- `%s`: %s %s\n", service, change, text
    text = ""
  }
  ($1 SUBSEP $2) != group {
    flush_group()
    group = $1 SUBSEP $2
    service = $1
    change = $2
  }
  $3 != kind {
    flush_kind()
    kind = $3
  }
  { if (++count <= 8) names = names (names == "" ? "" : ", ") "`" $4 "`" }
  END { flush_group() }' "${WORK}/database.items" >"${WORK}/database"
cat "${WORK}/database.unread" >>"${WORK}/database"

# Prints the metrics as one table row per kind, in a fixed order. A zero prints as an empty cell.
METRIC_ORDER='Project references|Package dependencies|API endpoints|API contracts and clients|UI routes|UI components|Database|Configuration keys|Infrastructure files'
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
  section 'API contracts and clients' "${WORK}/contracts.folded"
  section 'UI routes' "${WORK}/routes" '| App | Change | Route |' '|---|---|---|'
  section 'UI components' "${WORK}/components.table" '| App | Components | Size |' '|---|---|---|'
  section 'Database' "${WORK}/database"
  section 'Projects and dependencies' "${WORK}/dependencies"
  section 'Configuration' "${WORK}/configuration"
  section 'Infrastructure and pipelines' "${WORK}/infrastructure"
}
print_budget >&2
