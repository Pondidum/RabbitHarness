using System;
using System.Threading;
using Newtonsoft.Json;
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

			var connector = new RabbitConnector(Factory);

			connector.Query(QueueName, declare => { declare.AutoDelete(); declare.DeclareQueue(); }, props => { }, message, (props, json) =>
			{
				var result = JsonConvert.DeserializeObject<int>(json);
				result.ShouldBe(21);
				reset.Set();
				return true;
			});

			reset.WaitOne(TimeSpan.FromSeconds(10));
		}

		[RequiresRabbitFact(Host)]
		public void When_nothing_is_listening()
		{
			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			var connector = new RabbitConnector(Factory);

			connector.Query(QueueName, declare => { declare.AutoDelete(); declare.DeclareQueue(); }, props => { }, message, (props, json) =>
			{
				received = true;
				reset.Set();
				return true;
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

			var connector = new RabbitConnector(Factory);

			connector.Query(QueueName, declare => { declare.AutoDelete(); declare.DeclareQueue(); }, props => { }, message, (props, json) =>
			{
				received = true;
				reset.Set();
				return true;
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

	}
}