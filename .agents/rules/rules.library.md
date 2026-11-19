# Library Rules

Applies to `src/`, `tests/`, and `samples/`.

## Packages and dependencies

| Package ID | Project | Assembly and namespace |
|---|---|---|
| `DomainResult.Common` | `src/Common` | `DomainResults.Common` |
| `DomainResult` | `src/Mvc` | `DomainResults.Mvc` |

- `DomainResult.Common` must never reference `Microsoft.AspNetCore.*`, directly or through
  another package.
- Avoid a new `PackageReference` in `src/`. If a PR adds one, its description names the package
  and the reason.
- Never drop a target framework to make code compile.
- A change of target frameworks is its own PR. It touches the project files, both workflow files,
  and the target list in `README.md`.

## Public API

- MinVer computes versions from `v<major>.<minor>.<patch>` tags.
- Besides a signature change, a changed default in `HttpCodeConvention` and a changed
  status-to-HTTP-code mapping are breaking. The PR description marks each breaking change
  **Breaking**.
- Remove an `[Obsolete]` member no earlier than the next major version.

### Parity across result shapes

`README.md` lists the result shapes, under the `'DomainResult.Common' package` heading, and the
conversions, under the `'DomainResult' package` heading.

- A new status or behaviour reaches every shape and conversion where it applies, or the PR
  description says which one it leaves out and why.
- A new `DomainOperationStatus` value needs all of these: its factory methods, its HTTP code and
  title in `HttpCodeConvention`, its mapping in the `IActionResult` and the `IResult` converters,
  a sample in `samples/WebApi` and in `samples/WebApiMinimal`, tests for each shape, and a row in
  the status table of `README.md`.

## Code layout

- Each conversion has its own file in `src/Mvc/ActionResult/` or `src/Mvc/IResult/`, such as
  `DomainResultTo200OkResult.cs`. A file of the `static partial class DomainResultExtensions` is
  named after the conversion it holds.
- Public extension methods stay in the package's root namespace whatever their folder, so one
  `using` brings in all of them. Such a file keeps `// ReSharper disable once CheckNamespace`.
- Order members as: public members, private helper methods, private types.
- C# files use tabs and CRLF line endings, as `.editorconfig` sets.

## XML documentation

- Document every public type and member: the docs ship in the package. `Directory.Build.props`
  suppresses CS1591, so the build does not report a missing doc comment.
- The `<summary>` is one short statement. Caveats go in `<remarks>`.
- A factory method's `<summary>` names the `DomainOperationStatus` it returns and its default
  HTTP code, as `IDomainResult.cs` does. Docs in `src/Common` say nothing else about ASP.NET Core.
- Add `<param>` and `<returns>` only when they add value. A method's `<param>` tags cover every
  parameter or none.
- Implementations use `<inheritdoc/>`, with `<remarks>` only for contract-relevant differences.

## README and samples

- `README.md` ships on nuget.org as the package documentation. When public behaviour changes,
  update its examples, method lists, and status table. Each changed snippet must compile against
  the current API.
- `samples/` shows each status through a controller (`samples/WebApi/Controllers/`) and a
  minimal-API route (`samples/WebApiMinimal/Routes/`). Keep the two in step.

## Testing

- Test files mirror the source folders: `tests/DomainResults.Tests/Common/` and
  `tests/DomainResults.Tests/Mvc/`.
- Run at least `dotnet test --framework net10.0` locally. CI runs every target framework.
- A new test method is named `<Method>_<Scenario>_<ExpectedResult>`, with `GIVEN`, `WHEN`, and
  `THEN` comments. In an older test file, follow its existing names. Rename them only in a
  separate refactoring PR.
- Each test case, including each `[Theory]` row, covers a branch, boundary, or contract that no
  other case covers. Cases that differ only in input and expected result are one `[Theory]`.
- Cover each result shape and conversion that the change touches.
- Assert the specific status, HTTP code, and `ProblemDetails` fields.
- Test a conversion through its public extension method. Do not mock the library's own types.
- Do not test input that production cannot produce.

## Pull request slices

These ship in one PR:

- A public API change, its `README.md` update, and its samples.
- A new `DomainOperationStatus` value and every item that **Parity across result shapes** lists.
