using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class ExchangeDefinition
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public bool AutoDelete { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }

		public virtual void Declare(IModel channel)
		{
			channel.ExchangeDeclare(
				Name,
				Type,
				Durable,
				AutoDelete,
				Args);
		}
	}
}
