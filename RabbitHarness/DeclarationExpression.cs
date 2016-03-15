using RabbitMQ.Client;

namespace RabbitHarness
{
	public class DeclarationExpression
	{
		private ExchangeDeclaration _exchange;
		private QueueDeclaration _queue;

		public ExchangeDeclaration Exchange()
		{
			_exchange = new ExchangeDeclaration();
			return _exchange;
		}

		public QueueDeclaration Queue()
		{
			_queue = new QueueDeclaration();
			return _queue;
		}

		internal void Apply(Route route, IModel channel)
		{
			if (_queue != null)
				channel.QueueDeclare(route.QueueName, _queue.IsDurable, _queue.IsExclusive, _queue.IsAutoDeleting, _queue.HasArgs);

			if (_exchange != null)
				channel.ExchangeDeclare(route.ExchangeName, _exchange.ExchangeType, _exchange.IsDurable, _exchange.IsAutoDeleting, _exchange.HasArgs);
		}
	}
}
