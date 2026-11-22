using System.ComponentModel.DataAnnotations;

using DomainResults.Common;
using DomainResults.Common.Exceptions;

using Xunit;

namespace DomainResults.Tests.Common;

public class DomainResultValidationErrorsTests
{
	[Fact]
	public void Failed_ValidationResults_GroupsMessagesByMember()
	{
		// GIVEN validation results: two for one member, one for two members, and one without a member
		var validationResults = new[]
			{
				new ValidationResult("1", ["a"]),
				new ValidationResult("2", ["a", "b"]),
				new ValidationResult("3")
			};

		// WHEN a failed result is created from them
		var domainResult = DomainResult.Failed(validationResults);

		// THEN the messages are grouped by member, and the message without a member has an empty key
		Assert.Equal(DomainOperationStatus.Failed, domainResult.Status);
		Assert.Equal(new Dictionary<string, IReadOnlyCollection<string>>
			{
				["a"] = ["1", "2"],
				["b"] = ["2"],
				[""] = ["3"]
			}, domainResult.ValidationErrors);
		// and the errors keep the member names in brackets
		Assert.Equal(["1 (a)", "2 (a, b)", "3"], domainResult.Errors);
	}

	[Theory]
	[MemberData(nameof(ShapeCases))]
	public void Failed_ValidationResultsInEachShape_HasValidationErrors(Func<IEnumerable<ValidationResult>, IDomainResultBase> failedFunc)
	{
		// GIVEN a validation result of a member
		var validationResults = new[] { new ValidationResult("1", ["a"]) };

		// WHEN a failed result of the shape is created from it
		var domainResult = failedFunc(validationResults);

		// THEN the result has the validation errors
		Assert.Equal(DomainOperationStatus.Failed, domainResult.Status);
		Assert.Equal(new Dictionary<string, IReadOnlyCollection<string>> { ["a"] = ["1"] }, domainResult.ValidationErrors);
	}
	public static TheoryData<Func<IEnumerable<ValidationResult>, IDomainResultBase>> ShapeCases => new()
		{
			v => IDomainResult.Failed(v),
			v => IDomainResult<int>.Failed(v),
			v => IDomainResult.Failed<int>(v).Item2
		};

	[Fact]
	public void Failed_Messages_HasNoValidationErrors()
	{
		// GIVEN a failed result with messages only
		var domainResult = DomainResult.Failed("1");

		// WHEN the validation errors are read
		var validationErrors = domainResult.ValidationErrors;

		// THEN they are empty
		Assert.Empty(validationErrors);
	}

	[Fact]
	public void Deconstruct_ValidationErrors_KeepsValidationErrors()
	{
		// GIVEN a failed result with a value type and validation errors
		IDomainResult<int> domainResult = DomainResult.Failed<int>([new ValidationResult("1", ["a"])]);

		// WHEN it is deconstructed
		var (_, details) = domainResult;

		// THEN the details keep the validation errors
		Assert.Equal(domainResult.ValidationErrors, details.ValidationErrors);
	}

	[Fact]
	public void To_ValidationErrors_KeepsValidationErrors()
	{
		// GIVEN a failed result with a value type and validation errors
		var domainResult = DomainResult.Failed<int>([new ValidationResult("1", ["a"])]);

		// WHEN it is converted to another value type
		var converted = domainResult.To<int, long>();

		// THEN the converted result keeps the validation errors
		Assert.Equal(domainResult.ValidationErrors, converted.ValidationErrors);
	}

	[Fact]
	public void ThrowIfNoSuccess_ValidationErrors_ExceptionKeepsValidationErrors()
	{
		// GIVEN a failed result with validation errors
		var domainResult = DomainResult.Failed([new ValidationResult("1", ["a"])]);

		// WHEN the check throws
		var exception = Assert.Throws<DomainResultException>(() => domainResult.ThrowIfNoSuccess());

		// THEN the result in the exception keeps the validation errors
		Assert.Equal(domainResult.ValidationErrors, exception.DomainResult.ValidationErrors);
	}
}
