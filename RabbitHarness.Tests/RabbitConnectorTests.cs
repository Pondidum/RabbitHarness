using System;
using System.Threading;
using RabbitMQ.Client;
using Shouldly;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorTests
	{
		[RequiresRabbitFact(TestBase.Host)]
		public void When_listening()
		{
			var factory = new ConnectionFactory { HostName = TestBase.Host };
			var connector = new RabbitConnector(factory);

			var reset = new AutoResetEvent(true);
			var unsubscribe = connector.ListenTo("someQueue", (props, json) =>
			{
				reset.Set();
				return true;
			});

			reset.WaitOne(TimeSpan.FromSeconds(10));

			unsubscribe();
		}

		[RequiresRabbitFact(TestBase.Host)]
		public void When_creating_a_queue_and_listening()
		{
			var factory = new ConnectionFactory { HostName = TestBase.Host };
			var connector = new RabbitConnector(factory);

			var reset = new AutoResetEvent(true);
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
	}
}
