using System.Collections.Generic;

namespace RabbitHarness
{
	public class QueueDeclaration
	{
		internal bool IsDurable { get; set; }
		internal bool IsExclusive { get; set; }
		internal bool IsAutoDeleting { get; set; }
		internal IDictionary<string, object> HasArgs { get; set; }

		public QueueDeclaration Durable()
		{
			IsDurable = true;
			return this;
		}

		public QueueDeclaration AutoDelete()
		{
			IsAutoDeleting = true;
			return this;
		}

		public QueueDeclaration Exclusive()
		{
			IsExclusive = true;
			return this;
		}

		public QueueDeclaration WithArgs(IDictionary<string, object> args)
		{
			HasArgs = args;
			return this;
		}
	}
}
