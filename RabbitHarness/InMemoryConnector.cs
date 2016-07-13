using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace RabbitHarness
{
	public class InMemoryConnector : IRabbitConnector
	{
		private readonly Dictionary<string, List<Func<IBasicProperties, object, bool>>> _queues;
		private readonly Dictionary<string, List<ExchangeHandler>> _exchanges;

		public InMemoryConnector()
		{
			_queues = new Dictionary<string, List<Func<IBasicProperties, object, bool>>>();
			_exchanges = new Dictionary<string, List<ExchangeHandler>>();
		}

		public Action ListenTo<TMessage>(QueueDefinition queueDefinition, MessageHandler<TMessage> handler)
		{
			List<Func<IBasicProperties, object, bool>> handlers;

			if (_queues.TryGetValue(queueDefinition.Name, out handlers) == false)
			{
				handlers = new List<Func<IBasicProperties, object, bool>>();
				_queues[queueDefinition.Name] = handlers;
			}

			Func<IBasicProperties, object, bool> wrapped = (props, message) =>
			{
				while (handler.OnReceive(props, (TMessage)message) == false)
				{
				}

				return true;
			};

			handlers.Add(wrapped);

			return () =>
			{
				handlers.Remove(wrapped);
			};
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, QueueDefinition queueDefinition, MessageHandler<TMessage> handler)
		{
			List<ExchangeHandler> handlers;

			if (_exchanges.TryGetValue(exchangeDefinition.Name, out handlers) == false)
			{
				handlers = new List<ExchangeHandler>();
				_exchanges[exchangeDefinition.Name] = handlers;
			}

			var exchangeHandler = new ExchangeHandler
			{
				RoutingKey = queueDefinition.RoutingKeys.First(),
				Handler = (props, message) => handler.OnReceive(props, (TMessage)message),
			};

			handlers.Add(exchangeHandler);

			return () =>
			{
				handlers.Remove(exchangeHandler);
			};

		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, MessageHandler<TMessage> handler)
		{
			return ListenTo(exchangeDefinition, "#", handler);
		}

		public Action ListenTo<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, MessageHandler<TMessage> handler)
		{
			return ListenTo(exchangeDefinition, new QueueDefinition { Name = Guid.NewGuid().ToString(), AutoDelete = true, RoutingKeys = new[] { routingKey } }, handler);
		}

		public void SendTo(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			var props = new BasicProperties();
			customiseProps(props);

			List<Func<IBasicProperties, object, bool>> handlers;

			if (_queues.TryGetValue(queueDefinition.Name, out handlers) == false)
				return;

			handlers
				.ToList()
				.ForEach(handler => handler(props, message));
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			SendTo(exchangeDefinition, "#", customiseProps, message);
		}

		public void SendTo(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message)
		{
			var props = new BasicProperties();
			customiseProps(props);

			List<ExchangeHandler> handlers;

			if (_exchanges.TryGetValue(exchangeDefinition.Name, out handlers) == false)
				return;

			handlers
				.Where(eh => RoutingKeyMatches(eh.RoutingKey, routingKey))
				.ToList()
				.ForEach(ex => ex.Handler(props, message));
		}

		public static bool RoutingKeyMatches(string handlerKey, string messageKey)
		{
			if (handlerKey == messageKey)
				return true;
			
			if (string.IsNullOrEmpty(handlerKey))
				return true;

			if (handlerKey == "#")
				return true;
			
			var rx = new Regex("^" + handlerKey.Replace("*", "(.*?)") + "$");

			if (rx.IsMatch(messageKey))
				return true;

			return false;
		}

		public Task<QueryResponse<TMessage>> Query<TMessage>(QueueDefinition queueDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			var reply = Guid.NewGuid().ToString();

			Action unsubscribe = null;

			var source = new TaskCompletionSource<QueryResponse<TMessage>>();

			unsubscribe = ListenTo<TMessage>(new QueueDefinition { Name = reply }, new LambdaMessageHandler<TMessage>( (p, m) =>
			{
				unsubscribe();
				source.SetResult(new QueryResponse<TMessage> { Properties = p, Message = m});
				return true;
			}));

			SendTo(queueDefinition, props => props.ReplyTo = reply, message);

			return source.Task;
		}

		public Task<QueryResponse<TMessage>> Query<TMessage>(ExchangeDefinition exchangeDefinition, Action<IBasicProperties> customiseProps, object message)
		{
			var reply = Guid.NewGuid().ToString();

			Action unsubscribe = null;
			var source = new TaskCompletionSource<QueryResponse<TMessage>>();

			unsubscribe = ListenTo<TMessage>(new QueueDefinition { Name = reply }, new LambdaMessageHandler<TMessage>( (p, m) =>
			{
				unsubscribe();
				source.SetResult(new QueryResponse<TMessage> { Properties = p, Message = m });
				return true;
			}));

			SendTo(exchangeDefinition, props => props.ReplyTo = reply, message);

			return source.Task;
		}

		public Task<QueryResponse<TMessage>> Query<TMessage>(ExchangeDefinition exchangeDefinition, string routingKey, Action<IBasicProperties> customiseProps, object message)
		{
			throw new NotImplementedException();
		}

		private class ExchangeHandler
		{
			public string RoutingKey { get; set; }
			public Func<IBasicProperties, object, bool> Handler { get; set; }
		}
	}
}