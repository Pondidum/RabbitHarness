﻿using System;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

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

		[RequiresRabbitFact]
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
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact]
		public void When_listening_to_a_queue_and_serialization_fails()
		{
			var queue = new QueueDefinition
			{
				Name = QueueName,
				AutoDelete = true
			};

			Exception exception = null;
			Dto recieved = null;

			var handler = new LambdaMessageHandler<Dto>(
				(props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				},
				ex =>
				{
					exception = ex;
				});

			_connector.ListenTo(queue, handler);

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(null);
			exception.ShouldBeOfType<JsonSerializationException>();
		}

		private class Dto
		{
			public string Name { get; set; }
		}

		[RequiresRabbitFact]
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
				new LambdaMessageHandler<int>((props, message) =>
				{
					recieved++;
					return recieved > 1;
				}));

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(2);
		}

		[RequiresRabbitFact]
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
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			SendToQueue(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			unsubscribe();

			SendToQueue(456);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact]
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
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			SendToExchange(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact]
		public void When_listening_to_an_exchange_with_an_auto_queue()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
			};

			int recieved = 0;

			var unsubscribe = _connector.ListenTo<int>(
				exchange,
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			SendToExchange(123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact]
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
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			_connector.SendTo(queue, props => { }, 1235);

			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(1235);
		}

		[RequiresRabbitFact]
		public void When_sending_to_an_exchange()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
			};

			int recieved = 0;

			var unsubscribe = _connector.ListenTo<int>(
				exchange,
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			_connector.SendTo(exchange, props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact]
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
				new LambdaMessageHandler<int>((props, json) =>
				{
					recieved = json;
					_reset.Set();
					return true;
				}));

			_connector.SendTo(exchange, "some.key", props => { }, 123);
			_reset.WaitOne(TimeSpan.FromSeconds(5));

			recieved.ShouldBe(123);
		}

		[RequiresRabbitFact]
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

			_connector
				.Query<int>(queue, props => { }, message)
				.ContinueWith(response =>
				{
					recieved = response.Result.Message;
					_reset.Set();
				})
				.Wait();

			unsubscribe();

			recieved.ShouldBe(4);
		}

		[RequiresRabbitFact]
		public void When_querying_an_exchange()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Direct)
			{
				AutoDelete = true
			};

			var unsubscribe = ExchangeResponder();
			int recieved = 0;
			var message = 1234;

			_connector
				.Query<int>(exchange, props => { }, message)
				.ContinueWith(response =>
				{
					recieved = response.Result.Message;
					_reset.Set();
				})
				.Wait();

			unsubscribe();

			recieved.ShouldBe(4);
		}

		[RequiresRabbitFact]
		public void When_querying_an_exchange_with_a_routngkey()
		{
			var exchange = new ExchangeDefinition(ExchangeName, ExchangeType.Topic)
			{
				AutoDelete = true,
			};

			var unsubscribe = ExchangeResponder(ExchangeType.Topic, "some.*");
			int recieved = 0;
			var message = 1234;

			_connector
				.Query<int>(exchange, "some.key", props => { }, message)
				.ContinueWith(response =>
				{
					recieved = response.Result.Message;
					_reset.Set();
				})
				.Wait(TimeSpan.FromSeconds(5));

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
				new ExchangeDefinition(ExchangeName, ExchangeType.Direct) { AutoDelete = true },
				props => { },
				message);
		}

		protected Action QueueResponder()
		{
			var queue = new QueueDefinition { Name = QueueName, AutoDelete = true };

			return _connector.ListenTo<int>(queue, new LambdaMessageHandler<int>((props, message) =>
			{
				var result = message.ToString().Length;
				_connector.SendTo(
					new QueueDefinition { Name = props.ReplyTo },
					p => { p.CorrelationId = props.CorrelationId; },
					result);
				return true;
			}));
		}

		protected Action ExchangeResponder(string exchangeType = ExchangeType.Direct, string routingKey = "")
		{
			var exchange = new ExchangeDefinition(ExchangeName, exchangeType) { AutoDelete = true };

			return _connector.ListenTo<int>(exchange, routingKey, new LambdaMessageHandler<int>((props, message) =>
			{
				var result = message.ToString().Length;
				_connector.SendTo(
					new QueueDefinition { Name = props.ReplyTo },
					p => { p.CorrelationId = props.CorrelationId; },
					result);
				return true;
			}));
		}
	}
}
