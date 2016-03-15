using System.Collections.Generic;

namespace RabbitHarness
{
	public class ExchangeDeclaration
	{
		internal bool IsDurable { get; set; }
		internal bool IsExclusive { get; set; }
		internal bool IsAutoDeleting { get; set; }
		internal IDictionary<string, object> HasArgs { get; set; }
		internal string ExchangeType { get; set; }

		public ExchangeDeclaration OfType(string type)
		{
			ExchangeType = type;
			return this;
		}

		public ExchangeDeclaration Durable()
		{
			IsDurable = true;
			return this;
		}

		public ExchangeDeclaration AutoDelete()
		{
			IsAutoDeleting = true;
			return this;
		}

		public ExchangeDeclaration Exclusive()
		{
			IsExclusive = true;
			return this;
		}

		public ExchangeDeclaration WithArgs(IDictionary<string, object> args)
		{
			HasArgs = args;
			return this;
		}
	}
}
