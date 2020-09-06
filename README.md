# DomainResult
**NuGet for decoupling domain operation results from ActionResult-based types of ASP.NET Web API**

![CI](https://github.com/AKlaus/DomainResult/workflows/CI/badge.svg)
[![Test Coverage](https://coveralls.io/repos/github/AKlaus/DomainResult/badge.svg?branch=master)](https://coveralls.io/github/AKlaus/DomainResult?branch=master)
[![DomainResult NuGet version](https://img.shields.io/nuget/v/DomainResult.svg?style=flat&label=nuget%3A%20DomainResult)](https://www.nuget.org/packages/DomainResult)
[![DomainResult.Common NuGet version](https://img.shields.io/nuget/v/DomainResult.Common.svg?style=flat&label=nuget%3A%20DomainResult.Common)](https://www.nuget.org/packages/DomainResult.Common)
<br/>

Two tiny NuGet packages addressing challenges in the [ASP.NET Web API](https://dotnet.microsoft.com/apps/aspnet/apis) realm posed by separation of the _Domain Layer_ (aka _Business Layer_) from the _Application Layer_:
- eliminating a dependency on _Microsoft.AspNetCore.Mvc_ (and [IActionResult](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.iactionresult) in particular) in the _Domain Layer_ (usually a separate project);
- mapping a variety of responses from the _Domain Layer_ to appropriate [IActionResult](https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types)-related type.

## Basic use-case

For a _Domain Layer_ method like this:

```csharp
public async Task<(InvoiceResponseDto, IDomainResult)> GetInvoice(int invoiceId)
{
    if (invoiceId < 0)
        // Returns a validation error
        return IDomainResult.Error<InvoiceResponseDto>("Try harder");

    var invoice = await DataContext.Invoices.FindAsync(invoiceId);
    
    if (invoice == null)
        // Returns a Not Found response
        return IDomainResult.NotFound<InvoiceResponseDto>();

    // Returns the invoice
    IDomainResult.Success(invoice);
}
```

or if you're against [ValueTuple](https://docs.microsoft.com/en-us/dotnet/api/system.valuetuple), then a more traditional method signature:

```csharp
public async Task<IDomainResult<InvoiceResponseDto>> GetInvoice(int invoiceId)
{
    if (invoiceId < 0)
        // Returns a validation error
        return DomainResult.Error<InvoiceResponseDto>("Try harder");
    ...
}
```

The _Web API_ controller method would look like:

```csharp
public Task<IActionResult> GetInvoice()
{
    _service.GetInvoice().ToActionResult();
}
```
or for ASP.NET Core 2.1+ we can leverage [ActionResult&lt;T&gt;](https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types#actionresultt-type)

```csharp
public Task<ActionResult<InvoiceResponseDto>> GetInvoice()
{
    _service.GetInvoice().ToActionResultOfT();
}
```

The above returns:
- HTTP code `200 OK` along with an instance of `InvoiceResponseDto` on successful executions.
- HTTP code `400 Bad Request` or `422 Unprocessable Entity` (configurable) with a message "_Try harder_" when the invoice ID < 1.
- HTTP code `404 Not Found` for incorrect invoice IDs.

All non-2xx codes are wrapped in [ProblemDetails](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) as per [RFC 7807](https://tools.ietf.org/html/rfc7807).

## Quick start

- Install [DomainResult](https://www.nuget.org/packages/DomainResult) NuGet package to the _Web API_ project.
- Install [DomainResult.Common](https://www.nuget.org/packages/DomainResult.Common) NuGet package to the _Domain Layer_ (aka _Business Layer_) projects. If the _Domain Layer_ is inside the _Web API_ project, then skip this step.
- Follow the documentation below, `samples` in the repo and common sense.

The library targets `.NET Standard 2.0` ([supported .NET implementations](https://dotnet.microsoft.com/platform/dotnet-standard#versions)) and `.NET Core 3.0`.

## 'DomainResult.Common' package. How it works?

**Wraps response of the domain operation to handle the happy path (returning of the result) and various sad paths (errors like '_not found_').**

This is a tiny package with no dependency on `Microsoft.AspNetCore.*` namespace.

It's built around `IDomainResult` interface that has 3 properties:
```csharp
// Collection of error messages if any
IReadOnlyCollection<string> Errors { get; }

// Flag, whether the current status is successful or not
bool IsSuccess { get; }

// Current status of the domain operation: Success, Error, NotFound
DomainOperationStatus Status { get; }
```

And `IDomainResult<T>` interface that also adds
```csharp
// Value returned by the domain operation
T Value { get; }
```

It has more than **50 static extension methods** to return a successful or unsuccessful result from the domain method with one of the following types:

| Returned type        | Returned type wrapped in `Task` |
| -------------------- | ------------------------------- |
| `IDomainResult`      | `Task<IDomainResult>`           |
| `IDomainResult<T>`   | `Task<IDomainResult<T>>`        |
| `(T, IDomainResult)` | `Task<(T, IDomainResult)>`      |

### Examples:
| Extension method                                                                              | Details on the returned object                                                                                                             |
| --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `IDomainResult.Success()`<sup>[1](#myfootnote1)</sup>                                         | `IDomainResult`, where `Status` = `DomainOperationStatus.Success`                                                                          |
| `IDomainResult.Error("Ahh!")`<sup>[1](#myfootnote1)</sup>                                     | `IDomainResult`, where `Status` = `DomainOperationStatus.Error`<br>and `Errors` = `new []{ "Ahh!" }`                                       |
| `IDomainResult.ErrorTask("Ahh!")`<sup>[1](#myfootnote1)</sup><sup>, [2](#myfootnote2)</sup>   | `Task<IDomainResult>`, where `Status` = `DomainOperationStatus.Error`<br>and `Errors` = `new []{ "Ahh!" }`                                 |
| `IDomainResult.Success(10)`<sup>[1](#myfootnote1)</sup>                                       | `(int, IDomainResult)`, where `Status` = `DomainOperationStatus.Success`<br>and the `int` type value = `10`                                |
| `IDomainResult.NotFoundTask<int>()`<sup>[1](#myfootnote1)</sup><sup>, [2](#myfootnote2)</sup> | `Task<(int, IDomainResult)>`, where `Status` = `DomainOperationStatus.NotFound`<br>and the `int` type value = `default` (`0` in this case) |

<sup><a name="myfootnote1">1</a></sup> <small>Note that extension methods on interface are supported starting from `.NET Standard 2.1`. For older version use static extensions on `DomainResult` class.</small><br>
<sup><a name="myfootnote2">2</a></sup> <small>The `Task` suffix on the extension methods indicates that the returned type is wrapped in a `Task` (e.g. `SuccessTask()`, `ErrorTask()`, `NotFoundTask()`).</small>

## 'DomainResult' package. How it works?

**Converts a `IDomainResult`-based object to various `ActionResult`-based types providing more than `20` static extension methods.**

| Returned type                                 | Returned type wrapped in `Task`                     |
| --------------------------------------------- | --------------------------------------------------- |
| `IActionResult`                               | `Task<IActionResult>`                               |
| `ActionResult<T>`<sup>[3](#myfootnote3)</sup> | `Task<ActionResult<T>>`<sup>[3](#myfootnote3)</sup> |

<sup><a name="myfootnote3">3</a></sup> <small>[ActionResult&lt;T&gt;](https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types#actionresultt-type) type was introduced in ASP.NET Core 2.1.</small>

### Examples:
| Extension method                    | Details on the returned object                                                                                                                                                                                     |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `IDomainResult.ToActionResult()`    | `IActionResult` with HTTP code `204 NoContent`,<br>or `404 NotFound` if `IDomainResult.Status==DomainOperationStatus.NotFound`,<br>or `400`/`422` HTTP code if `IDomainResult.Status==DomainOperationStatus.Error` |
| `IDomainResult<T>.ToActionResult()` | `IActionResult` with HTTP code `204 NoContent`                                                                                                                                                                     |


'DomainResult' package has dependency on `Microsoft.AspNetCore.*` namespace and `DomainResult.Common` package.
