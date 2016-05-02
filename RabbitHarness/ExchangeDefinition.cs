using System.Collections.Generic;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class ExchangeDefinition
	{
		public string Name { get; protected set; }
		public string Type { get; protected set; }
		public bool AutoDelete { get; set; }
		public bool Durable { get; set; }
		public IDictionary<string, object> Args { get; set; }

		protected ExchangeDefinition()
		{
		}

		public ExchangeDefinition(string name, string type)
		{
			Name = name;
			Type = type;
		}

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
