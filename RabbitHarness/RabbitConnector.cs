using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHarness
{
	public class RabbitConnector
	{
		private readonly ConnectionFactory _factory;

		public RabbitConnector(ConnectionFactory factory)
		{
			_factory = factory;
		}

		public void Send(string queueName, Action<IBasicProperties> configureProps, object message)
		{
			using (var connection = _factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				var props = channel.CreateBasicProperties();
				configureProps(props);

				channel.BasicPublish("", queueName, props, bytes);
			}
		}

		public Action ListenTo(string queueName, Func<object, string, bool> handler)
		{
			return ListenTo(queueName, options => { }, handler);
		}

		/// <summary>
		/// Listen to a queue or exchange, and run the <param name="handler" /> when a message is received.
		/// Assumes the message has a UTF8 string body.
		/// </summary>
		/// <param name="queueName">The name of the queue or exchange to bind to.</param>
		/// <param name="declare">Options to declare a queue or exchange before listening starts.</param>
		/// <param name="handler">return true to Ack the message, false to Nack it.</param>
		/// <returns>Invoke the action returned to unsubscribe from the queue/exchange.</returns>
		public Action ListenTo(string queueName, Action<QueueDeclaration> declare, Func<IBasicProperties, string, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			var wrapper = new EventHandler<BasicDeliverEventArgs>((s, e) =>
			{
				try
				{
					var json = Encoding.UTF8.GetString(e.Body);
					var success = handler(e.BasicProperties, json);

					if (success)
						channel.BasicAck(e.DeliveryTag, multiple: false);
					else
						channel.BasicNack(e.DeliveryTag, multiple: false, requeue: true);

				}
				catch (Exception)
				{
					channel.BasicNack(e.DeliveryTag, multiple: false, requeue: true);
					throw;
				}

			});

			var listener = new EventingBasicConsumer(channel);
			listener.Received += wrapper;

			var options = new QueueDeclaration();
			declare(options);

			options.Apply(queueName, channel);

			channel.BasicConsume(queueName, true, listener);

			return () =>
			{
				listener.Received -= wrapper;

				channel.Dispose();
				connection.Dispose();
			};
		}

		public void Query(string queueName, Action<QueueDeclaration> declare, Action<IBasicProperties> configureProps, object message, Func<IBasicProperties, string, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			var correlationID = Guid.NewGuid().ToString();
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

						handler(e.BasicProperties, Encoding.UTF8.GetString(e.Body));
						return;
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

			configureProps(props);

			t.Start();

			var options = new QueueDeclaration();
			declare(options);

			options.Apply(queueName, channel);

			channel.BasicPublish(
				exchange: "",
				routingKey: queueName,
				basicProperties: props,
				body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
		}
	}
}
