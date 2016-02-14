using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHarness
{
	public static class Extensions
	{
		public static void Query<TResponse>(this ConnectionFactory factory, string queueName, object message, Action<ResponseContext<TResponse>> callback)
		{
			var context = new QueryContext
			{
				QueueName = queueName,
			};

			factory.Query(context, message, callback);
		}

		public static void Query<TResponse>(this ConnectionFactory factory, QueryContext context, object message, Action<ResponseContext<TResponse>> callback)
		{
			var connection = context.CreateConnection(factory);
			var channel = connection.CreateModel();

			var correlationID = context.CreateCorrelationId();
			var replyTo = channel.QueueDeclare().QueueName;

			var t = new Task(() =>
			{

				var listener = new QueueingBasicConsumer(channel);
				channel.BasicConsume(replyTo, true, listener);

				try
				{
					while (true)
					{

						var e = listener.Queue.Dequeue();

						if (e.BasicProperties.CorrelationId != correlationID)
							continue;

						var rc = new ResponseContext<TResponse>
						{
							Content = MessageFrom<TResponse>(e.Body),
							Headers = e.BasicProperties,
						};

						callback(rc);
					}
				}
				finally
				{
					channel.Dispose();
					connection.Dispose();
				}
			});


			var props = channel.CreateBasicProperties();
			props.CorrelationId = correlationID;
			props.ReplyTo = replyTo;
			props.Timestamp = context.GetTimestamp();

			context.CustomiseProperties(props);

			t.Start();

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