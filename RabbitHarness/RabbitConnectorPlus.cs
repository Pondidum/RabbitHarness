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

			queueDefinition.Declare(channel);

			var unsubscribe = Listen(channel, queueDefinition.Name, handler);

			return () =>
			{
				unsubscribe();
				channel.Dispose();
				connection.Dispose();
			};
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			exchangeDefinition.Declare(channel);
			queueDefinition.Declare(channel);

			foreach (var key in queueDefinition.RoutingKeys)
				channel.QueueBind(queueDefinition.Name, exchangeDefinition.Name, key);

			var unsubscribe = Listen(channel, queueDefinition.Name, handler);

			return () =>
			{
				unsubscribe();
				channel.Dispose();
				connection.Dispose();
			};
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, Func<IBasicProperties, TMessage, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			var queueName = channel.QueueDeclare();
			exchangeDefinition.Declare(channel);

			channel.QueueBind(queueName, exchangeDefinition.Name, "");

			var unsubscribe = Listen(channel, queueName, handler);

			return () =>
			{
				unsubscribe();
				channel.Dispose();
				connection.Dispose();
			};
		}

		public void SendTo(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			using (var connection = _factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				queueDefinition.Declare(channel);

				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish("", queueDefinition.Name, props, bytes);
			}
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			SendTo(exchangeDefinition, "", customiseProps, message);
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message)
		{
			using (var connection = _factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				exchangeDefinition.Declare(channel);

				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish(exchangeDefinition.Name, routingKey, props, bytes);
			}
		}

		private static Action Listen<TMessage>(IModel channel, string queueName, Func<IBasicProperties, TMessage, bool> handler)
		{
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

			channel.BasicConsume(
				queueName,
				noAck: true,
				consumer: listener);

			return () =>
			{
				listener.Received -= wrapper;
			};
		}
	}

	public class ExchangeDefinition
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public bool AutoDelete { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }

		public virtual void Declare(IModel channel)
		{
			channel.ExchangeDeclare(
				Name,
				Type,
				Durable,
				AutoDelete,
				Args);
		}
	}

	public class QueueDefinition
	{
		public string Name { get; set; }
		public bool AutoDelete { get; set; }
		public bool Exclusive { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }

		/// <summary>
		/// Only applies when binding to an exchange
		/// </summary>
		public IEnumerable<string> RoutingKeys { get; set; }

		public QueueDefinition()
		{
			RoutingKeys = new[] { "" };
		}

		public virtual void Declare(IModel channel)
		{
			channel.QueueDeclare(
				Name,
				Durable,
				Exclusive,
				AutoDelete,
				Args);
		}
	}
}
