#!/usr/bin/env bash
#
# scope reads a local Git repository and never calls curl, so these cases build a throwaway
# repository with a base commit and a head commit instead of stubbing the API.

# Creates an empty repository in $1 and makes it the working directory of the caller's subshell.
# Git runs inside $1 and takes no path: run_tests.sh turns off MSYS path conversion, so Git for
# Windows would read a /tmp/... argument as a path on the current drive.
scope_init_repo() {
  cd "$1" || return 1
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

scope_widget_controller() { # head|base
  mkdir -p svc/Api/Controllers
  {
    printf '%s\n' \
      'namespace Svc.Api.Controllers;' \
      '' \
      '[ApiController]' \
      '[Authorize(Policy = Policies.RequireUser)]' \
      '[Route("api/widgets")]' \
      'public class WidgetController(IWidgetService service) : ControllerBase' \
      '{' \
      '	/// <summary>' \
      '	/// Lists widgets.' \
      '	/// </summary>' \
      '	[HttpGet]' \
      '	public async Task<ActionResult<WidgetsResponse>> List(' \
      '		[FromQuery] int page,' \
      '		CancellationToken ct)' \
      '	{'
    if [[ "$1" == head ]]; then
      printf '%s\n' '		var widgets = await service.List(page, pageSize: 50, ct);'
    else
      printf '%s\n' '		var widgets = await service.List(page, pageSize: 20, ct);'
    fi
    printf '%s\n' \
      '		return widgets.ToActionResult();' \
      '	}' \
      ''
    printf '%s\n' '	#region Commands'
    if [[ "$1" == head ]]; then
      printf '%s\n' \
        '	[HttpPost]' \
        '	public Task<ActionResult<long>> Create([FromBody] CreateWidgetRequest request, CancellationToken ct)' \
        '		=> service.Create(request, ct).ToActionResultOfT();' \
        ''
    else
      printf '%s\n' \
        '	[HttpDelete("{id:long}")]' \
        '	public Task<IActionResult> Delete(long id, CancellationToken ct)' \
        '		=> service.Delete(id, ct).ToActionResult();' \
        ''
    fi
    printf '%s\n' '	#endregion' ''
    printf '%s\n' \
      '	[HttpGet("{id:long}")]' \
      '	public async Task<ActionResult<WidgetResponse>> Get(long id, CancellationToken ct)' \
      '	{' \
      '		var widget = await service.Get(id, ct);' \
      '		return Map(widget);' \
      '	}' \
      '' \
      '	private static ActionResult<WidgetResponse> Map(IDomainResult<Widget> widget)' \
      '	{'
    if [[ "$1" == head ]]; then
      printf '%s\n' '		return widget.ToActionResultOfT(w => new WidgetResponse(w.Id, w.Name));'
    else
      printf '%s\n' '		return widget.ToActionResultOfT(w => new WidgetResponse(w.Id));'
    fi
    printf '%s\n' \
      '	}' \
      '}'
  } >svc/Api/Controllers/WidgetController.cs
}

test_scope_classifies_endpoint_changes() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  scope_widget_controller base
  scope_commit base
  scope_widget_controller head
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| API endpoints | 1 | 1 | 1 |' &&
    assert_contains "$output" '| `svc` | Added | `POST api/widgets` | RequireUser |' &&
    assert_contains "$output" '| `svc` | Removed | `DELETE api/widgets/{id:long}` | RequireUser |' &&
    assert_contains "$output" '| `svc` | Changed | `GET api/widgets` | RequireUser |' &&
    # Only the private helper below Get changed. It must not read as a change to Get.
    assert_not_contains "$output" '`GET api/widgets/{id:long}`'
}

test_scope_reports_policy_change() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  scope_widget_controller base
  scope_commit base
  # awk, not sed: BSD sed on macOS does not read \n or \t in a replacement.
  awk '{ print } /\[HttpDelete/ { print "\t[Authorize(Policies.RequireAdmin)]" }' \
    svc/Api/Controllers/WidgetController.cs >svc/Api/Controllers/WidgetController.cs.new
  mv svc/Api/Controllers/WidgetController.cs.new svc/Api/Controllers/WidgetController.cs
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| Policy changed | `DELETE api/widgets/{id:long}` | RequireUser -> RequireAdmin |'
}

# Deleting the first line of an action leaves no head line inside it, so its base span decides.
test_scope_marks_a_deleted_first_attribute() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  scope_widget_controller base
  awk '/\[HttpGet\("\{id:long\}"\)\]/ { print "\t[RequestSizeLimit(1024)]" } { print }' \
    svc/Api/Controllers/WidgetController.cs >svc/Api/Controllers/WidgetController.cs.new
  mv svc/Api/Controllers/WidgetController.cs.new svc/Api/Controllers/WidgetController.cs
  scope_commit base
  scope_widget_controller base
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| API endpoints |  | 1 |  |' &&
    assert_contains "$output" '| `svc` | Changed | `GET api/widgets/{id:long}` | RequireUser |'
}

