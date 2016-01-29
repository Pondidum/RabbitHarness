using System;
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
		private readonly ITestOutputHelper _output;

		public Scratchpad(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void When_testing_something()
		{
			var factory = new ConnectionFactory { HostName = "192.168.99.100" };

			using (var connection = factory.CreateConnection())
			using (var model = connection.CreateModel())
			{
				model.QueueDeclare("some queue", true, false, false, null);
				model.BasicQos(0, 1, false);
			}

			var message = new
			{
				Message = "message"
			};

			var reset = new AutoResetEvent(false);

			factory.Query<string>("some queue", message, response =>
			{
				_output.WriteLine(response);
				reset.Set();
			});

			reset.WaitOne();

			using (var connection = factory.CreateConnection())
			using (var model = connection.CreateModel())
			{
				model.QueueDelete("some queue");
			}
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
