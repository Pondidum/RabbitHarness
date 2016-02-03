using System;
using System.Threading;
using Shouldly;

namespace RabbitHarness.Tests
{
	public class QueryTests : TestBase
	{

		[RequiresRabbitFact(Host)]
		public void When_a_reply_is_sent()
		{
			CreateResponder();

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };


			Factory.Query<int>(Context, message, response =>
			{
				response.Content.ShouldBe(21);
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(10));
		}

		[RequiresRabbitFact(Host)]
		public void When_nothing_is_listening()
		{
			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			Factory.Query<int>(Context, message, response =>
			{
				received = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

		[RequiresRabbitFact(Host)]
		public void When_something_else_responds()
		{
			CreateResponder(p => p.CorrelationId = Guid.NewGuid().ToString());

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			Factory.Query<int>(Context, message, response =>
			{
				received = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

	}
}