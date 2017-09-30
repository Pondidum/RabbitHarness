using System;
using System.Threading;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace RabbitHarness.Tests.Extensions
{
	public class ToTimestampTests
	{
		private readonly ITestOutputHelper _output;

		public ToTimestampTests(ITestOutputHelper output)
		{
			_output = output;
		}

		[Theory]
		[InlineData("2016/03/11 20:07:39")]
		[InlineData("1970/01/01 00:00:00")]
		public void When_converting(string datetime)
		{
			var dt = DateTime.Parse(datetime);

			dt.ToTimestamp().ToDateTime().ShouldBe(dt);
		}
	}
}
