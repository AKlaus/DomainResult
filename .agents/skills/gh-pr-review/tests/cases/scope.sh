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

# Areas names the top-level folders a PR touches. Experiments and the root workspace are not areas,
# and the table does not count projects.
test_scope_lists_areas() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  printf '{}\n' >package.json
  scope_lines svc/Api/Api.csproj 3
  scope_lines svc/Api/Controllers/WidgetController.cs 10
  scope_lines svc/Domain/Domain.csproj 3
  scope_lines web/package.json 3
  scope_lines web/src/index.ts 10
  scope_commit base

  printf 'line 11\n' >>svc/Api/Controllers/WidgetController.cs
  printf 'line 11\n' >>web/src/index.ts
  scope_lines lib/Store/Store.csproj 3
  scope_lines lib/Store/Store.cs 10
  command git rm -rq svc/Domain
  scope_lines sandbox/Viewer/package.json 3
  scope_lines sandbox/Viewer/src/main.ts 10
  scope_lines svc/Tests/Api.ScratchPad/Api.ScratchPad.csproj 3
  scope_lines svc/Tests/Api.ScratchPad/Program.cs 10
  printf '{ "private": true }\n' >package.json
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '**Areas**: `lib`, `svc`, `web`'$'\n' &&
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
  # react sits in two kinds, and the head removes only its peer entry. Each kind compares on its
  # own, so the kept entry prints nothing and the removed one prints a row.
  mkdir -p web
  printf '{ "devDependencies": { "react": "^18.2.0" }, "peerDependencies": { "react": "^18.2.0" } }\n' \
    >web/package.json
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
  printf '{ "devDependencies": { "react": "^18.2.0", "vitest": "3.2.6" } }\n' \
    >web/package.json
  printf '%s\n' '<Project>' '  <ItemGroup>' \
    '    <PackageReference Include="MinVer" Version="6.1.0" PrivateAssets="All" />' \
    '  </ItemGroup>' '</Project>' >src/Directory.Build.props
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| Project references | 1 |  |  |' &&
    assert_contains "$output" '| Package dependencies | 1 | 1 | 2 |' &&
    assert_contains "$output" '- `src/Directory.Build.props` package reference: changed `MinVer` 4.2.0 -> 6.1.0' &&
    assert_contains "$output" '| Configuration keys | 1 |  |  |' &&
    assert_contains "$output" '- `svc/Api` project reference: added `lib/ObjectStore`' &&
    assert_contains "$output" '- `svc/Api` package reference: removed `CsvHelper`' &&
    assert_contains "$output" '- npm `web`: added `vitest (devDependencies)` 3.2.6' &&
    assert_contains "$output" '- npm `web`: removed `react (peerDependencies)` ^18.2.0' &&
    assert_not_contains "$output" 'react (devDependencies)' &&
    assert_contains "$output" '- `svc/Api/appsettings.json`: added `Widgets:MaxPages`' &&
    # A changed value is not a new key, and values never print: they can hold secrets.
    assert_not_contains "$output" 'PageSize' &&
    assert_not_contains "$output" 'secret'
}

test_scope_lists_routes_from_routing_files_only() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p web/src/modules/Widgets
  printf '%s\n' "export const routes = [{ path: '/widgets', element: <Widgets /> }];" >web/src/AppRouteElements.tsx
  printf '%s\n' "export const crumbs = [{ path: '/', label: 'Home' }];" >web/src/modules/Widgets/constants.ts
  scope_commit base

  printf '%s\n' "export const routes = [{ path: '/widgets/:id', element: <Widget /> }];" >web/src/AppRouteElements.tsx
  printf '%s\n' "export const crumbs = [{ path: '/widgets', label: 'Widgets' }];" >web/src/modules/Widgets/constants.ts
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| `web` | Added | `/widgets/:id` |' &&
    assert_contains "$output" '| `web` | Removed | `/widgets` |' &&
    # constants.ts holds breadcrumbs, not routes; its path change is not a route change.
    assert_not_contains "$output" '`/`'
}

