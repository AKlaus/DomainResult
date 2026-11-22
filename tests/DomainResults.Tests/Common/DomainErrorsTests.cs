using System.ComponentModel.DataAnnotations;

using DomainResults.Common;
using DomainResults.Common.Exceptions;

using Xunit;

namespace DomainResults.Tests.Common;

public class DomainErrorsTests
{
	[Fact]
	public void Failed_ValidationResults_KeepsMessagesAndMemberNames()
	{
		// GIVEN validation results with one member, two members, and no member
		var validationResults = new[]
			{
				new ValidationResult("1", ["a"]),
				new ValidationResult("2", ["a", "b"]),
				new ValidationResult("3")
			};

		// WHEN a failed result is created from them
		var domainResult = DomainResult.Failed(validationResults);

		// THEN each domain error keeps its message and member names, in order
		Assert.Equal(DomainOperationStatus.Failed, domainResult.Status);
		Assert.Equal(["1: a", "2: a, b", "3: "], Describe(domainResult.DomainErrors));
		// and the errors are their text, with the member names in brackets
		Assert.Equal(["1 (a)", "2 (a, b)", "3"], domainResult.Errors);
	}

	[Fact]
	public void Failed_NullMessageAndNullMemberName_KeepsThemAsEmptyText()
	{
		// GIVEN validation results with a null message, and with a null member name
		var domainResult = DomainResult.Failed([new ValidationResult(null), new ValidationResult("2", [null!])]);

		// WHEN the domain errors are read
		var domainErrors = domainResult.DomainErrors;

		// THEN the null message and the null member name become empty text
		Assert.Equal([": ", "2: "], Describe(domainErrors));
		Assert.Equal(["", "2 ()"], domainResult.Errors);
	}

	[Theory]
	[MemberData(nameof(ShapeCases))]
	public void Failed_ValidationResultsInEachShape_HasDomainErrors(Func<IEnumerable<ValidationResult>, IDomainResultBase> failedFunc)
	{
		// GIVEN a validation result of a member
		var validationResults = new[] { new ValidationResult("1", ["a"]) };

		// WHEN a failed result of the shape is created from it
		var domainResult = failedFunc(validationResults);

		// THEN the result has the domain error with the member name
		Assert.Equal(DomainOperationStatus.Failed, domainResult.Status);
		Assert.Equal(["1: a"], Describe(domainResult.DomainErrors));
	}
	public static TheoryData<Func<IEnumerable<ValidationResult>, IDomainResultBase>> ShapeCases => new()
		{
			v => IDomainResult.Failed(v),
			v => IDomainResult<int>.Failed(v),
			v => IDomainResult.Failed<int>(v).Item2
		};

	[Fact]
	public void Failed_Messages_HasDomainErrorsWithoutMemberNames()
	{
		// GIVEN a failed result with messages only
		var domainResult = DomainResult.Failed(["1", "2"]);

		// WHEN the domain errors are read
		var domainErrors = domainResult.DomainErrors;

		// THEN each message is a domain error without member names
		Assert.Equal(["1: ", "2: "], Describe(domainErrors));
	}

	[Fact]
	public void DomainErrors_ResultWithoutOwnImplementation_MirrorsErrors()
	{
		// GIVEN a consumer's result type that does not implement 'DomainErrors'
		IDomainResultBase domainResult = new ConsumerResult();

		// WHEN the domain errors are read
		var domainErrors = domainResult.DomainErrors;

		// THEN each error is a domain error without member names
		Assert.Equal(["1: "], Describe(domainErrors));
	}

	[Fact]
	public void Deconstruct_DomainErrors_KeepsDomainErrors()
	{
		// GIVEN a failed result with a value type and validation results
		IDomainResult<int> domainResult = DomainResult.Failed<int>([new ValidationResult("1", ["a"])]);

		// WHEN it is deconstructed
		var (_, details) = domainResult;

		// THEN the details keep the domain errors
		Assert.Equal(domainResult.DomainErrors, details.DomainErrors);
	}

	[Fact]
	public void To_DomainErrors_KeepsDomainErrors()
	{
		// GIVEN a failed result with a value type and validation results
		var domainResult = DomainResult.Failed<int>([new ValidationResult("1", ["a"])]);

		// WHEN it is converted to another value type
		var converted = domainResult.To<int, long>();

		// THEN the converted result keeps the domain errors
		Assert.Equal(domainResult.DomainErrors, converted.DomainErrors);
	}

	[Fact]
	public void ThrowIfNoSuccess_DomainErrors_ExceptionKeepsDomainErrors()
	{
		// GIVEN a failed result with validation results
		var domainResult = DomainResult.Failed([new ValidationResult("1", ["a"])]);

		// WHEN the check throws
		var exception = Assert.Throws<DomainResultException>(() => domainResult.ThrowIfNoSuccess());

		// THEN the result in the exception keeps the domain errors
		Assert.Equal(domainResult.DomainErrors, exception.DomainResult.DomainErrors);
	}

	private static IEnumerable<string> Describe(IEnumerable<DomainError> domainErrors)
		=> domainErrors.Select(error => $"{error.Message}: {string.Join(", ", error.MemberNames)}");

	private sealed class ConsumerResult : IDomainResult
	{
		public IReadOnlyCollection<string> Errors => ["1"];
		public bool IsSuccess => false;
		public DomainOperationStatus Status => DomainOperationStatus.Failed;
	}
}
