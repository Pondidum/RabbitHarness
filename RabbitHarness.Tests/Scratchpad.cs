﻿using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace RabbitHarness.Tests
{
	public class Scratchpad : IDisposable
	{
		public const string Host = "192.168.99.100";
		private readonly ITestOutputHelper _output;
		private IConnection _connection;
		private IModel _channel;
		private ConnectionFactory _factory;

		public Scratchpad(ITestOutputHelper output)
		{
			_output = output;

			_factory = new ConnectionFactory { HostName = Host };

			_connection = _factory.CreateConnection();
			_channel = _connection.CreateModel();

		}

		[RequiresRabbitFact(Host)]
		public void When_a_reply_is_sent()
		{
			CreateResponder();

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };

			_factory.Query<int>("some queue", message, response =>
			{
				response.ShouldBe(21);
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(10));
		}

		[RequiresRabbitFact(Host)]
		public void When_nothing_is_listening()
		{
			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			_factory.Query<int>("some queue", message, response =>
			{
				received = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

		[RequiresRabbitFact(Host)]
		public void When_something_else_responds()
		{
			CreateResponder(p =>  p.CorrelationId = Guid.NewGuid().ToString());

			var reset = new AutoResetEvent(false);
			var message = new { Message = "message" };
			var received = false;

			_factory.Query<int>("some queue", message, response =>
			{
				received = true;
				reset.Set();
			});

			reset.WaitOne(TimeSpan.FromSeconds(5));
			received.ShouldBe(false);
		}

		private void CreateResponder()
		{
			CreateResponder(p => { });
		}
		private void CreateResponder(Action<IBasicProperties> mangle)
		{

			_channel.QueueDeclare("some queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
			_channel.BasicQos(0, 1, false);

			var listener = new EventingBasicConsumer(_channel);

			listener.Received += (s, e) =>
			{
				var result = Encoding.UTF8.GetString(e.Body).Length.ToString();

				var props = _channel.CreateBasicProperties();
				props.CorrelationId = e.BasicProperties.CorrelationId;

				mangle(props);

				_channel.BasicPublish(
					exchange: "",
					routingKey: e.BasicProperties.ReplyTo,
					basicProperties: props,
					body: Encoding.UTF8.GetBytes(result));
				_channel.BasicAck(e.DeliveryTag, false);
			};

			_channel.BasicConsume("some queue", false, listener);

			return;
		}

		public void Dispose()
		{
			_channel.Dispose();
			_connection.Dispose();
		}
	}
}
