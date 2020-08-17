using System;
using System.Collections.Generic;

using AK.DomainResults.Domain;
using AK.DomainResults.Mvc;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace DomainResults.Mvc.Tests
{
	public class ValueResultToActionTests
	{
		#region Test of '(TValue, IDomainResult)' conversion -------------------

		[Theory]
		[MemberData(nameof(TestCases))]
		public void SuccessfulNumericValueResult<TValue,TAction>((TValue, IDomainResult) tupleValue, Func<TAction, TValue> getValueFunc) where TAction : class
		{
			// WHEN convert a value to ActionResult
			var actionRes = tupleValue.ToActionResult();

			// THEN the type of the created 'ActionResult' is correct
			Assert.True(actionRes.GetType().IsAssignableFrom(typeof(TAction)));

			if (getValueFunc != null)
				// and value remains there
				Assert.Equal(tupleValue.Item1, getValueFunc(actionRes as TAction));
		}

		public static readonly IEnumerable<object[]> TestCases = new List<object[]>
		{
			new object[] {	(10,  ErrorDetails.Success()), 
							(Func<OkObjectResult, int>)(res => (int)res.Value) 
						 },
			new object[] {	("1", ErrorDetails.Success()), 
							(Func<OkObjectResult, string>)(res => (string)res.Value) 
						 },
			new object[] {	(new TestDto { Prop = "1" }, ErrorDetails.Success()), 
							(Func<OkObjectResult, TestDto>)(res => (TestDto)res.Value) 
						 },
		};
		#endregion // Test of '(TValue, IDomainResult)' conversion ------------

		class TestDto
		{
			public string Prop { get; set; }
		}
	}
}
