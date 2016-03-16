using System;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public interface IRabbitConnector
	{
		Action ListenTo<TMessage>(QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler);
		Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler);
		Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, Func<IBasicProperties, TMessage, bool> handler);

		void SendTo(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message);
		void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message);
		void SendTo(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message);

		void Query<TMessage>(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler);
		void Query<TMessage>(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler);
	}
}
