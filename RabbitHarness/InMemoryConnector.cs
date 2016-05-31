using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace RabbitHarness
{
	public class InMemoryConnector : IRabbitConnector
	{
		private Dictionary<string, List<Func<IBasicProperties, object, bool>>> _queues;

		public InMemoryConnector()
		{
			_queues = new Dictionary<string, List<Func<IBasicProperties, object, bool>>>();
		}

		public Action ListenTo<TMessage>(QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler)
		{
			List<Func<IBasicProperties, object, bool>> handlers;

			if (_queues.TryGetValue(queueDefinition.Name, out handlers) == false)
			{
				handlers = new List<Func<IBasicProperties, object, bool>>();
				_queues[queueDefinition.Name] = handlers;
			}

			Func<IBasicProperties, object, bool> wrapped = (props, message) => handler(props, (TMessage)message);

			handlers.Add(wrapped);

			return () =>
			{
				handlers.Remove(wrapped);
			};
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, Func<IBasicProperties, TMessage, bool> handler)
		{
			throw new NotImplementedException();
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, Func<IBasicProperties, TMessage, bool> handler)
		{
			throw new NotImplementedException();
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Func<IBasicProperties, TMessage, bool> handler)
		{
			throw new NotImplementedException();
		}

		public void SendTo(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			var props = new BasicProperties();
			customiseProps(props);

			List<Func<IBasicProperties, object, bool>> handlers;

			if (_queues.TryGetValue(queueDefinition.Name, out handlers) == false)
				return;

			handlers.ForEach(handler => handler(props, message));

		}

		public void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			throw new NotImplementedException();
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message)
		{
			throw new NotImplementedException();
		}

		public void Query<TMessage>(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler)
		{
			throw new NotImplementedException();
		}

		public void Query<TMessage>(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler)
		{
			throw new NotImplementedException();
		}

		public void Query<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message, Func<IBasicProperties, TMessage, bool> handler)
		{
			throw new NotImplementedException();
		}
	}
}