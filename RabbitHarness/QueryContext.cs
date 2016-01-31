using System;
using RabbitMQ.Client;

namespace RabbitHarness
{
	public class QueryContext
	{
		public Func<ConnectionFactory, IConnection> CreateConnection { get; set; }
		public string QueueName { get; set; }
		public string ExchangeName { get; set; }
		public Func<string> CreateCorrelationId { get; set; }

		public QueryContext()
		{
			CreateConnection = f => f.CreateConnection();
			CreateCorrelationId = () => Guid.NewGuid().ToString();
			ExchangeName = "";
		}
	}
}
