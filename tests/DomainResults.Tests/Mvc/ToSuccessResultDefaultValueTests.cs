using DomainResults.Common;
using DomainResults.Mvc;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Tests.Mvc;

/// <summary>
///		Conversion of a successful result with a default value or a null value, for both result shapes with a value
/// </summary>
public class ToSuccessResultDefaultValueTests
{
	private static readonly Uri Location = new("http://localhost/");

	#region ToActionResult -----------------------------------------------------

	[Theory]
	[MemberData(nameof(DefaultValueTestCases))]
	public void ToActionResult_DefaultValue_Returns200OkWithValue<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a default value of a value type

		// WHEN convert each result shape to IActionResult
		IActionResult[] results = [domainResult.ToActionResult(), tupleResult.ToActionResult()];

		// THEN each response is 200 OK with the value
		Assert.All(results, res =>
		{
			var okResult = Assert.IsType<OkObjectResult>(res);
			Assert.Equal(200, okResult.StatusCode);
			Assert.Equal(domainResult.Value, okResult.Value);
		});
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void ToActionResult_NullValue_Returns204NoContent<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a null value

		// WHEN convert each result shape to IActionResult
		IActionResult[] results = [domainResult.ToActionResult(), tupleResult.ToActionResult()];

		// THEN each response is 204 NoContent
		Assert.All(results, res => Assert.IsType<NoContentResult>(res));
	}

	#endregion // ToActionResult -----------------------------------------------

	#region ToActionResultOfT --------------------------------------------------

	[Theory]
	[MemberData(nameof(DefaultValueTestCases))]
	public void ToActionResultOfT_DefaultValue_ReturnsValue<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a default value of a value type

		// WHEN convert each result shape to ActionResult<T>
		ActionResult<TValue>[] results = [domainResult.ToActionResultOfT(), tupleResult.ToActionResultOfT()];

		// THEN each response holds the value and no other action result, so ASP.NET Core returns 200 OK
		Assert.All(results, res =>
		{
			Assert.Null(res.Result);
			Assert.Equal(domainResult.Value, res.Value);
		});
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void ToActionResultOfT_NullValue_Returns204NoContent<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a null value

		// WHEN convert each result shape to ActionResult<T>
		ActionResult<TValue>[] results = [domainResult.ToActionResultOfT(), tupleResult.ToActionResultOfT()];

		// THEN each response is 204 NoContent
		Assert.All(results, res => Assert.IsType<NoContentResult>(res.Result));
	}

	#endregion // ToActionResultOfT --------------------------------------------

	#region ToResult -----------------------------------------------------------

	[Theory]
	[MemberData(nameof(DefaultValueTestCases))]
	public void ToResult_DefaultValue_Returns200OkWithValue<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a default value of a value type

		// WHEN convert each result shape to IResult
		IResult[] results = [domainResult.ToResult(), tupleResult.ToResult()];

		// THEN each response is 200 OK with the value
		Assert.All(results, res => res.AssertOkObjectResultTypeAndValue(domainResult.Value));
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void ToResult_NullValue_Returns204NoContent<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a null value

		// WHEN convert each result shape to IResult
		IResult[] results = [domainResult.ToResult(), tupleResult.ToResult()];

		// THEN each response is 204 NoContent
		Assert.All(results, res => res.AssertNoContentResultType());
	}

	#endregion // ToResult -----------------------------------------------------

	#region ToCustomActionResult -----------------------------------------------

	[Theory]
	[MemberData(nameof(DefaultValueTestCases))]
	public void ToCustomActionResult_DefaultValue_ReturnsCustomResult<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a default value of a value type

		// WHEN convert each result shape with a custom conversion to 201 Created
		IActionResult[] results =
		[
			domainResult.ToCustomActionResult(value => new CreatedResult(Location, value)),
			tupleResult.ToCustomActionResult(value => new CreatedResult(Location, value))
		];

		// THEN each response is 201 Created with the value
		Assert.All(results, res => AssertCreatedResult(res, domainResult.Value));
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void ToCustomActionResult_NullValue_Returns204NoContent<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a null value

		// WHEN convert each result shape with a custom conversion to 201 Created
		IActionResult[] results =
		[
			domainResult.ToCustomActionResult(value => new CreatedResult(Location, value)),
			tupleResult.ToCustomActionResult(value => new CreatedResult(Location, value))
		];

		// THEN each response is 204 NoContent
		Assert.All(results, res => Assert.IsType<NoContentResult>(res));
	}

	#endregion // ToCustomActionResult -----------------------------------------

	#region ToCustomActionResultOfT --------------------------------------------

	[Theory]
	[MemberData(nameof(DefaultValueTestCases))]
	public void ToCustomActionResultOfT_DefaultValue_ReturnsCustomResult<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a default value of a value type

		// WHEN convert each result shape with a custom conversion to 201 Created
		ActionResult<TValue>[] results =
		[
			domainResult.ToCustomActionResultOfT(value => new CreatedResult(Location, value)),
			tupleResult.ToCustomActionResultOfT(value => new CreatedResult(Location, value))
		];

		// THEN each response is 201 Created with the value
		Assert.All(results, res => AssertCreatedResult(res.Result, domainResult.Value));
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void ToCustomActionResultOfT_NullValue_Returns204NoContent<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a null value

		// WHEN convert each result shape with a custom conversion to 201 Created
		ActionResult<TValue>[] results =
		[
			domainResult.ToCustomActionResultOfT(value => new CreatedResult(Location, value)),
			tupleResult.ToCustomActionResultOfT(value => new CreatedResult(Location, value))
		];

		// THEN each response is 204 NoContent
		Assert.All(results, res => Assert.IsType<NoContentResult>(res.Result));
	}

	#endregion // ToCustomActionResultOfT --------------------------------------

	#region ToCustomResult -----------------------------------------------------

	[Theory]
	[MemberData(nameof(DefaultValueTestCases))]
	public void ToCustomResult_DefaultValue_ReturnsCustomResult<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a default value of a value type

		// WHEN convert each result shape with a custom conversion to 201 Created
		IResult[] results =
		[
			domainResult.ToCustomResult(value => TypedResults.Created(Location, value)),
			tupleResult.ToCustomResult(value => TypedResults.Created(Location, value))
		];

		// THEN each response is 201 Created with the value
		Assert.All(results, res => res.AssertCreatedResultTypeAndValueAndLocation(domainResult.Value, Location.ToString()));
	}

	[Theory]
	[MemberData(nameof(NullValueTestCases))]
	public void ToCustomResult_NullValue_Returns204NoContent<TValue>(IDomainResult<TValue> domainResult, (TValue, IDomainResult) tupleResult)
	{
		// GIVEN a successful result with a null value

		// WHEN convert each result shape with a custom conversion to 201 Created
		IResult[] results =
		[
			domainResult.ToCustomResult(value => TypedResults.Created(Location, value)),
			tupleResult.ToCustomResult(value => TypedResults.Created(Location, value))
		];

		// THEN each response is 204 NoContent
		Assert.All(results, res => res.AssertNoContentResultType());
	}

	#endregion // ToCustomResult -----------------------------------------------

	/// <summary>
	///		A default value of a value type
	/// </summary>
	public static readonly IEnumerable<object[]> DefaultValueTestCases =
	[
		GetTestCase(0)
	];

	/// <summary>
	///		Null values of a reference type and a nullable value type
	/// </summary>
	public static readonly IEnumerable<object[]> NullValueTestCases =
	[
		GetTestCase<TestDto?>(null),
		GetTestCase<int?>(null)
	];

	private static object[] GetTestCase<T>(T value)
		=> [DomainResult.Success(value), IDomainResult.Success(value)];

	private static void AssertCreatedResult<TValue>(IActionResult? res, TValue expectedValue)
	{
		var createdResult = Assert.IsType<CreatedResult>(res);
		Assert.Equal(201, createdResult.StatusCode);
		Assert.Equal(expectedValue, createdResult.Value);
		Assert.Equal(Location.ToString(), createdResult.Location);
	}
}
