using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public interface IRabbitConnector
	{
		Action ListenTo<TMessage>(QueueDefinition queueDefinition, MessageHandler<TMessage> handler);
		Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, MessageHandler<TMessage> handler);
		Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, MessageHandler<TMessage> handler);
		Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, MessageHandler<TMessage> handler);

		void SendTo(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message);
		void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message);
		void SendTo(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message);

		Task<QueryResponse<TMessage>> Query<TMessage>(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message);
		Task<QueryResponse<TMessage>> Query<TMessage>(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message);
		Task<QueryResponse<TMessage>> Query<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message);
	}

	public class MessageHandler<TMessage>
	{
		public virtual void OnException(Exception ex)
		{
		}

		public virtual bool OnReceive(IBasicProperties props, TMessage message)
		{
			return false;
		}
	}

	public class LambdaMessageHandler<TMessage> : MessageHandler<TMessage>
	{
		private readonly Func<IBasicProperties, TMessage, bool> _handler;

		public LambdaMessageHandler(Func<IBasicProperties, TMessage, bool> handler)
		{
			_handler = handler;
		}

		public override bool OnReceive(IBasicProperties props, TMessage message)
		{
			return _handler(props, message);
		}
	}
}
