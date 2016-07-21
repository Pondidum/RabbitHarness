using RabbitMQ.Client;

namespace RabbitHarness
{
	public class RabbitConnectorConfiguration
	{
		public ConnectionFactory Factory { get; set; }
		public IMessageSerializer Serializer { get; set; }
	}
}
