using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class QueueDeclaration
	{
		private bool _durable;
		private bool _autoDelete;
		private bool _exclusive;
		private IDictionary<string, object> _args;
		private Declarations _declare;
		private string _exchangeType;

		public QueueDeclaration()
		{
			_declare = Declarations.Nothing;
		}

		public void DeclareQueue()
		{
			_declare = Declarations.Queue;
		}

		public void DeclareExchange()
		{
			_declare = Declarations.Exchange;
		}


		public void Durable()
		{
			_durable = true;
		}

		public void AutoDelete()
		{
			_autoDelete = true;
		}

		public void Exclusive()
		{
			_exclusive = true;
		}

		public void WithArgs(IDictionary<string, object> args)
		{
			_args = args;
		}

		public void ExchangeType(string type)
		{
			_exchangeType = type;
		}

		internal void Apply(string name, IModel channel)
		{
			if (_declare == Declarations.Queue)
				channel.QueueDeclare(name, _durable, _exclusive, _autoDelete, _args);

			if (_declare == Declarations.Exchange)
				channel.ExchangeDeclare(name, _exchangeType, _durable, _autoDelete, _args);
		}

		private enum Declarations
		{
			Nothing,
			Queue,
			Exchange
		}
	}
}
