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
		private IMessageSerializer _messageSerializer;

		public RabbitConnector(ConnectionFactory factory)
		{
			_factory = factory;
			WithSerializer(new DefaultMessageSerializer());
		}

		public RabbitConnector WithSerializer(IMessageSerializer messageSerializer)
		{
			_messageSerializer = new RawMessageSerializerDecorator(messageSerializer);
			return this;
		}

		public Action ListenTo<TMessage>(QueueDefinition queueDefinition, MessageHandler<TMessage> handler)
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

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, MessageHandler<TMessage> handler)
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

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, MessageHandler<TMessage> handler)
		{
			return ListenTo(exchangeDefinition, DefaultRoutingKey, handler);
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, MessageHandler<TMessage> handler)
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

				var bytes = _messageSerializer.Serialize(message);

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

				var bytes = _messageSerializer.Serialize(message);

				var props = channel.CreateBasicProperties();
				customiseProps(props);

				channel.BasicPublish(exchangeDefinition.Name, routingKey, props, bytes);
			}
		}

		public Task<QueryResponse<TMessage>> Query<TMessage>(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			var connection = _factory.CreateConnection();
			var sendChannel = connection.CreateModel();
			var replyChannel = connection.CreateModel();

			var replyTo = replyChannel.QueueDeclare().QueueName;

			var sendProps = sendChannel.CreateBasicProperties();
			customiseProps(sendProps);
			sendProps.CorrelationId = sendProps.CorrelationId ?? Guid.NewGuid().ToString();
			sendProps.ReplyTo = replyTo;


			var source = new TaskCompletionSource<QueryResponse<TMessage>>();
			Action unsubscribe = null;

			unsubscribe = Listen<TMessage>(replyChannel, replyTo, new LambdaMessageHandler<TMessage>((p, m) =>
			{
				if (p.CorrelationId != sendProps.CorrelationId)
					return true;

				source.SetResult(new QueryResponse<TMessage> { Properties = p, Message = m });
				unsubscribe();
				return true;
			}));

			sendChannel.BasicPublish(
				exchange: "",
				routingKey: queueDefinition.Name,
				basicProperties: sendProps,
				body: _messageSerializer.Serialize(message));

			sendChannel.Dispose();

			return source.Task;
		}

		public Task<QueryResponse<TMessage>> Query<TMessage>(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			return Query<TMessage>(exchangeDefinition, DefaultRoutingKey, customiseProps, message);
		}

		public Task<QueryResponse<TMessage>> Query<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message)
		{
			var connection = _factory.CreateConnection();
			var replyChannel = connection.CreateModel();
			var sendChannel = connection.CreateModel();

			var replyTo = replyChannel.QueueDeclare().QueueName;

			var sendProps = sendChannel.CreateBasicProperties();
			customiseProps(sendProps);
			sendProps.CorrelationId = sendProps.CorrelationId ?? Guid.NewGuid().ToString();
			sendProps.ReplyTo = replyTo;

			var source = new TaskCompletionSource<QueryResponse<TMessage>>();
			Action unsubscribe = null;

			unsubscribe = Listen<TMessage>(replyChannel, replyTo, new LambdaMessageHandler<TMessage>((p, m) =>
			{
				if (p.CorrelationId != sendProps.CorrelationId)
					return true;

				source.SetResult(new QueryResponse<TMessage> { Properties = p, Message = m });
				unsubscribe();
				return true;
			}));

			sendChannel.BasicPublish(
				exchange: exchangeDefinition.Name,
				routingKey: routingKey,
				basicProperties: sendProps,
				body: _messageSerializer.Serialize(message));

			sendChannel.Dispose();

			return source.Task;
		}

		private Action Listen<TMessage>(IModel channel, string queueName, MessageHandler<TMessage> handler)
		{
			var wrapper = new EventHandler<BasicDeliverEventArgs>((s, e) =>
			{
				try
				{
					var message = _messageSerializer.Deserialize<TMessage>(e.Body);

					var success = handler.OnReceive(e.BasicProperties, message);

					if (success)
						channel.BasicAck(e.DeliveryTag, multiple: false);
					else
						channel.BasicNack(e.DeliveryTag, multiple: false, requeue: true);
				}
				catch (Exception ex)
				{
					channel.BasicNack(e.DeliveryTag, multiple: false, requeue: true);
					handler.OnException(ex);
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
