using System;
using System.Threading;
using RabbitMQ.Client;
using Shouldly;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorTests : TestBase
	{
		private readonly AutoResetEvent _reset;
		private readonly IRabbitConnector _connector;

		public RabbitConnectorTests()
		{
			_reset = new AutoResetEvent(false);
			_connector = new RabbitConnector(Factory);
			//_connector = new InMemoryConnector();
		}

		[RequiresRabbitFact(Host)]
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

		[RequiresRabbitFact(Host)]
		public void When_listening_to_a_queue_and_the_message_is_not_acknowleged()
		{
			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true
			};

			var recieved = 0;

			_connector.ListenTo<int>(
				queue,
				(props, message) =>
				{
					recieved++;
					return recieved > 1;
				});

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(2);
		}

		[RequiresRabbitFact(Host)]
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

		[RequiresRabbitFact(Host)]
		public void When_listening_to_an_exchange_with_a_custom_queue()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
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

		[RequiresRabbitFact(Host)]
		public void When_listening_to_an_exchange_with_an_auto_queue()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
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

		[RequiresRabbitFact(Host)]
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

		[RequiresRabbitFact(Host)]
		public void When_sending_to_an_exchange()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
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

			_connector.SendTo(exchange, props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact(Host)]
		public void When_sending_to_an_exchange_with_a_routing_key()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
			};

			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true,
				RoutingKeys = new[] { "some.key" }
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

			_connector.SendTo(exchange, "some.key", props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact(Host)]
		public void When_querying_a_queue()
		{
			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true
			};

			var unsubscribe = QueueResponder();
			int recieved = 0;
			var message = 1234;

			_connector.Query<int>(
				queue,
				props => { },
				message,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			_reset.WaitOne(TimeSpan.FromSeconds(5));
			unsubscribe();

			recieved.ShouldBe(4);
		}

		[RequiresRabbitFact(Host)]
		public void When_querying_an_exchange()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
			};

			var unsubscribe = ExchangeResponder();
			int recieved = 0;
			var message = 1234;

			_connector.Query<int>(
				exchange,
				props => { },
				message,
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				});

			_reset.WaitOne(TimeSpan.FromSeconds(5));
			unsubscribe();

			recieved.ShouldBe(4);
		}

		private void SendToQueue(object message)
		{
			_connector.SendTo(
				new QueueDefinition { Name = QueueName, AutoDelete = true },
				props => { },
				message);
		}

		private void SendToExchange(object message)
		{
			_connector.SendTo(
				new ExchangeDefinition(ExchangeName, ExchangeType.Direct),
				props => { },
				message);
		}

		protected Action QueueResponder()
		{
			var queue = new QueueDefinition { Name = QueueName, AutoDelete = true };

			return _connector.ListenTo<int>(queue, (props, message) =>
			{
				var result = message.ToString().Length;
				_connector.SendTo(
					new QueueDefinition { Name = props.ReplyTo },
					p => { p.CorrelationId = props.CorrelationId; },
					result);
				return true;
			});
		}

		protected Action ExchangeResponder()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct) { AutoDelete = true };

			return _connector.ListenTo<int>(exchange, (props, message) =>
			{
				var result = message.ToString().Length;
				_connector.SendTo(
					new QueueDefinition { Name = props.ReplyTo },
					p => { p.CorrelationId = props.CorrelationId; },
					result);
				return true;
			});
		}
	}
}