# A deployable service keeps its pins in requirements/ and its deployment files in its own devops/.
test_scope_lists_service_requirements_and_devops_files() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p svc/app/requirements svc/devops
  printf -- '-r requirements/prod.txt\n' >svc/app/requirements.txt
  printf 'numpy==1.26.0\n' >svc/app/requirements/prod.txt
  scope_commit base

  printf 'numpy==2.1.0\nonnxruntime==1.20.0\n' >svc/app/requirements/prod.txt
  printf 'kind: Deployment\n' >svc/devops/kubernetes.yaml
  mkdir -p .github/workflows
  printf 'name: CI\n' >.github/workflows/ci.yml
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '- Python `svc/app/requirements/prod.txt`: added `onnxruntime` ==1.20.0' &&
    assert_contains "$output" '- Python `svc/app/requirements/prod.txt`: changed `numpy` ==1.26.0 -> ==2.1.0' &&
    assert_contains "$output" '- added `svc/devops/kubernetes.yaml`' &&
    assert_contains "$output" '- added `.github/workflows/ci.yml`'
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

# analytics and downhole-data-service both serve POST api/admin/sync. Removing it from one service
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

test_scope_lists_contracts_components_and_migrations() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p web/preview/harness web/src/pages svc/SDK.DTOs/Request
  printf '{ "name": "web" }\n' >web/package.json
  printf 'export const Harness = () => null;\n' >web/preview/harness/index.tsx
  printf 'export const GadgetPanel = () => null;\n' >web/src/pages/GadgetPanel.tsx
  printf '%s\n' 'namespace Svc.Dtos;' '' 'public record UpdateGadgetRequest(' '	int Id,' '	double From,' '	double To);' \
    >svc/SDK.DTOs/Request/UpdateGadgetRequest.cs
  scope_commit base

  mkdir -p svc/Database/Migrations/SQL svc/Domain/Queries/SQL web/src/api-models web/src/pages/Gadgets lib/Tests/Gen.Tests
  printf 'public record WidgetRequest(int Id);\n' >svc/SDK.DTOs/Request/WidgetRequest.cs
  printf 'export interface Widget { id: number }\n' >web/src/api-models/widget.gen.ts
  printf 'export interface Fixture { id: number }\n' >lib/Tests/Gen.Tests/fixture.gen.ts
  printf 'export const Harness = () => <div />;\n' >web/preview/harness/index.tsx
  mv web/src/pages/GadgetPanel.tsx web/src/pages/Gadgets/GadgetPanel.tsx
  rm svc/SDK.DTOs/Request/UpdateGadgetRequest.cs
  printf '%s\n' 'namespace Svc.Dtos;' '' 'public record UpdateGadgetPositionRequest(' '	int Id,' '	double From,' '	double To);' \
    >svc/SDK.DTOs/Request/UpdateGadgetPositionRequest.cs
  printf 'public class AddWidget : Migration { }\n' >svc/Database/Migrations/20261028000000_AddWidget.cs
  printf 'CREATE TABLE widget (id int);\n' >svc/Database/Migrations/SQL/20261028000000_AddWidget.sql
  printf 'SELECT id FROM widget;\n' >svc/Domain/Queries/SQL/GetWidget.sql
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| API contracts and clients | 2 | 1 |  |' &&
    assert_contains "$output" '- `svc/SDK.DTOs`: added `WidgetRequest`' &&
    assert_contains "$output" '- `web/src/api-models`: added (generated) `widget`' &&
    # Neither an API caller nor a user sees a type or file name. A renamed DTO with edits is a
    # modified one, and a component moved without edits is no change.
    assert_contains "$output" '- `svc/SDK.DTOs`: modified `UpdateGadgetPositionRequest`' &&
    assert_not_contains "$output" 'UpdateGadgetRequest' &&
    assert_not_contains "$output" 'GadgetPanel' &&
    assert_contains "$output" '| UI components |  | 1 |  |' &&
    # A generated file in a test project is a fixture, not a contract.
    assert_not_contains "$output" 'fixture' &&
    # A component outside src/ is named from its project folder.
    assert_contains "$output" '| `web` | 1 | small |' &&
    # The Database list names tables and functions, not scripts or migration classes.
    assert_contains "$output" '| Database | 1 |  |  |' &&
    assert_contains "$output" '- `svc/Database`: added table `widget`'$'\n' &&
    assert_not_contains "$output" '20261028000000_AddWidget' &&
    # A query beside its service is code, not a migration.
    assert_not_contains "$output" 'GetWidget'
}

