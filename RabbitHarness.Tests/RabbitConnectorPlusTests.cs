using System;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorPlusTests : TestBase
	{
		[Fact]
		public void When_listening_to_a_queue()
		{
			var connector = new RabbitConnectorPlus(Factory);

			var queue = new QueueDefinition 
			{
				Name = QueueName,
				AutoDelete = true
			};

			var reset = new AutoResetEvent(false);
			int recieved = 0;

			connector.ListenTo<int>(
				queue,
				(props, json) =>
				{
					recieved = json;
					reset.Set();
					return true;
				});

			Send(123);
			reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}


		[Fact]
		public void When_listening_to_a_queue_and_unsubscribed()
		{
			var connector = new RabbitConnectorPlus(Factory);

			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true
			};

			var reset = new AutoResetEvent(false);
			int recieved = 0;

			var unsubscribe = connector.ListenTo<int>(
				queue,
				(props, json) =>
				{
					recieved = json;
					reset.Set();
					return true;
				});

			Send(123);
			reset.WaitOne(TimeSpan.FromSeconds(5));

			unsubscribe();

			Send(456);
			reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}


		private void Send(object message)
		{
			using (var connection = Factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				channel.BasicPublish("", QueueName, channel.CreateBasicProperties(), bytes);
			}
		}
	}
}
