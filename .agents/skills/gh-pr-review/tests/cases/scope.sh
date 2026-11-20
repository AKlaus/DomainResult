#!/usr/bin/env bash
#
# scope reads a local Git repository and never calls curl, so these cases build a throwaway
# repository with a base commit and a head commit instead of stubbing the API.

# Creates an empty repository in $1 and makes it the working directory of the caller's subshell.
# Git runs inside $1 and takes no path: run_tests.sh turns off MSYS path conversion, so Git for
# Windows would read a /tmp/... argument as a path on the current drive. A folder that cannot be
# entered, or that already sits in a repository, ends the case: the case would commit to that one.
scope_init_repo() {
  mkdir -p "$1" && cd "$1" || exit 1
  if command git rev-parse --git-dir >/dev/null 2>&1; then
    printf 'ERROR: %s is inside a Git repository.\n' "$1" >&2
    exit 1
  fi
  command git init -q
  command git config core.autocrlf false
  command git config user.name 'Test User'
  command git config user.email 'test@example.invalid'
}

scope_commit() {
  command git add -A
  command git commit -qm "$1"
  command git tag "$1"
}

# Writes a file of $2 numbered lines, so its whole content counts as changed.
scope_lines() {
  mkdir -p "$(dirname "$1")"
  seq 1 "$2" | sed 's/^/line /' >"$1"
}

scope_run() {
  bash "${SCRIPT_DIR}/run.sh" scope "$@"
}

# Runs scope from base to head. Sets output to stdout and budget to stderr, and fails when scope
# fails or when the budget leaks into the description that stdout holds.
scope_run_budget() {
  local stderr_file
  stderr_file=$(mktemp)
  output=$(scope_run base head 2>"$stderr_file") || {
    cat "$stderr_file" >&2
    rm -f "$stderr_file"
    return 1
  }
  budget=$(cat "$stderr_file")
  rm -f "$stderr_file"
  assert_not_contains "$output" 'Budget'
}

# Arguments: the expected budget text, then one path:lines pair per file the head adds.
test_scope_budget() {
  local expected="$1" spec temp_root output budget
  shift
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf 'readme\n' >README.txt
  scope_commit base
  for spec in "$@"; do
    scope_lines "${spec%:*}" "${spec##*:}"
  done
  scope_commit head

  scope_run_budget &&
    assert_contains "$budget" "$expected"
}

# Writes $2 lines to $1 from the template $3, with each %d replaced by the line number. awk turns
# a \n in the template into a line break, so one template can write several lines per number.
scope_template() {
  mkdir -p "$(dirname "$1")"
  seq 1 "$2" | awk -v t="$3" '{ s = t; gsub(/%d/, $1, s); print s }' >"$1"
}

# Arguments: the expected budget text, then one "path|lines|base template|head template" spec per
# file. An empty base template makes the head add the file; an empty head template deletes it.
test_scope_budget_edit() {
  local expected="$1" spec path count before after temp_root output budget
  shift
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf 'readme\n' >README.txt
  for spec in "$@"; do
    IFS='|' read -r path count before after <<<"$spec"
    [[ -z "$before" ]] || scope_template "$path" "$count" "$before"
  done
  scope_commit base
  for spec in "$@"; do
    IFS='|' read -r path count before after <<<"$spec"
    if [[ -n "$after" ]]; then
      scope_template "$path" "$count" "$after"
    else
      rm -f "$path"
    fi
  done
  scope_commit head

  scope_run_budget &&
    assert_contains "$budget" "$expected"
}

# A 500-line file counted twice would be over the target. Counted by its one edit, it is within.
# Arguments: the destination and its area. A move across top-level folders prints "old => new";
# a move inside one prints "svc/{old => new}".
test_scope_counts_a_move_by_its_edits() {
  local destination="$1" area="$2" temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  scope_lines 'svc/Domain/Big Widget.cs' 500
  scope_commit base
  mkdir -p "$(dirname "$destination")"
  command git mv 'svc/Domain/Big Widget.cs' "$destination"
  sed 's/^line 150$/line 150 changed/' "$destination" >moved.tmp
  mv moved.tmp "$destination"
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" 'Budget: within the 500-line' &&
    assert_contains "$output" "**Areas**: \`${area}\`"$'\n'
}

# A PR shows the changes since its branch left the target, not commits the target gained since.
test_scope_ignores_commits_on_base_after_the_fork() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf 'readme\n' >README.txt
  scope_commit fork
  command git checkout -qb feature
  scope_lines svc/Domain/Widget.cs 10
  scope_commit head
  command git checkout -q fork
  scope_lines svc/Domain/Other.cs 900
  scope_commit base

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" 'Budget: within the 500-line'
}

