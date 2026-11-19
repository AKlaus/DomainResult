# Library Rules

Applies to the two NuGet packages in `src/`, their tests in `tests/`, and the apps in `samples/`.

## Packages and dependencies

| Package ID | Project | Assembly and namespace | Targets |
|---|---|---|---|
| `DomainResult.Common` | `src/Common` | `DomainResults.Common` | `netstandard2.1` |
| `DomainResult` | `src/Mvc` | `DomainResults.Mvc` | `net7.0`, `net8.0`, `net9.0`, `net10.0` |

- `DomainResult.Common` is for the consumer's domain layer. It must never reference
  `Microsoft.AspNetCore.*`, directly or through another package. This independence is the reason
  the library exists.
- `DomainResult` converts an `IDomainResult` to `IActionResult`, `ActionResult<T>`, and `IResult`.
  It references `DomainResult.Common` and the `Microsoft.AspNetCore.App` framework.
- The dependency direction is `samples` and `tests` -> `src/Mvc` -> `src/Common`. Nothing in `src/`
  references `samples/` or `tests/`.
- Each new `PackageReference` in `src/` becomes a dependency of every consumer. Avoid one. If a PR
  adds one, its description names the package and the reason.
- Code must compile for every target framework. Guard a framework-specific API with
  `#if NET8_0_OR_GREATER` (or the matching symbol), never by dropping a target.
- Adding or removing a target framework touches the project files, both `.github/workflows/*.yml`
  files, and the target list in `README.md`. It is its own PR.

## Public API

Every `public` type and member is a contract with the consumers of a published package.

- Versions follow Semantic Versioning. MinVer computes them from Git tags (`v<major>.<minor>.<patch>`).
- These changes break consumers and need a major version: a removed or renamed public member, a
  changed signature, a changed default in `HttpCodeConvention`, and a changed mapping from
  `DomainOperationStatus` to an HTTP code. The PR description marks each one **Breaking**.
- Prefer an additive change. Add an overload rather than change a signature. A new optional
  parameter on an existing public method breaks binary compatibility, so add an overload instead.
- Keep a member `internal` or `private` unless consumers need it.
- Do not mark a member `[Obsolete]` and remove it in the same release. Remove it in the next major version.

### Parity across result shapes

The public surface is a matrix. A new status or behaviour reaches every shape where it applies,
or the PR description says which shape it leaves out and why.

- **Result shapes** (`DomainResult.Common`): `IDomainResult`, `IDomainResult<T>`, and
  `(T, IDomainResult)`, each also wrapped in a `Task`. Factory methods exist as static members of
  `IDomainResult` and of `DomainResult`, with a `*Task()` variant of each.
- **Conversions** (`DomainResult`): `ToActionResult()`, `ToActionResultOfT()`,
  `ToCustomActionResult()`, and `ToCustomActionResultOfT()` for controllers, and `ToResult()` and
  `ToCustomResult()` for the minimal API. `ToActionResult()` and `ToResult()` take every result
  shape above. The `*OfT` and `Custom*` conversions take the shapes that carry a value.
- A new `DomainOperationStatus` value needs all of these in one PR: its factory methods, its HTTP
  code and title in `HttpCodeConvention`, its mapping in the `IActionResult` and the `IResult`
  converters, a sample in `samples/WebApi` and in `samples/WebApiMinimal`, tests for each shape,
  and a row in the status table of `README.md`.

## Code layout

- Folders group the converters by output type: `src/Mvc/ActionResult/` and `src/Mvc/IResult/`.
  Within a folder, one file holds one conversion, such as `DomainResultTo200OkResult.cs`.
- Public extension methods stay in the package's root namespace (`DomainResults.Common` or
  `DomainResults.Mvc`) whatever their folder, so one `using` brings in all of them. Such a file
  keeps the `// ReSharper disable once CheckNamespace` comment. This overrides the rule that the
  namespace matches the folder.
- A `static partial class`, such as `DomainResultExtensions`, spans several files. Each file
  is named after the conversion it holds, not after the class.
- Within a class, order members as: public members -> private helper methods -> private types.
- Follow `.editorconfig`: tabs, CRLF line endings, and `System` usings first.

## Error handling

- Return an `IDomainResult` for an expected failure. Throw only for a programming error, such as
  a `null` argument that the signature forbids, or through `ThrowIfNoSuccess()` when the consumer
  asks for an exception.
- An error response is a `ProblemDetails` (RFC 9457, which replaced RFC 7807). Its status and title come from
  `HttpCodeConvention`, and its `detail` comes from `IDomainResult.Errors`.

## XML documentation

- `GenerateDocumentationFile` is on for `src/`, and the docs ship in the package. Document every
  public type and member.
- Follow the **Communication style** section of `AGENTS.md`.
- Preserve external API and BCL spelling (`Color`, `Serialize`) within the API-facing abstraction;
  otherwise use British English.
- The `<summary>` is a short, single statement. Put caveats and implementation notes in `<remarks>`.
- For a factory method, the `<summary>` names the `DomainOperationStatus` it returns and the HTTP
  code it converts to by default, as `IDomainResult.cs` does.
- Include `<param>` and `<returns>` only when they add value. A method's `<param>` tags are
  all-or-nothing: documenting only some of them raises a compiler warning.
- Document inherited contracts once. Implementations use `<inheritdoc/>` and add `<remarks>` only
  for contract-relevant differences.

## README and samples

`README.md` is the package documentation on nuget.org (`PackageReadmeFile`), so it ships with
every release.

- A PR that adds or changes public behaviour updates `README.md` in the same PR: the examples,
  the method lists, and the status table.
- Code in `README.md` compiles against the current API. Check each changed snippet against the
  source.
- `samples/` shows each status through both a controller (`samples/WebApi/Controllers/`) and a
  minimal-API route (`samples/WebApiMinimal/Routes/`). Keep the two in step.

## Testing

- All tests live in `tests/DomainResults.Tests`. A test file mirrors its source folder:
  `tests/DomainResults.Tests/Common/` for `src/Common`, and `tests/DomainResults.Tests/Mvc/` for `src/Mvc`.
- The test project runs on every target framework, and CI runs each one. Run at least the
  latest framework locally: `dotnet test --framework net10.0`.
- A new test method is named `<Method>_<Scenario>_<ExpectedResult>`, and marks its `GIVEN`,
  `WHEN`, and `THEN` sections with comments. When you edit an older test file, follow its existing
  names; rename its tests only in a separate refactoring PR.
- Each test case, including each `[Theory]` row, covers a branch, boundary, or contract that no
  other case covers. Cases that differ only in input and expected result are one `[Theory]`.
- Cover each result shape and each conversion that the change touches, as **Parity across result
  shapes** lists.
- Assert the specific status, HTTP code, and `ProblemDetails` fields, not only failure.
- Do not mock the library's own types. Test a conversion through its public extension method.
- Do not test input that production cannot produce.

## Pull request slices

Follow the **Pull request scope** section of `AGENTS.md`. These parts of a change must ship in one
PR, because each is incomplete without the others:

- A public API change, its tests, its `README.md` update, and its samples.
- A new `DomainOperationStatus` value and every item that **Parity across result shapes** lists.

A dependency upgrade is its own PR. So is a change of target frameworks.
