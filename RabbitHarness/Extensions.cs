using System;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHarness
{
	public static class Extensions
	{
		public static void Query<TResponse>(this ConnectionFactory factory, string queueName, object message, Action<TResponse> callback)
		{
			var context = new QueryContext
			{
				QueueName = queueName,
			};

			factory.Query(context, message, callback);
		}

		public static void Query<TResponse>(this ConnectionFactory factory, QueryContext context, object message, Action<TResponse> callback)
		{
			var connection = context.CreateConnection(factory);
			var channel = connection.CreateModel();

			var correlationID = context.CreateCorrelationId();
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

			context.CustomiseProperties(props);

			channel.BasicPublish(
				exchange: context.ExchangeName,
				routingKey: context.QueueName,
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