test_scope_reports_unrelated_histories() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf 'readme\n' >README.txt
  scope_commit base
  command git checkout -q --orphan other
  scope_commit head

  if output=$(scope_run base head 2>&1); then
    printf 'Expected scope to fail. Output:\n%s\n' "$output" >&2
    return 1
  fi
  assert_contains "$output" "share no commit"
}

# Areas names the top-level folders a PR touches. Agent tooling and root files are not areas, and
# the table does not count projects.
test_scope_lists_areas() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf '<Project />\n' >Directory.Build.props
  scope_lines svc/Api/Api.csproj 3
  scope_lines svc/Api/Controllers/WidgetController.cs 10
  scope_lines svc/Domain/Domain.csproj 3
  scope_commit base

  printf 'line 11\n' >>svc/Api/Controllers/WidgetController.cs
  scope_lines lib/Store/Store.csproj 3
  scope_lines lib/Store/Store.cs 10
  command git rm -rq svc/Domain
  scope_lines .agents/skills/widgets/scripts/run.sh 10
  printf '<Project>\n</Project>\n' >Directory.Build.props
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '**Areas**: `lib`, `svc`'$'\n' &&
    assert_not_contains "$output" '| Projects' &&
    assert_not_contains "$output" 'lines ('
}

test_scope_lists_dependencies_and_configuration_keys() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p svc/Api
  printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk.Web">' '  <ItemGroup>' \
    '    <PackageReference Include="CsvHelper" />' \
    '    <ProjectReference Include="..\Domain\Domain.csproj" />' \
    '  </ItemGroup>' '</Project>' >svc/Api/Api.csproj
  printf '{ "Widgets": { "PageSize": 20 }, "ConnectionStrings": { "Db": "secret-old" } }\n' \
    >svc/Api/appsettings.json
  mkdir -p src
  printf '%s\n' '<Project>' '  <ItemGroup>' \
    '    <PackageReference Include="MinVer" Version="4.2.0" PrivateAssets="All" />' \
    '  </ItemGroup>' '</Project>' >src/Directory.Build.props
  scope_commit base

  printf '%s\n' '<Project Sdk="Microsoft.NET.Sdk.Web">' '  <ItemGroup>' \
    '    <ProjectReference Include="..\Domain\Domain.csproj" />' \
    '    <ProjectReference Include="..\..\lib\ObjectStore\ObjectStore.csproj" />' \
    '  </ItemGroup>' '</Project>' >svc/Api/Api.csproj
  printf '{ "Widgets": { "PageSize": 50, "MaxPages": 5 }, "ConnectionStrings": { "Db": "secret-new" } }\n' \
    >svc/Api/appsettings.json
  printf '%s\n' '<Project>' '  <ItemGroup>' \
    '    <PackageReference Include="MinVer" Version="6.1.0" PrivateAssets="All" />' \
    '  </ItemGroup>' '</Project>' >src/Directory.Build.props
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| Project references | 1 |  |  |' &&
    assert_contains "$output" '| Package dependencies |  | 1 | 1 |' &&
    assert_contains "$output" '- `src/Directory.Build.props` package reference: changed `MinVer` 4.2.0 -> 6.1.0' &&
    assert_contains "$output" '| Configuration keys | 1 |  |  |' &&
    assert_contains "$output" '- `svc/Api` project reference: added `lib/ObjectStore`' &&
    assert_contains "$output" '- `svc/Api` package reference: removed `CsvHelper`' &&
    assert_contains "$output" '- `svc/Api/appsettings.json`: added `Widgets:MaxPages`' &&
    # A changed value is not a new key, and values never print: they can hold secrets.
    assert_not_contains "$output" 'PageSize' &&
    assert_not_contains "$output" 'secret'
}

# A workflow file under .github/workflows/ is a CI change; other YAML is not.
test_scope_lists_ci_workflows() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p .github/workflows
  printf 'name: CI\n' >.github/workflows/ci.yml
  scope_commit base

  printf 'name: CI\non: push\n' >.github/workflows/ci.yml
  printf 'name: Release\n' >.github/workflows/release.yml
  printf 'version: 2\n' >.github/dependabot.yml
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| CI workflows | 1 | 1 |  |' &&
    assert_contains "$output" '- modified `.github/workflows/ci.yml`' &&
    assert_contains "$output" '- added `.github/workflows/release.yml`' &&
    assert_not_contains "$output" 'dependabot'
}

