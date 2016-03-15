using System;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public interface IRabbitConnector
	{
		void Send(Route route, Action<IBasicProperties> configureProps, object message);
		Action ListenTo(Route route, Func<object, string, bool> handler);

		/// <summary>
		/// Listen to a queue or exchange, and run the <param name="handler" /> when a message is received.
		/// Assumes the message has a UTF8 string body.
		/// </summary>
		/// <param name="route">The names of the queue and/or exchange to bind to.</param>
		/// <param name="declare">Options to declare a queue or exchange before listening starts.</param>
		/// <param name="handler">return true to Ack the message, false to Nack it.</param>
		/// <returns>Invoke the action returned to unsubscribe from the queue/exchange.</returns>
		Action ListenTo(Route route, Action<DeclarationExpression> declare, Func<IBasicProperties, string, bool> handler);

		void Query(Route route, Action<DeclarationExpression> declare, Action<IBasicProperties> configureProps, object message, Func<IBasicProperties, string, bool> handler);
	}
}