# A partial class keeps its [Route] and policy in XController.cs. An action that moves to another
# file of the class is not a change; a new action there takes the class route and policy.
test_scope_reads_partial_controller_files() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p svc/Api/Controllers
  printf '%s\n' '[ApiController]' '[Authorize(Policy = Policies.RequireUser)]' '[Route("api/widgets")]' \
    'public partial class WidgetController(IWidgetService service) : ControllerBase' '{' \
    '	[HttpGet]' '	public Task<IActionResult> List(CancellationToken ct) => service.List(ct);' '' \
    '	[HttpPost]' '	public Task<IActionResult> Create(CreateWidgetRequest request, CancellationToken ct)' \
    '		=> service.Create(request, ct);' '}' >svc/Api/Controllers/WidgetController.cs
  scope_commit base

  printf '%s\n' '[ApiController]' '[Authorize(Policy = Policies.RequireUser)]' '[Route("api/widgets")]' \
    'public partial class WidgetController(IWidgetService service) : ControllerBase' '{' \
    '	[HttpGet]' '	public Task<IActionResult> List(CancellationToken ct) => service.List(ct);' '}' \
    >svc/Api/Controllers/WidgetController.cs
  printf '%s\n' 'public partial class WidgetController' '{' \
    '	[HttpPost]' '	public Task<IActionResult> Create(CreateWidgetRequest request, CancellationToken ct)' \
    '		=> service.Create(request, ct);' '' \
    '	[HttpDelete("{id:long}")]' '	public Task<IActionResult> Delete(long id, CancellationToken ct) => service.Delete(id, ct);' \
    '}' >svc/Api/Controllers/WidgetController.Command.cs
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| `svc` | Added | `DELETE api/widgets/{id:long}` | RequireUser |' &&
    assert_not_contains "$output" '`POST api/widgets`'
}

# Arguments: the head's Create message, the head's with-delete or without-delete, the endpoint that
# must read as its change, and an endpoint that must not appear. The base passes "created" and has
# Delete.
test_scope_bounds_actions() {
  local message="$1" delete="$2" expected="$3" unexpected="$4" temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  scope_controller() { # message with-delete|without-delete
    printf '%s\n' '[Route("api/widgets")]' 'public class WidgetController(IWidgetService service) : ControllerBase' '{' \
      '	[HttpPost]' '	public Task<IActionResult> Create(CreateWidgetRequest request, CancellationToken ct)' \
      '		=> service.Create(request, ct)' "			.ToActionResult(\"$1\");" ''
    if [[ "$2" == with-delete ]]; then
      printf '%s\n' '	[HttpDelete("{id:long}")]' '	public Task<IActionResult> Delete(long id, CancellationToken ct)' \
        '		=> service.Delete(id, ct).ToActionResult();' ''
    fi
    printf '%s\n' '	[HttpGet("{id:long}")]' '	public Task<IActionResult> Get(long id, CancellationToken ct)' \
      '		=> service.Get(id, ct).ToActionResult();' '}'
  }
  mkdir -p svc/Api/Controllers
  scope_controller created with-delete >svc/Api/Controllers/WidgetController.cs
  scope_commit base
  scope_controller "$message" "$delete" >svc/Api/Controllers/WidgetController.cs
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" "$expected" &&
    assert_not_contains "$output" "$unexpected"
}

# Arguments: the action that the head takes out of WidgetController.cs (post, in the middle, or
# delete, at the end), moves or removed, and a line that the report must hold. A move to
# WidgetController.Command.cs is no change. Neither a move nor a removal marks an action as changed.
test_scope_takes_out_an_action() {
  local action="$1" fate="$2" expected="$3" temp_root output kept
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  scope_action() { # get|post|delete
    case "$1" in
      get) printf '%s\n' '	[HttpGet]' '	public Task<IActionResult> List(CancellationToken ct)' '		=> service.List(ct);' ;;
      post) printf '%s\n' '	[HttpPost]' '	public Task<IActionResult> Create(CreateWidgetRequest request, CancellationToken ct)' \
        '		=> service.Create(request, ct);' ;;
      delete) printf '%s\n' '	[HttpDelete("{id:long}")]' '	public Task<IActionResult> Delete(long id, CancellationToken ct)' \
        '		=> service.Delete(id, ct);' ;;
    esac
  }
  scope_main_controller() { # actions...
    local first=1 name
    printf '%s\n' '[Route("api/widgets")]' 'public partial class WidgetController(IWidgetService service) : ControllerBase' '{'
    for name in "$@"; do
      (( first )) || printf '\n'
      first=0
      scope_action "$name"
    done
    printf '}\n'
  }
  mkdir -p svc/Api/Controllers
  scope_main_controller get post delete >svc/Api/Controllers/WidgetController.cs
  scope_commit base

  kept=(get post delete)
  kept=("${kept[@]/$action/}")
  scope_main_controller ${kept[*]} >svc/Api/Controllers/WidgetController.cs
  if [[ "$fate" == moves ]]; then
    { printf '%s\n' 'public partial class WidgetController' '{'; scope_action "$action"; printf '}\n'; } \
      >svc/Api/Controllers/WidgetController.Command.cs
  fi
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" "$expected" &&
    assert_not_contains "$output" '| Changed | `'
}

