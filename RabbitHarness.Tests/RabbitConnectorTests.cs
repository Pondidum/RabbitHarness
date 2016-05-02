﻿using System;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;

namespace RabbitHarness.Tests
{
	public class RabbitConnectorTests : TestBase
	{
		private readonly AutoResetEvent _reset;
		private readonly RabbitConnector _connector;

		public RabbitConnectorTests()
		{
			_reset = new AutoResetEvent(false);
			_connector = new RabbitConnector(Factory);
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

		protected Action QueueResponder()
		{
			var connection = Factory.CreateConnection();
			var channel = connection.CreateModel();

			channel.QueueDeclare(QueueName, durable: false, exclusive: false, autoDelete: true, arguments: null);
			channel.BasicQos(0, 1, false);

			var listener = new EventingBasicConsumer(channel);

			EventHandler<BasicDeliverEventArgs> handler = (s, e) =>
			{
				var result = Encoding.UTF8.GetString(e.Body).Length.ToString();

				var props = channel.CreateBasicProperties();
				props.CorrelationId = e.BasicProperties.CorrelationId;

				channel.BasicPublish(
					exchange: "",
					routingKey: e.BasicProperties.ReplyTo,
					basicProperties: props,
					body: Encoding.UTF8.GetBytes(result));
				channel.BasicAck(e.DeliveryTag, false);
			};

			listener.Received += handler;

			channel.BasicConsume(QueueName, false, listener);

			return () =>
			{
				listener.Received -= handler;
				channel.Dispose();
				connection.Dispose();
			};
		}

		protected Action ExchangeResponder()
		{
			var connection = Factory.CreateConnection();
			var channel = connection.CreateModel();

			channel.ExchangeDeclare(ExchangeName, "direct", durable: false, autoDelete: true, arguments: null);
			var queueName = channel.QueueDeclare();

			channel.BasicQos(0, 1, false);
			channel.QueueBind(queueName, ExchangeName, "");

			var listener = new EventingBasicConsumer(channel);

			EventHandler<BasicDeliverEventArgs> handler = (s, e) =>
			{
				var result = Encoding.UTF8.GetString(e.Body).Length.ToString();

				var props = channel.CreateBasicProperties();
				props.CorrelationId = e.BasicProperties.CorrelationId;

				channel.BasicPublish(
					exchange: "",
					routingKey: e.BasicProperties.ReplyTo,
					basicProperties: props,
					body: Encoding.UTF8.GetBytes(result));
				channel.BasicAck(e.DeliveryTag, false);
			};

			listener.Received += handler;

			channel.BasicConsume(queueName, false, listener);

			return () =>
			{
				listener.Received -= handler;
				channel.Dispose();
				connection.Dispose();
			};
		}
	}
}