test_scope_reads_files_with_a_byte_order_mark() {
  local temp_root output budget
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  # GIVEN a C# file that starts with a byte-order mark, and an edit that removes the mark
  mkdir -p src
  printf '\357\273\277namespace Widgets;\n// A comment – with an en dash\n' >src/Widget.cs
  scope_commit base
  printf 'namespace Widgets;\n// A comment – with an en dash\n' >src/Widget.cs
  scope_commit head

  # WHEN scope runs in a UTF-8 locale
  # THEN it reports the budget instead of failing in awk
  LC_ALL=en_US.UTF-8 scope_run_budget &&
    assert_contains "$budget" 'Budget: within the 500-line'
}

# A list stops at 15 rows with a count of the rest.
test_scope_cuts_long_lists() {
  local temp_root output keys i
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf 'readme\n' >README.txt
  scope_commit base

  mkdir -p svc/Api
  # 20 files of 8 long keys: the list stops at 15 rows.
  keys=$(printf '"AVeryLongConfigurationKeyForTheWidgetService%s": 0, ' a b c d e f g h)
  for i in $(seq 1 20); do printf '{ %s"End": 0 }\n' "$keys" >"svc/Api/appsettings.W${i}.json"; done
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '- and 5 more'
}

# src/Directory.Build.props pins one version of a package per target framework. Each condition is
# its own reference, so an edit elsewhere in the file is no package change.
test_scope_keys_package_references_by_condition() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  # GIVEN two conditional versions of one package
  mkdir -p src
  scope_props() { # property value, version of the second condition
    printf '%s\n' '<Project>' "  <PropertyGroup><Deterministic>$1</Deterministic></PropertyGroup>" '  <ItemGroup>' \
      "    <PackageReference Include=\"MinVer\" Version=\"4.2.0\" Condition=\"'\$(TargetFramework)'=='net7.0'\" />" \
      "    <PackageReference Include=\"MinVer\" Version=\"$2\" Condition=\"'\$(TargetFramework)'!='net7.0'\" />" \
      '  </ItemGroup>' '</Project>' >src/Directory.Build.props
  }
  scope_props true 6.1.0
  scope_commit base
  scope_props false 6.1.0
  scope_commit edit
  scope_props false 7.0.0
  scope_commit head

  # WHEN the file changes elsewhere, and then in one version
  local unrelated
  unrelated=$(scope_run base edit 2>&1) && output=$(scope_run edit head 2>&1) || {
    printf '%s\n%s\n' "${unrelated:-}" "${output:-}" >&2
    return 1
  }

  # THEN only the version change is reported, under its condition
  assert_contains "$unrelated" 'No tangible change' &&
    assert_contains "$output" "changed \`MinVer if '\$(TargetFramework)'!='net7.0'\` 6.1.0 -> 7.0.0" &&
    assert_not_contains "$output" '4.2.0'
}

