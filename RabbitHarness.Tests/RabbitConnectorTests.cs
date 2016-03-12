using System;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit.Abstractions;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorTests
	{
		private ITestOutputHelper _output;

		public RabbitConnectorTests(ITestOutputHelper output)
		{
			_output = output;
		}

		[RequiresRabbitFact(TestBase.Host)]
		public void When_creating_a_queue_and_listening()
		{
			var factory = new ConnectionFactory { HostName = TestBase.Host };
			var connector = new RabbitConnector(factory);

			var reset = new AutoResetEvent(false);
			var received = "";

			var unsubscribe = connector.ListenTo(
				"SomeQueue",
				queue =>
				{
					queue.AutoDelete();
					queue.DeclareQueue();
				},
				(props, json) =>
				{
					received = json;
					reset.Set();
					return true;
				});

			connector.Send("SomeQueue", props => { }, new { Name = "Test" });

			reset.WaitOne(TimeSpan.FromSeconds(10));

			received.ShouldBe("{\"Name\":\"Test\"}");

			unsubscribe();
		}

		[RequiresRabbitFact(TestBase.Host)]
		public void When_a_reply_is_sent()
		{

			Action<QueueDeclaration> declare = queue =>
			{
				queue.AutoDelete();
				queue.DeclareQueue();
			};

			var factory = new ConnectionFactory { HostName = TestBase.Host };
			var connector = new RabbitConnector(factory);

			var reset = new AutoResetEvent(false);
			var received = "";

			connector.ListenTo("SomeQueue", declare, (props, json) =>
			{
				connector.Send(
					props.ReplyTo,
					rp => { rp.CorrelationId = props.CorrelationId; },
					new { Name = "Reply" });

				return true;
			});

			connector.Query(
				"SomeQueue",
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
	}
}
