using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHarness
{
	public class RabbitConnectorPlus
	{
		private readonly ConnectionFactory _factory;

		public RabbitConnectorPlus(ConnectionFactory factory)
		{
			_factory = factory;
		}

		public Action ListenTo<TMessage>(QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			var wrapper = new EventHandler<BasicDeliverEventArgs>((s, e) =>
			{
				try
				{
					var json = Encoding.UTF8.GetString(e.Body);
					var message = JsonConvert.DeserializeObject<TMessage>(json);

					var success = handler(e.BasicProperties, message);

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

			channel.QueueDeclare(
				queueDefinition.Name,
				queueDefinition.Durable,
				queueDefinition.Exclusive,
				queueDefinition.AutoDelete,
				queueDefinition.Args);

			channel.BasicConsume(
				queueDefinition.Name,
				noAck:
				true, consumer: listener);

			return () =>
			{
				listener.Received -= wrapper;
				channel.Dispose();
				connection.Dispose();
			};
		}
	}

	public class QueueDefinition
	{
		public string Name { get; set; }
		public bool AutoDelete { get; set; }
		public bool Exclusive { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }
	}
}
