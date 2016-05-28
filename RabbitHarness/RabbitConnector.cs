using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitHarness
{
	public class RabbitConnector : IRabbitConnector
	{
		private const string DefaultRoutingKey = "";

		private readonly ConnectionFactory _factory;
		private readonly IMessageHandler _messageHandler;

		public RabbitConnector(ConnectionFactory factory)
			: this(factory, new DefaultMessageHandler())
		{

		}

		public RabbitConnector(ConnectionFactory factory, IMessageHandler messageHandler)
		{
			_factory = factory;
			_messageHandler = new RawMessageHandlerDecorator(messageHandler);
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
			return ListenTo(exchangeDefinition, DefaultRoutingKey, handler);
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Func<IBasicProperties, TMessage, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var channel = connection.CreateModel();

			var queueName = channel.QueueDeclare();
			exchangeDefinition.Declare(channel);

			channel.QueueBind(queueName, exchangeDefinition.Name, routingKey);

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

				var bytes = _messageHandler.Serialize(message);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish("", queueDefinition.Name, props, bytes);
			}
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			SendTo(exchangeDefinition, DefaultRoutingKey, customiseProps, message);
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message)
		{
			using (var connection = _factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				exchangeDefinition.Declare(channel);

				var bytes = _messageHandler.Serialize(message);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish(exchangeDefinition.Name, routingKey, props, bytes);
			}
		}


		public void Query<TMessage>(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var sendChannel = connection.CreateModel();
			var replyChannel = connection.CreateModel();

			var replyTo = replyChannel.QueueDeclare().QueueName;

			var sendProps = sendChannel.CreateBasicProperties();
			customiseProps(sendProps);
			sendProps.CorrelationId = sendProps.CorrelationId ?? Guid.NewGuid().ToString();
			sendProps.ReplyTo = replyTo;

			var t = new Task(() =>
			{
				queueDefinition.Declare(replyChannel);

				var listener = new QueueingBasicConsumer(replyChannel);
				replyChannel.BasicConsume(replyTo, true, listener);

				try
				{
					while (true)
					{
						var e = listener.Queue.Dequeue();

						if (e.BasicProperties.CorrelationId != sendProps.CorrelationId)
							continue;

						var reply = _messageHandler.Deserialize<TMessage>(e.Body);

						handler(e.BasicProperties, reply);
						return;
					}
				}
				finally
				{
					replyChannel.Dispose();
					connection.Dispose();
				}
			});

			t.Start();


			sendChannel.BasicPublish(
				exchange: "",
				routingKey: queueDefinition.Name,
				basicProperties: sendProps,
				body: _messageHandler.Serialize(message));

			sendChannel.Dispose();

		}

		public void Query<TMessage>(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler)
		{
			Query(exchangeDefinition, DefaultRoutingKey, customiseProps, message, handler);
		}

		public void Query<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler)
		{
			var connection = _factory.CreateConnection();
			var replyChannel = connection.CreateModel();
			var sendChannel = connection.CreateModel();

			var replyTo = replyChannel.QueueDeclare().QueueName;

			var sendProps = sendChannel.CreateBasicProperties();
			customiseProps(sendProps);
			sendProps.CorrelationId = sendProps.CorrelationId ?? Guid.NewGuid().ToString();
			sendProps.ReplyTo = replyTo;

			var t = new Task(() =>
			{

				exchangeDefinition.Declare(replyChannel);

				var listener = new QueueingBasicConsumer(replyChannel);
				replyChannel.BasicConsume(replyTo, true, listener);

				try
				{
					while (true)
					{
						var e = listener.Queue.Dequeue();

						if (e.BasicProperties.CorrelationId != sendProps.CorrelationId)
							continue;

						var reply = _messageHandler.Deserialize<TMessage>(e.Body);

						handler(e.BasicProperties, reply);
						return;
					}
				}
				finally
				{
					replyChannel.Dispose();
					connection.Dispose();
				}
			});

			t.Start();

			sendChannel.BasicPublish(
				exchange: exchangeDefinition.Name,
				routingKey: routingKey,
				basicProperties: sendProps,
				body: _messageHandler.Serialize(message));

			sendChannel.Dispose();
		}

		private Action Listen<TMessage>(IModel channel, string queueName, Func<IBasicProperties, TMessage, bool> handler)
		{
			var wrapper = new EventHandler<BasicDeliverEventArgs>((s, e) =>
			{
				try
				{
					var message = _messageHandler.Deserialize<TMessage>(e.Body);

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
				noAck: false,
				consumer: listener);

			return () =>
			{
				listener.Received -= wrapper;
			};
		}
	}
}
