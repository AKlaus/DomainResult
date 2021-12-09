# DomainResult

**NuGet for decoupling domain operation results from ActionResult-based types of ASP.NET Web API**

![CI](https://github.com/AKlaus/DomainResult/workflows/CI/badge.svg)
[![Test Coverage](https://coveralls.io/repos/github/AKlaus/DomainResult/badge.svg?branch=master)](https://coveralls.io/github/AKlaus/DomainResult?branch=master)
[![DomainResult NuGet version](https://img.shields.io/nuget/v/DomainResult.svg?style=flat&label=nuget%3A%20DomainResult)](https://www.nuget.org/packages/DomainResult)
[![DomainResult.Common NuGet version](https://img.shields.io/nuget/v/DomainResult.Common.svg?style=flat&label=nuget%3A%20DomainResult.Common)](https://www.nuget.org/packages/DomainResult.Common)
<br/>

Two tiny NuGet packages addressing challenges in the [ASP.NET Web API](https://dotnet.microsoft.com/apps/aspnet/apis) realm posed by separation of the _Domain Layer_ (aka _Business Layer_) from the _Application Layer_:

- eliminating dependency on _Microsoft.AspNetCore.Mvc_ (and [IActionResult](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.iactionresult) in particular) in the _Domain Layer_ (usually a separate project);
- mapping various of responses from the _Domain Layer_ to appropriate [ActionResult](https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types)-related type.

### Content:

- [Basic use-case](#basic-use-case)
- [Quick start](#quick-start)
- ['DomainResult.Common' package. Returning result from Domain Layer method](#domainresultcommon-package-returning-result-from-domain-layer-method)
  - [Examples](#examples)
- ['DomainResult' package. Conversion to ActionResult](#domainresult-package-conversion-to-actionresult)
  - [Examples](#basic-examples)
- [Custom Problem Details output](#custom-problem-details-output)
  - [Custom ActionResult response for 2xx HTTP codes](#custom-actionresult-response-for-2xx-http-codes)
  - [Custom error handling](#custom-error-handling)
- [Alternative solutions](#alternative-solutions)
  - [Why not FluentResults?](#why-not-fluentresults)
  - [Why not Hellang's ProblemDetails?](#why-not-fluentresults)

## Basic use-case

For a _Domain Layer_ method like this:

```cs
public async Task<(InvoiceResponseDto, IDomainResult)> GetInvoice(int invoiceId)
{
    if (invoiceId < 0)
        // Returns a validation error
        return IDomainResult.Failed<InvoiceResponseDto>("Try harder");

    var invoice = await DataContext.Invoices.FindAsync(invoiceId);

    if (invoice == null)
        // Returns a Not Found response
        return IDomainResult.NotFound<InvoiceResponseDto>();

    // Returns the invoice
    return IDomainResult.Success(invoice);
}
```

or if you're against [ValueTuple](https://docs.microsoft.com/en-us/dotnet/api/system.valuetuple) or static methods on interfaces (it's [new in C# 8](https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/default-interface-methods-versions#provide-parameterization)), then a more traditional method signature:

```cs
public async Task<IDomainResult<InvoiceResponseDto>> GetInvoice(int invoiceId)
{
    if (invoiceId < 0)
        // Returns a validation error
        return DomainResult.Failed<InvoiceResponseDto>("Try harder");

    var invoice = await DataContext.Invoices.FindAsync(invoiceId);

    if (invoice == null)
        // Returns a Not Found response
        return DomainResult.NotFound<InvoiceResponseDto>();

    // Returns the invoice
    return DomainResult.Success(invoice);
}
```

The _Web API_ controller method would look like:

```cs
[ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public Task<IActionResult> GetInvoice()
{
    return _service.GetInvoice().ToActionResult();
}
```

or leverage [ActionResult&lt;T&gt;](https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types#actionresultt-type)

```cs
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public Task<ActionResult<InvoiceResponseDto>> GetInvoice()
{
    return _service.GetInvoice().ToActionResultOfT();
}
```

The above returns:

- HTTP code `200 OK` along with an instance of `InvoiceResponseDto` on successful executions.
- Non-2xx codes wrapped in [ProblemDetails](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) (as per [RFC 7807](https://tools.ietf.org/html/rfc7807)):
  - HTTP code `400 Bad Request` with a message "_Try harder_" when the invoice ID < 1 (the HTTP code can be configured to `422 Unprocessable Entity`).
  - HTTP code `404 Not Found` for incorrect invoice IDs.

## Quick start

- Install [DomainResult](https://www.nuget.org/packages/DomainResult) NuGet package for the _Web API_ project.
- Install [DomainResult.Common](https://www.nuget.org/packages/DomainResult.Common) NuGet package for the _Domain Layer_ (aka _Business Layer_) projects. If the _Domain Layer_ is inside the _Web API_ project, then skip this step.
- Follow the documentation below, `samples` in the repo and common sense.

The library targets `.NET Core 3.1`, `.NET 5.0` and `.NET 6.0`.

## 'DomainResult.Common' package. Returning result from Domain Layer method

A tiny package with no dependency on `Microsoft.AspNetCore.*` namespaces that provides:

- data types for returning from domain operations (wraps up the returned value and adds operation status with error messages if applicable);
- extension methods to effortlessly form the desired response.

It's built around `IDomainResult` interface that has 3 properties:

```cs
IReadOnlyCollection<string> Errors { get; } // Collection of error messages if any
bool IsSuccess { get; }                     // Flag, whether the current status is successful or not
DomainOperationStatus Status { get; }       // Current status of the domain operation: Success, Failed, NotFound, Unauthorized, etc.
```

And `IDomainResult<T>` interface that also adds

```cs
// Value returned by the domain operation
T Value { get; }
```

It has **50+ static extension methods** to return a successful or unsuccessful result from the domain method with one of the following types:

| Returned type        | Returned type wrapped in `Task` |
| -------------------- | ------------------------------- |
| `IDomainResult`      | `Task<IDomainResult>`           |
| `IDomainResult<T>`   | `Task<IDomainResult<T>>`        |
| `(T, IDomainResult)` | `Task<(T, IDomainResult)>`      |

### Examples:

```cs
// Successful result with no value
IDomainResult res = IDomainResult.Success();        // res.Status is 'Success'
// Successful result with an int
(value, state) = IDomainResult.Success(10);         // value = 10; state.Status is 'Success'
// The same but wrapped in a task
var res = IDomainResult.SuccessTask(10);            // res is Task<(int, IDomainResult)>
// Implicit convertion
IDomainResult<int> res = 10;                        // res.Value = 10; res.Status is 'Success'

// Error message
IDomainResult res = IDomainResult.Failed("Ahh!");   // res.Status is 'Failed' and res.Errors = new []{ "Ahh!" }
// Error when expected an int
(value, state) = IDomainResult.Failed<int>("Ahh!"); // value = 0, state.Status is 'Failed' and state.Errors = new []{ "Ahh!" }

// 'Not Found' acts like the errors
(value, state) = IDomainResult.NotFound<int>();     // value = 0, state.Status is 'NotFound'
Task<(int val, IDomainResult state)> res = IDomainResult.NotFoundTask<int>();  // value = 0, state.Status is 'NotFound'

// 'Unauthorized' response
(value, state) = IDomainResult.Unauthorized<int>(); // value = 0, state.Status is 'Unauthorized'
```

_Notes_:

- The `Task` suffix on the extension methods indicates that the returned type is wrapped in a `Task` (e.g. `SuccessTask()`, `FailedTask()`, `NotFoundTask()`, `UnauthorizedTask()`).
- The `Failed()` and `NotFound()` methods take as input parameters: `string`, `string[]`. `Failed()` can also take [ValidationResult](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationresult).

## 'DomainResult' package. Conversion to ActionResult

**Converts a `IDomainResult`-based object to various `ActionResult`-based types providing 20+ static extension methods.**

| Returned type                                  | Returned type wrapped in `Task`                      | Extension methods                                    |
| ---------------------------------------------- | ---------------------------------------------------- | ---------------------------------------------------- |
| `IActionResult`                                | `Task<IActionResult>`                                | `ToActionResult()`<br>`ToCustomActionResult()`       |
| `ActionResult<T>` | `Task<ActionResult<T>>` | `ToActionResultOfT()`<br>`ToCustomActionResultOfT()` |

<sub><sup>Note: `DomainResult` package has dependency on `Microsoft.AspNetCore.*` namespace and `DomainResult.Common` package.</sup></sub>

The mapping rules are built around `IDomainResult.Status`:

| `IDomainResult.Status` | Returned `ActionResult`-based type                                                                               |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `Success`              | If no value is returned then `204 NoContent`, otherwise - `200 OK`<br>Supports custom codes (e.g. `201 Created`) |
| `NotFound`             | HTTP code `404 NotFound` (default)                                                                               |
| `Failed`               | HTTP code `400` (default) or can be configured to `422` or any other code                                        |
| `Unauthorized`         | HTTP code `403 Forbidden` (default)                                                                              |

### Examples:

```cs
// Returns `IActionResult` with HTTP code `204 NoContent` on success
IDomainResult.ToActionResult();
// The same as above, but returns `Task<IActionResult>` with no need in 'await'
Task<IDomainResult>.ToActionResult();

// Returns `IActionResult` with HTTP code `200 Ok` along with the value
IDomainResult<T>.ToActionResult();
(T, IDomainResult).ToActionResult();
// As above, but returns `Task<IActionResult>` with no need in 'await'
Task<IDomainResult<T>>.ToActionResult();
Task<(T, IDomainResult)>.ToActionResult();

// Returns `ActionResult<T>` with HTTP code `200 Ok` along with the value
IDomainResult<T>.ToActionResultOfT();
(T, IDomainResult).ToActionResultOfT();
// As above, but returns `Task<ActionResult<T>>` with no need in 'await'
Task<IDomainResult<T>>.ToActionResultOfT();
Task<(T, IDomainResult)>.ToActionResultOfT();
```

## Custom Problem Details output

There is a way to tune the Problem Details output case-by-case.

### Custom ActionResult response for 2xx HTTP codes

When returning a standard `200` or `204` HTTP code is not enough, there are extension methods to knock yourself out - `ToCustomActionResult()` and `ToCustomActionResultOfT`.

Example of returning [201 Created](https://httpstatuses.com/201) along with a location header field pointing to the created resource (as per [RFC7231](https://tools.ietf.org/html/rfc7231#section-7.2)):

```cs
[HttpPost]
[ProducesResponseType(StatusCodes.Status201Created)]
public ActionResult<int> CreateItem(CreateItemDto dto)
{
  // Service method for creating an item and returning its ID.
  // Can return any of the IDomainResult types (e.g. (int, IDomainResult, IDomainResult<int>, Task<...>, etc).
  var result = _service.CreateItem(dto);
  // Custom conversion of the successful response only. For others, it returns standard 4xx HTTP codes
  return result.ToCustomActionResultOfT(
            // On success returns '201 Created' with a link to '/{id}' route in HTTP headers
            val => CreatedAtAction(nameof(GetById), new { id = val }, val)
        );
}

// Returns an entity by ID
[HttpGet("{id}")]
public IActionResult GetById([FromRoute] int id)
{
	...
}
```

It works with any of extensions in `Microsoft.AspNetCore.Mvc.ControllerBase`. Here are some:

- [AcceptedAtAction](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.acceptedataction) and [AcceptedAtRoute](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.acceptedatroute) for HTTP code [202 Accepted](https://httpstatuses.com/202);
- [File](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.File) or [PhysicalFile](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.PhysicalFile) for returning `200 OK` with the specified `Content-Type`, and the specified file name;
- [Redirect](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.Redirect), [RedirectToRoute](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.RedirectToRoute), [RedirectToAction](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.RedirectToAction) for returning [302 Found](https://httpstatuses.com/302) with various details.

### Custom error handling

The default HTTP codes for statuses `Failed`, `NotFound` and `Unauthorized` are defined in public static properties of `ActionResultConventions` with default values:

```cs
// The HTTP code to return for client request error
int ErrorHttpCode { get; set; }                 = 400;
// The 'title' property of the returned JSON on HTTP code 400
string ErrorProblemDetailsTitle { get; set; }   = "Bad Request";

// The HTTP code to return when a record not found
int NotFoundHttpCode { get; set; }              = 404;
// The 'title' property of the returned JSON on HTTP code 404
string NotFoundProblemDetailsTitle { get; set; }= "Not Found";

// The HTTP code to return when access to a record is forbidden
int UnauthorizedHttpCode { get; set; }          = 403;
// The 'title' property of the returned JSON on HTTP code 403
string UnauthorizedProblemDetailsTitle { get; set; }= "Unauthorized access";
```

Feel free to change them (hmm... remember they're static, with all the pros and cons). The reasons you may want it:

- Localisation of the titles
- Favour [422](https://httpstatuses.com/422) HTTP code in stead of [400](https://httpstatuses.com/400) (see opinions [here](https://stackoverflow.com/a/52098667/968003) and [here](https://stackoverflow.com/a/20215807/968003)).

The extension methods also support custom response in case of the `IDomainResult.Status` being `Failed`, `NotFound` or `Unauthorized`:

```cs
[HttpGet("[action]")]
[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
public Task<ActionResult<int>> GetFailedWithCustomStatusAndMessage()
{
  var res = _service.GetFailedWithNoMessage();
  return res.ToActionResultOfT(
            (problemDetails, state) =>
            {
              if (state.Errors?.Any() == true)
                return;
              problemDetails.Status = 422;      // Replace the default 400 code
              problemDetails.Title = "D'oh!";   // Replace the default 'Bad Request' title
              problemDetails.Detail = "I wish devs put more efforts into it...";   // Custom message
            });
}
```

## Alternative solutions

The problem solved here is not unique, so how does _DomainResult_ stand out?

### Why not FluentResults?

[FluentResults](https://github.com/altmann/FluentResults) is a great tool for indicating success or failure in the returned object. But there are different objectives:

- _FluentResults_ provides a generalised container for returning results and potential errors;
- _DomainResult_ is focused on a more specialised case when the Domain Logic is consumed by Web API.

Hence, _DomainResult_ provides out-of-the-box:

- Specialised extension methods (like `IDomainResult.NotFound()` that in _FluentResult_ would be indistinctive from other errors)
- Supports various ways of conversions to `ActionResult` (returning _Problem Details_ in case of error), functionality that is not available in _FluentResults_ and quite weak in the other NuGets extending _FluentResults_.

### Why not Hellang's ProblemDetails?

[Hellang.Middleware.ProblemDetails](https://github.com/khellang/Middleware) is another good one, where you can map exceptions to problem details.

In this case, the difference is ideological - "_throwing exception_" vs "_returning a faulty status_" for the sad path of execution in the business logic.

Main distinctive features of _DomainResult_ are

- Allows simpler nested calls of the domain logic (no exceptions handlers when severity of their "sad" path is not exception-worthy).
- Provides a predefined set of responses for main execution paths ("_bad request_", "_not found_", etc.). Works out-of-the-box.
- Has an option to tune each output independently.
