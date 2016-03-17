using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class QueueDefinition
	{
		public string Name { get; set; }
		public bool AutoDelete { get; set; }
		public bool Exclusive { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }

		/// <summary>
		/// Only applies when binding to an exchange
		/// </summary>
		public IEnumerable<string> RoutingKeys { get; set; }

		public QueueDefinition()
		{
			RoutingKeys = new[] { "" };
		}

		public virtual void Declare(IModel channel)
		{
			channel.QueueDeclare(
				Name,
				Durable,
				Exclusive,
				AutoDelete,
				Args);
		}
	}
}
