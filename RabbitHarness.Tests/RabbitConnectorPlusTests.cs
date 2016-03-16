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
		private readonly AutoResetEvent _reset;
		private readonly RabbitConnectorPlus _connector;

		public RabbitConnectorPlusTests()
		{
			_reset = new AutoResetEvent(false);
			_connector = new RabbitConnectorPlus(Factory);
		}

		[Fact]
		public void When_listening_to_a_queue()
		{
			var queue = new QueueDefinition 
			{
				Name = QueueName,
				AutoDelete = true
			};

			int recieved = 0;

			_connector.ListenTo<int>(
				queue,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}


		[Fact]
		public void When_listening_to_a_queue_and_unsubscribed()
		{
			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true
			};

			int recieved = 0;

			var unsubscribe = _connector.ListenTo<int>(
				queue,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			unsubscribe();

			SendToQueue(456);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[Fact]
		public void When_listening_to_an_exchange_with_a_custom_queue()
		{
			var exchange = new ExchangeDefinition
			{
				Name = ExchangeName,
				AutoDelete = true,
				Type = "direct"
			};

			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true,
			};

			int recieved = 0;

			var unsubscribe = _connector.ListenTo<int>(
				exchange,
				queue,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			SendToExchange(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[Fact]
		public void When_listening_to_an_exchange_with_an_auto_queue()
		{
			var exchange = new ExchangeDefinition
			{
				Name = ExchangeName,
				AutoDelete = true,
				Type = "direct"
			};

			int recieved = 0;

			var unsubscribe = _connector.ListenTo<int>(
				exchange,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			SendToExchange(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[Fact]
		public void When_sending_to_a_queue()
		{
			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true
			};

			int recieved = 0;

			_connector.ListenTo<int>(
				queue,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			_connector.SendTo(queue, props => { }, 1235);
			
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(1235);
		}

		private void SendToQueue(object message)
		{
			using (var connection = Factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				channel.BasicPublish("", QueueName, channel.CreateBasicProperties(), bytes);
			}
		}

		private void SendToExchange(object message)
		{
			using (var connection = Factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				channel.BasicPublish(ExchangeName, "", channel.CreateBasicProperties(), bytes);
			}
		}
	}
}
