# DomainResult
![CI](https://github.com/AKlaus/DomainResult/workflows/CI/badge.svg)
<br/>

A tiny NuGet package for the [ASP.NET Web API](https://dotnet.microsoft.com/apps/aspnet/apis) realm addressing two challenge posed by separation of the _Domain Layer_ (aka _Business Layer_) from the _Application Layer_:
- eliminating a dependency on _Microsoft.AspNetCore.Mvc_ (and IActionResult in particular) in the _Domain Layer_ (usually a separate project);
- mapping a variety of responses to from the _Domain Layer_ to appropriate [IActionResult](https://docs.microsoft.com/en-us/aspnet/core/web-api/action-return-types)-related type.

## Simple example

The _Domain Layer_ project has a method:

```csharp
public async Task<DomainResult<InvoiceResponseDto>> GetInvoice(int invoiceId)
{
    if (invoiceId < 0)
        // Return a validation error
        return DomainResult.Error<InvoiceResponseDto>("Try harder");

    var invoice = await DataContext.Invoices.FindAsync(invoiceId);
    
    if (invoice == null)
        // Not found response
        return DomainResult.NotFound<InvoiceResponseDto>();

    // Returning the invoice
    DomainResult.Success(invoice);
}
```

or if you prefer a more expressive way:

```csharp
public async Task<(InvoiceResponseDto, IDomainResult)> GetInvoice(int invoiceId)
{
    ...
}
```

The _Application Layer_ project would have the following Web API controller method:

```csharp
public Task<IActionResult> GetInvoice()
{
    _service.GetInvoice().ToActionResult();
}
```