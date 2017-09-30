using RabbitMQ.Client;

namespace RabbitHarness
{
	public class RabbitConnectorConfiguration
	{
		public IConnectionFactory Factory { get; set; }
		public IMessageSerializer Serializer { get; set; }
	}
}
