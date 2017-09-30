using RabbitMQ.Client;

namespace RabbitHarness
{
	public class QueryResponse<TMessage>
	{
		public IBasicProperties Properties { get; set; }
		public TMessage Message { get; set; }
	}
}