# Each dialect names its objects differently. A function body and the migration bookkeeping table
# are not changes; an alias resolves to its table; a function that leaves CustomFunctions/ is removed.
test_scope_names_database_objects() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p svc/Database/CustomFunctions svc/Database/Migrations/SQL
  printf '<Project Sdk="Microsoft.NET.Sdk" />\n' >svc/Database/Database.csproj
  printf '%s\n' 'CREATE OR REPLACE FUNCTION svc.get_widget(p_id int) RETURNS int AS $$' 'BEGIN' \
    '    UPDATE gadget SET hits = hits + 1;' '    RETURN 1;' 'END' '$$ LANGUAGE plpgsql;' \
    >svc/Database/CustomFunctions/get_widget.pgsql
  printf '%s\n' 'CREATE OR REPLACE FUNCTION svc.old_widget() RETURNS int AS $$ SELECT 1 $$ LANGUAGE sql;' \
    >svc/Database/CustomFunctions/old_widget.pgsql
  printf '%s\n' "CREATE TYPE widget_kind AS ENUM ('a');" \
    'CREATE FUNCTION svc.count_widgets() RETURNS int AS $$ SELECT 1 $$ LANGUAGE sql;' \
    >svc/Database/Migrations/0001_Init.pgsql
  scope_commit base

  printf '%s\n' 'CREATE OR REPLACE FUNCTION svc.get_widget(p_id int) RETURNS int AS $$' 'BEGIN' \
    '    UPDATE gadget SET hits = hits + 1;' '    RETURN 2;' 'END' '$$ LANGUAGE plpgsql;' \
    >svc/Database/CustomFunctions/get_widget.pgsql
  rm svc/Database/CustomFunctions/old_widget.pgsql
  printf '%s\n' 'DO $$' 'BEGIN' "    ALTER TYPE widget_kind ADD VALUE 'b';" \
    "    INSERT INTO \"_migrations_history\"(migration_id) VALUES ('0002');" 'END $$;' \
    >svc/Database/Migrations/0002_WidgetKind.pgsql
  printf '%s\n' 'CREATE TABLE dbo.Gadgets (Id INT);' 'CREATE TABLE [dbo].[Gizmos] ([Id] INT);' 'GO' \
    "UPDATE g SET Name = N'x' FROM dbo.Widgets g WHERE g.Id = 1;" 'GO' \
    'CREATE PROCEDURE dbo.TouchGadgets AS' 'BEGIN' '    DELETE FROM dbo.Sprockets;' 'END' 'GO' \
    'CREATE VIEW dbo.ActiveGadgets AS SELECT Id FROM dbo.Gadgets;' 'GO' \
    >svc/Database/Migrations/SQL/0003_Gadgets.sql
  # A drop and re-create of an existing routine changes it. An altered table that is dropped is
  # removed.
  printf '%s\n' 'DROP FUNCTION IF EXISTS svc.count_widgets();' \
    'CREATE FUNCTION svc.count_widgets() RETURNS int AS $$ SELECT 2 $$ LANGUAGE sql;' \
    'ALTER TABLE cogs ADD COLUMN size int;' 'DROP TABLE cogs;' \
    >svc/Database/Migrations/0004_CountWidgets.pgsql
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  # Tables, views, functions, procedures, and types count together, once per object.
  assert_contains "$output" '| Database | 4 | 4 | 2 |' &&
    assert_contains "$output" '- `svc/Database`: added procedure `TouchGadgets`; tables `Gadgets`, `Gizmos`; view `ActiveGadgets`' &&
    assert_contains "$output" '- `svc/Database`: changed functions `svc.count_widgets`, `svc.get_widget`; type `widget_kind`' &&
    assert_contains "$output" '- `svc/Database`: changed data in table `Widgets`' &&
    assert_contains "$output" '- `svc/Database`: removed function `svc.old_widget`; table `cogs`' &&
    assert_not_contains "$output" '`gadget`' &&
    assert_not_contains "$output" 'Sprockets' &&
    assert_not_contains "$output" '_migrations_history' &&
    assert_not_contains "$output" '`g`'
}

