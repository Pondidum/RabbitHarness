using System;
using RabbitMQ.Client;

namespace RabbitHarness.Tests
{
	public class TestBase : IDisposable
	{
		protected const string Host = "192.168.99.100";
		protected readonly string QueueName;
		protected readonly string ExchangeName;

		protected ConnectionFactory Factory { get; }

		public TestBase()
		{
			Factory = new ConnectionFactory { HostName = Host };

			QueueName = "TestsQueue" + Guid.NewGuid();
			ExchangeName = "TestExchange" + Guid.NewGuid();
		}

		public void Dispose()
		{
			using (var connection = Factory.CreateConnection())
			using (var channel = connection.CreateModel())
			{
				channel.QueueDelete(QueueName);
				channel.ExchangeDelete(ExchangeName);
			}
		}
	}
}
