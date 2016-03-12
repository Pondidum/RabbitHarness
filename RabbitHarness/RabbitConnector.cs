using System;
using System.Text;
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

		public Action ListenTo(string queueName, Func<object, string, bool> handler)
		{
			return ListenTo(queueName, options => { }, handler);
		}

		/// <summary>
		/// Listen to a queue or exchange, and run the <param name="handler" /> when a message is received.
		/// Assumes the message has a UTF8 string body.
		/// </summary>
		/// <param name="queueName">The name of the queue or exchange to bind to.</param>
		/// <param name="handler">return true to Ack the message, false to Nack it.</param>
		/// <returns>Invoke the action returned to unsubscribe from the queue/exchange.</returns>
		public Action ListenTo(string queueName, Action<QueueDeclaration> declare, Func<object, string, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			var wrapper = new EventHandler<BasicDeliverEventArgs>((s, e) =>
			{
				try
				{
					var json = Encoding.UTF8.GetString(e.Body);
					var success = handler(null, json);

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
	}
}
