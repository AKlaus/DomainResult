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

## Common use-case

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
- HTTP code `200 OK` along with an instance of `InvoiceResponseDto` for successful execution.
- HTTP code `400 Bad Request` with a message "_Try harder_" for invoice ID < 1.
- HTTP code `404 Not Found` for incorrect invoice ID.

All non-2xx codes are wrapped in [ProblemDetails](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails) as per [RFC 7807](https://tools.ietf.org/html/rfc7807).

## Quick start

- Install [DomainResult](https://www.nuget.org/packages/DomainResult) NuGet package to the _Web API_ project.
- Install [DomainResult.Common](https://www.nuget.org/packages/DomainResult.Common) NuGet package to the _Domain Layer_ (aka _Business Layer_) projects. If the _Domain Layer_ is in the _Web API_ project, then skip this step.
- Follow the documentation below, `samples` in the repo or your heart.

The library targets `.NET Standard 2.0` and `.NET Core 3.0` (see [supported .NET implementations](https://dotnet.microsoft.com/platform/dotnet-standard#versions)).

## How it works

There are 2 packages: `DomainResult.Common` and `DomainResult` (that refers `DomainResult.Common` under the hood).

### DomainResult.Common package

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

It has more than `50` static extension methods to satisfy various ways of returning the result from the domain method.

### DomainResult package

**Convert a `IDomainResult`-based object to various `ActionResult`-based types providing more than `20` static extension methods.**

This package has dependency on `Microsoft.AspNetCore.*` namespace and `DomainResult.Common` package.