# Each app gets one row: how many component folders changed, and one approximate size for all of
# them together, from the changed lines of their .tsx, .ts, and style files. A folder of helpers
# without a .tsx file is no component.
test_scope_sizes_components() {
  local temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p web admin
  printf '{ "name": "web" }\n' >web/package.json
  printf '{ "name": "admin" }\n' >admin/package.json
  scope_lines web/src/pages/A/A.tsx 10
  scope_lines web/src/pages/B/B.tsx 10
  scope_lines web/src/helpers/format.ts 10
  scope_lines admin/src/Big/Big.tsx 10
  scope_lines admin/src/Big/useBig.ts 10
  scope_commit base

  # web: 30 + 25 lines, each small, are average together. admin: 150 + 60 lines in one folder.
  scope_lines web/src/pages/A/A.tsx 40
  scope_lines web/src/pages/B/B.tsx 35
  scope_lines web/src/helpers/format.ts 20
  scope_lines admin/src/Big/Big.tsx 160
  scope_lines admin/src/Big/useBig.ts 70
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" '| UI components |  | 3 |  |' &&
    assert_contains "$output" '| App | Components | Size |
|---|---|---|
| `admin` | 1 | big |
| `web` | 2 | average |' &&
    assert_not_contains "$output" 'pages/'
}

# Arguments: how the head changes web/src/Panel/, and the UI components row it must report. A
# component is added or removed when it appears or disappears, not when one of its files does.
test_scope_names_component_status_by_existence() {
  local change="$1" expected="$2" temp_root output
  temp_root=$(mktemp -d)
  trap "rm -rf '$temp_root'" EXIT
  scope_init_repo "$temp_root"

  mkdir -p web/src/Panel
  printf '{ "name": "web" }\n' >web/package.json
  printf 'export const Panel = () => null;\n' >web/src/Panel/index.tsx
  printf 'export const legacyLabel = 1;\n' >web/src/Panel/legacyLabel.ts
  scope_commit base

  case "$change" in
    add-helper) printf 'export const newLabel = 1;\n' >web/src/Panel/newLabel.ts ;;
    delete-helper) rm web/src/Panel/legacyLabel.ts ;;
    add-component) mkdir -p web/src/Card && printf 'export const Card = () => null;\n' >web/src/Card/index.tsx ;;
    delete-component) rm -r web/src/Panel ;;
  esac
  scope_commit head

  output=$(scope_run base head 2>&1) || {
    printf '%s\n' "$output" >&2
    return 1
  }

  assert_contains "$output" "$expected"
}

