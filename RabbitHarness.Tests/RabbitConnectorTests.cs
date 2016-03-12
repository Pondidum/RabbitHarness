using System;
using System.Threading;
using RabbitMQ.Client;

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
			connector.ListenTo("someQueue", (props, json) =>
			{
				reset.Set();
				return true;
			});

			reset.WaitOne(TimeSpan.FromSeconds(10));
		}

		[RequiresRabbitFact(TestBase.Host)]
		public void When_creating_a_queue_and_listening()
		{
			var factory = new ConnectionFactory { HostName = TestBase.Host };
			var connector = new RabbitConnector(factory);

			var reset = new AutoResetEvent(true);
			connector.ListenTo(
				"someQueue",
				queue =>
				{
					queue.Durable();
					queue.AutoDelete();
				},
				(props, json) =>
				{
					reset.Set();
					return true;
				});

			reset.WaitOne(TimeSpan.FromSeconds(10));
		}
	}
}
