using System;
using System.Collections.Generic;
using System.Linq;
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

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler)
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

			channel.ExchangeDeclare(
				exchangeDefinition.Name,
				exchangeDefinition.Type,
				exchangeDefinition.Durable,
				exchangeDefinition.AutoDelete,
				exchangeDefinition.Args);

			channel.QueueDeclare(
				queueDefinition.Name,
				queueDefinition.Durable,
				queueDefinition.Exclusive,
				queueDefinition.AutoDelete,
				queueDefinition.Args);

			foreach (var key in queueDefinition.RoutingKeys)
				channel.QueueBind(queueDefinition.Name, exchangeDefinition.Name, key);

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

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, Func<IBasicProperties, TMessage, bool> handler)
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

			channel.ExchangeDeclare(
				exchangeDefinition.Name,
				exchangeDefinition.Type,
				exchangeDefinition.Durable,
				exchangeDefinition.AutoDelete,
				exchangeDefinition.Args);

			var queue = channel.QueueDeclare();

			channel.QueueBind(queue, exchangeDefinition.Name, "");

			channel.BasicConsume(
				queue,
				noAck:
				true, consumer: listener);

			return () =>
			{
				listener.Received -= wrapper;
				channel.Dispose();
				connection.Dispose();
			};
		}

		public void SendTo(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			using (var connection = _factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				channel.QueueDeclare(
					queueDefinition.Name,
					queueDefinition.Durable,
					queueDefinition.Exclusive,
					queueDefinition.AutoDelete,
					queueDefinition.Args);

				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish("", queueDefinition.Name, props, bytes);
			}
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			using (var connection = _factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				channel.ExchangeDeclare(
					exchangeDefinition.Name,
					exchangeDefinition.Type,
					exchangeDefinition.Durable,
					exchangeDefinition.AutoDelete,
					exchangeDefinition.Args);

				var json = JsonConvert.SerializeObject(message);
				var bytes = Encoding.UTF8.GetBytes(json);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish(exchangeDefinition.Name, "", props, bytes);
			}
		}
	}

	public class ExchangeDefinition
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public bool AutoDelete { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }
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
	}
}
