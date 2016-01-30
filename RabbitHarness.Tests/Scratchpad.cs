﻿using System;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using Xunit.Abstractions;

namespace RabbitHarness.Tests
{
	public class Scratchpad
	{
		public const string Host = "192.168.99.100";
		private readonly ITestOutputHelper _output;

		public Scratchpad(ITestOutputHelper output)
		{
			_output = output;
		}

		[RequiresRabbitFact(Host)]
		public void When_testing_something()
		{
			var factory = new ConnectionFactory { HostName = Host };

			using (var connection = factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				channel.QueueDeclare("some queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
				channel.BasicQos(0, 1, false);

				CreateResponder(channel);

				var reset = new AutoResetEvent(false);
				var message = new { Message = "message" };

				factory.Query<string>("some queue", message, response =>
				{
					_output.WriteLine(response);
					reset.Set();
				});

				reset.WaitOne(TimeSpan.FromSeconds(10));

				channel.QueueDelete("some queue");
			}
		}

		private static void CreateResponder(IModel channel)
		{
			var listener = new EventingBasicConsumer(channel);

			listener.Received += (s, e) =>
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

			channel.BasicConsume("some queue", false, listener);
		}
	}

	public static class Extensions
	{
		public static void Query<TResponse>(this ConnectionFactory factory, string queueName, object message, Action<TResponse> callback)
		{
			var connection = factory.CreateConnection();
			var channel = connection.CreateModel();

			var correlationID = Guid.NewGuid().ToString();
			var replyTo = channel.QueueDeclare().QueueName;

			var listener = new EventingBasicConsumer(channel);
			channel.BasicConsume(replyTo, true, listener);
			listener.Received += (s, e) =>
			{
				if (e.BasicProperties.CorrelationId != correlationID)
					return;

				try
				{
					callback(MessageFrom<TResponse>(e.Body));
				}
				finally
				{
					channel.Dispose();
					connection.Dispose();
				}
			};

			var props = channel.CreateBasicProperties();
			props.CorrelationId = correlationID;
			props.ReplyTo = replyTo;

			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: props,
				body: BodyFrom(message));
		}

		private static byte[] BodyFrom(object message)
		{
			var json = JsonConvert.SerializeObject(message);
			return Encoding.UTF8.GetBytes(json);
		}

		private static T MessageFrom<T>(byte[] body)
		{
			var json = Encoding.UTF8.GetString(body);
			return JsonConvert.DeserializeObject<T>(json);
		}
	}
}