# Two services can both serve POST api/admin/sync. Removing it from one service
# is a removal, even when the other service keeps it.
test_scope_keys_endpoints_by_service() {
  local temp_root output service
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  for service in alpha beta; do
    mkdir -p "${service}/Api/Controllers"
    printf '%s\n' '[Route("api/admin")]' 'public class SyncController : ControllerBase' '{' \
      '	[HttpPost("sync")]' '	public IActionResult Sync() => Ok();' '}' >"${service}/Api/Controllers/SyncController.cs"
  done
  scope_commit base

  printf '%s\n' '[Route("api/admin")]' 'public class SyncController : ControllerBase' '{' \
    '	[HttpPost("sync")]' '	public IActionResult Sync() => NoContent();' '}' >alpha/Api/Controllers/SyncController.cs
  printf '%s\n' '[Route("api/admin")]' 'public class SyncController : ControllerBase' '{' '}' \
    >beta/Api/Controllers/SyncController.cs
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| API endpoints |  | 1 | 1 |' &&
    assert_contains "$output" '| `alpha` | Changed | `POST api/admin/sync` | none |' &&
    assert_contains "$output" '| `beta` | Removed | `POST api/admin/sync` | none |'
}

# Directory.Packages.props holds the central NuGet versions. A list stops at 15 rows with a count of
# the rest.
test_scope_lists_central_versions_and_cuts_long_lists() {
  local temp_root output keys i
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf '%s\n' '<Project>' '  <ItemGroup>' '    <PackageVersion Include="CsvHelper" Version="30.0.1" />' \
    '  </ItemGroup>' '</Project>' >Directory.Packages.props
  scope_commit base

  printf '%s\n' '<Project>' '  <ItemGroup>' '    <PackageVersion Include="CsvHelper" Version="33.0.1" />' \
    '  </ItemGroup>' '</Project>' >Directory.Packages.props
  mkdir -p svc/Api
  # 20 files of 8 long keys: the list stops at 15 rows.
  keys=$(printf '"AVeryLongConfigurationKeyForTheWidgetService%s": 0, ' a b c d e f g h)
  for i in $(seq 1 20); do printf '{ %s"End": 0 }\n' "$keys" >"svc/Api/appsettings.W${i}.json"; done
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '- NuGet: changed `CsvHelper` 30.0.1 -> 33.0.1' &&
    assert_contains "$output" '- and 5 more'
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
run_test 'scope classifies added, removed, and changed endpoints' test_scope_classifies_endpoint_changes
run_test 'scope reports an endpoint policy change' test_scope_reports_policy_change
run_test 'scope: deleting the first attribute of an action changes it' test_scope_marks_a_deleted_first_attribute
run_test 'scope reads partial controller files' test_scope_reads_partial_controller_files
run_test 'scope: deleting an action does not mark its neighbours' test_scope_bounds_actions created without-delete '| Removed | `DELETE api/widgets/{id:long}` |' '| Changed | `'
run_test 'scope: a wrapped expression body belongs to its action' test_scope_bounds_actions 'widget created' with-delete '| Changed | `POST api/widgets` |' '`DELETE'
run_test 'scope: moving a middle action to a partial file is no change' test_scope_takes_out_an_action post moves 'No tangible change'
run_test 'scope: removing the final action marks no other action' test_scope_takes_out_an_action delete removed '| Removed | `DELETE api/widgets/{id:long}` | none |'
run_test 'scope: moving the final action to a partial file is no change' test_scope_takes_out_an_action delete moves 'No tangible change'
run_test 'scope keys endpoints by service' test_scope_keys_endpoints_by_service
run_test 'scope lists central package versions and cuts long lists' test_scope_lists_central_versions_and_cuts_long_lists