# The budget comes from the table in AGENTS.md. A copy of the skill below a repository root with
# its own AGENTS.md reads that table, and fails when the table is missing.
test_scope_reads_the_budget_from_agents_md() {
  local temp_root output skill
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  skill="${temp_root}/tooling/.agents/skills/gh-pr-review"
  mkdir -p "${skill}/scripts/scope"
  cp "${SCRIPT_DIR}/run.sh" "${SCRIPT_DIR}/config.sh" "${skill}/scripts/"
  cp "${SCRIPT_DIR}"/scope/* "${skill}/scripts/scope/"
  printf '%s\n' '| Lines | Target | Ceiling |' '|---|---|---|' '| Production | 10 | 20 |' \
    '| Production and tests | none | 30 |' >"${temp_root}/tooling/AGENTS.md"

  # GIVEN a change of 11 production lines, and a table whose target is 10
  scope_init_repo "${temp_root}/repo"
  printf 'readme\n' >README.txt
  scope_commit base
  scope_lines svc/Domain/Widget.cs 11
  scope_commit head

  # WHEN scope runs, with the table and then without it
  output=$(bash "${skill}/scripts/run.sh" scope base head 2>&1 >/dev/null) || {
    printf '%s\n' "$output" >&2
    return 1
  }
  printf '# No table\n' >"${temp_root}/tooling/AGENTS.md"

  # THEN the table sets the budget, and a missing table fails the run
  assert_contains "$output" 'Budget: over the 10-line production target' &&
    output=$(bash "${skill}/scripts/run.sh" scope base head 2>&1) && return 1
  assert_contains "$output" 'the budget table in' &&
    assert_contains "$output" 'tooling/AGENTS.md was not read'
}

run_test 'scope budget: 500 production lines are within the target' test_scope_budget 'Budget: within the 500-line' svc/Domain/Widget.cs:500
run_test 'scope budget: 501 production lines are over the target' test_scope_budget 'Budget: over the 500-line' svc/Domain/Widget.cs:501
run_test 'scope budget: 1000 production lines are within the ceiling' test_scope_budget 'Budget: over the 500-line' svc/Domain/Widget.cs:1000
run_test 'scope budget: 1001 production lines are over the ceiling' test_scope_budget 'Budget: over the ceiling' svc/Domain/Widget.cs:1001
run_test 'scope budget: 2000 production and test lines are within the ceiling' test_scope_budget 'Budget: within the 500-line' svc/Domain/Widget.cs:100 svc/Tests/Domain.Tests/WidgetTests.cs:1900
run_test 'scope budget: 2001 production and test lines are over the ceiling' test_scope_budget 'Budget: over the ceiling' svc/Domain/Widget.cs:100 svc/Tests/Domain.Tests/WidgetTests.cs:1901
run_test 'scope budget: auxiliary files do not count' test_scope_budget 'Budget: within the 500-line' svc/Domain/Widget.cs:500 svc/packages.lock.json:5000 all.sln:900 all.sln.DotSettings:900
run_test 'scope budget: agent tooling and docs do not count' test_scope_budget 'Budget: within the 500-line' svc/Domain/Widget.cs:500 .agents/skills/widgets/scripts/run.sh:900 AGENTS.md:900 svc/Tests/Domain.Tests/README.md:900 docs/guide.txt:900
run_test 'scope budget: added and deleted comment lines do not count' test_scope_budget_edit 'Budget: within the 500-line' \
  'svc/Domain/Widget.cs|500||line %d' \
  'svc/Domain/Widget Notes.cs|100||// note %d' \
  'svc/Domain/Legacy.cs|100|/* legacy %d */|' \
  'svc/Domain/Widget.Docs.cs|100||/// <summary>\n/// note %d\n/// </summary>' \
  'svc/Api/Api.csproj|100||<!-- note %d -->' \
  '.github/workflows/ci.yml|100||# note %d'
run_test 'scope budget: an edit far inside a block comment does not count' test_scope_budget_edit 'Budget: within the 500-line' \
  'svc/Domain/Notes.cs|251|/*\na\nb\nc\nd\ne\nf\ng\nnote %d\nh\ni\nj\n*/|/*\na\nb\nc\nd\ne\nf\ng\nnote: %d\nh\ni\nj\n*/'
run_test 'scope budget: a code line with a trailing comment counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|501||run(%d); // note'
run_test 'scope budget: a rename does not count' test_scope_budget_edit 'Budget: within the 500-line' \
  'svc/Domain/Widget.cs|500||line %d' \
  'svc/Domain/Usage.cs|300|var widget%d = new Widget();|var gadget%d = new Gadget();'
run_test 'scope budget: a replacement made only once counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses p%d|line %d uses q'
run_test 'scope budget: a keyword swap counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d is true|line %d is false'
run_test 'scope budget: a version change inside a name counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d targets Net8|line %d targets net9'
run_test 'scope budget: a replacement whose old identifier remains in the file counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses Widget\nkeep %d Widget|line %d uses Gadget\nkeep %d Widget'
run_test 'scope budget: a replacement by a name the old file used counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses orderedFields\nkeep %d fields|line %d uses fields\nkeep %d fields'
run_test 'scope budget: a rename with another edit counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses Widget|line %d uses Gadget()'
run_test 'scope counts a move to another folder by its edits' test_scope_counts_a_move_by_its_edits lib/Widgets/BigWidget.cs lib
run_test 'scope counts a move inside a folder by its edits' test_scope_counts_a_move_by_its_edits svc/Widgets/BigWidget.cs svc
run_test 'scope ignores commits on base after the fork' test_scope_ignores_commits_on_base_after_the_fork
run_test 'scope reports unrelated histories' test_scope_reports_unrelated_histories
run_test 'scope lists areas, not projects' test_scope_lists_areas
run_test 'scope lists dependency and configuration key changes' test_scope_lists_dependencies_and_configuration_keys
run_test 'scope lists CI workflows' test_scope_lists_ci_workflows
run_test 'scope reads files with a byte-order mark' test_scope_reads_files_with_a_byte_order_mark
run_test 'scope cuts long lists' test_scope_cuts_long_lists
run_test 'scope keys package references by condition' test_scope_keys_package_references_by_condition
run_test 'scope reads the budget from AGENTS.md' test_scope_reads_the_budget_from_agents_md
