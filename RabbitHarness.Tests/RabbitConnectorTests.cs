using System;
using System.Threading;
using Shouldly;
using Xunit.Abstractions;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorTests : TestBase
	{
		private ITestOutputHelper _output;

		public RabbitConnectorTests(ITestOutputHelper output)
		{
			_output = output;
		}

		[RequiresRabbitFact(Host)]
		public void When_creating_a_queue_and_listening()
		{
			var connector = new RabbitConnector(Factory);

			var reset = new AutoResetEvent(false);
			var received = "";

			var unsubscribe = connector.ListenTo(
				Route.Queue(QueueName),
				declare => declare.Queue().AutoDelete(),
				(props, json) =>
				{
					received = json;
					reset.Set();
					return true;
				});

			connector.Send(Route.Queue(QueueName), props => { }, new { Name = "Test" });

			reset.WaitOne(TimeSpan.FromSeconds(10));

			received.ShouldBe("{\"Name\":\"Test\"}");

			unsubscribe();
		}

		[RequiresRabbitFact(Host)]
		public void When_a_reply_is_sent()
		{

			Action<DeclarationExpression> declare = d => d.Queue().AutoDelete();

			var connector = new RabbitConnector(Factory);

			var reset = new AutoResetEvent(false);
			var received = "";

			connector.ListenTo(Route.Queue(QueueName), declare, (props, json) =>
			{
				connector.Send(
					Route.Queue(props.ReplyTo),
					rp => { rp.CorrelationId = props.CorrelationId; },
					new { Name = "Reply" });

				return true;
			});

			connector.Query(
				Route.Queue(QueueName),
				declare,
				props => { },
				new { Name = "QueryTest" },
				(props, json) =>
				{
					received = json;
					reset.Set();
					return true;
				});

			reset.WaitOne(TimeSpan.FromSeconds(10));
			received.ShouldBe("{\"Name\":\"Reply\"}");
		}

		[RequiresRabbitFact(Host)]
		public void When_nothing_is_listening()
		{
			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			var connector = new RabbitConnector(Factory);

			connector.Query(Route.Queue(QueueName), declare => declare.Queue().AutoDelete(), props => { }, message, (props, json) =>
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

			connector.Query(Route.Queue(QueueName), declare => declare.Queue().AutoDelete(), props => { }, message, (props, json) =>
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