# Directory.Packages.props holds the central NuGet versions. A list stops at 15 rows with a count of
# the rest, and a report long enough to near the description limit warns on stderr.
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
run_test 'scope budget: generated code and auxiliary files do not count' test_scope_budget 'Budget: within the 500-line' svc/Domain/Widget.cs:500 web/src/api-client/widget-client.gen.ts:5000 web/src/__snapshots__/Widget.test.tsx.snap:900 yarn.lock:5000 all.sln:900
run_test 'scope budget: experiments, agent tooling and docs do not count' test_scope_budget 'Budget: within the 500-line' svc/Domain/Widget.cs:500 sandbox/Viewer/src/main.ts:900 svc/Tests/OnBoarding.ScratchPad/Run.cs:900 .agents/skills/widgets/scripts/run.sh:900 AGENTS.md:900 svc/Tests/Domain.Tests/README.md:900 docs/guide.txt:900
run_test 'scope budget: migration scripts and migration classes count' test_scope_budget 'Budget: over the 500-line' db/Migrations/SQL/20261021000000_AddWidget.sql:250 db/Migrations/20261021000000_AddWidget.cs:251
run_test 'scope budget: added and deleted comment lines do not count' test_scope_budget_edit 'Budget: within the 500-line' \
  'svc/Domain/Widget.cs|500||line %d' \
  'svc/Domain/Widget Notes.cs|100||// note %d' \
  'svc/Domain/Legacy.cs|100|/* legacy %d */|' \
  'web/src/widget.ts|100||/**\n * note %d\n */' \
  'db/Migrations/SQL/20261021000000_AddWidget.sql|100||-- note %d' \
  'svc/Api/Api.csproj|100||<!-- note %d -->' \
  'tools/import.py|100||# note %d'
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
run_test 'scope budget: an upper-case keyword swap counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'db/Migrations/SQL/20261021000000_Widget.sql|251|SELECT %d WHERE a AND b;|SELECT %d WHERE a OR b;'
run_test 'scope budget: a version change inside a name counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d targets Net8|line %d targets net9'
run_test 'scope budget: a replacement whose old identifier remains in the file counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses Widget\nkeep %d Widget|line %d uses Gadget\nkeep %d Widget'
run_test 'scope budget: a replacement by a name the old file used counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses orderedFields\nkeep %d fields|line %d uses fields\nkeep %d fields'
run_test 'scope budget: a rename with another edit counts' test_scope_budget_edit 'Budget: over the 500-line' \
  'svc/Domain/Widget.cs|251|line %d uses Widget|line %d uses Gadget()'
run_test 'scope budget: a test under Migrations counts as a test' test_scope_budget 'Budget: over the ceiling' svc/Domain/Widget.cs:100 svc/Tests/Domain.Tests/Migrations/WidgetMigrationTests.cs:1901
run_test 'scope counts a move to another folder by its edits' test_scope_counts_a_move_by_its_edits lib/Widgets/BigWidget.cs lib
run_test 'scope counts a move inside a folder by its edits' test_scope_counts_a_move_by_its_edits svc/Widgets/BigWidget.cs svc
run_test 'scope ignores commits on base after the fork' test_scope_ignores_commits_on_base_after_the_fork
run_test 'scope reports unrelated histories' test_scope_reports_unrelated_histories
run_test 'scope lists areas, not projects' test_scope_lists_areas
run_test 'scope lists dependency and configuration key changes' test_scope_lists_dependencies_and_configuration_keys
run_test 'scope lists routes from routing files only' test_scope_lists_routes_from_routing_files_only
run_test 'scope lists service requirements and devops files' test_scope_lists_service_requirements_and_devops_files
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
run_test 'scope lists contracts, components, and migrations' test_scope_lists_contracts_components_and_migrations
run_test 'scope names the database objects that a script changes' test_scope_names_database_objects
run_test 'scope sizes each changed component folder' test_scope_sizes_components
run_test 'scope: a helper added to a component modifies it' test_scope_names_component_status_by_existence add-helper '| UI components |  | 1 |  |'
run_test 'scope: a helper deleted from a component modifies it' test_scope_names_component_status_by_existence delete-helper '| UI components |  | 1 |  |'
run_test 'scope: a new component folder is added' test_scope_names_component_status_by_existence add-component '| UI components | 1 |  |  |'
run_test 'scope: a deleted component folder is removed' test_scope_names_component_status_by_existence delete-component '| UI components |  |  | 1 |'
run_test 'scope lists central package versions and cuts long lists' test_scope_lists_central_versions_and_cuts_long_lists
